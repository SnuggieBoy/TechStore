using System;
using TechStore.Application.DTOs.Order;
using TechStore.Application.DTOs.Product;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Application.Interfaces.Services;
using TechStore.Domain.Entities;

namespace TechStore.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _emailService = emailService;
        }

        /// <summary>
        /// Create order with: duplicate merge, input sanitize, transaction, stock validation.
        /// </summary>
        public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            // FIX #7: Sanitize string inputs
            var shippingAddress = SanitizeString(dto.ShippingAddress);
            var paymentMethod = SanitizeString(dto.PaymentMethod ?? "COD");

            // FIX #2: Merge duplicate ProductId (PublicId) items (combine quantities)
            var mergedItems = dto.Items
                .GroupBy(i => i.ProductId?.Trim() ?? string.Empty)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => new CreateOrderItemDto
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(i => i.Quantity)
                })
                .ToList();

            // FIX #8: Use DB transaction for atomic stock operations
            var transactionDisposable = await _orderRepository.BeginTransactionAsync();
            // Cast to access Commit/Rollback
            dynamic transaction = transactionDisposable;

            try
            {
                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                // Validate all products and stock (ProductId is PublicId string)
                foreach (var item in mergedItems)
                {
                    if (!Guid.TryParse(item.ProductId, out var productPublicId))
                        throw new KeyNotFoundException("Product not found");
                    var product = await _productRepository.GetByPublicIdAsync(productPublicId)
                        ?? throw new KeyNotFoundException("Product not found");

                    if (product.StockQuantity < item.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, Requested: {item.Quantity}");

                    // Deduct stock
                    product.StockQuantity -= item.Quantity;
                    _productRepository.Update(product);

                    var unitPrice = product.Price;
                    totalAmount += unitPrice * item.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    PaymentStatus = "Unpaid",
                    OrderStatus = "Pending",
                    PaymentMethod = paymentMethod,
                    ShippingAddress = shippingAddress,
                    OrderItems = orderItems
                };

                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload with navigation properties
                var created = await _orderRepository.GetByIdAsync(order.Id);
                var orderDto = MapToDto(created!);

                // Send order confirmation email (fire-and-forget; failure does not fail the request)
                try
                {
                    if (!string.IsNullOrWhiteSpace(created!.User?.Email))
                        await _emailService.SendOrderConfirmationAsync(
                            created.User.Email,
                            created.User.FullName ?? created.User.Username ?? "Khách hàng",
                            created.Id,
                            created.TotalAmount,
                            created.PaymentStatus,
                            created.OrderStatus);
                }
                catch
                {
                    // Email failure is non-fatal; order already saved
                }

                return orderDto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// FIX #10: Paginated order history for customer.
        /// </summary>
        public async Task<PagedResult<OrderDto>> GetMyOrdersAsync(int userId, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            page = Math.Max(1, page);

            var orders = await _orderRepository.GetByUserIdAsync(userId);
            var totalCount = orders.Count;

            var items = orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResult<OrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// FIX #10: Paginated order list for admin.
        /// </summary>
        public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 10)
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            page = Math.Max(1, page);

            var orders = await _orderRepository.GetAllAsync();
            var totalCount = orders.Count;

            var items = orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResult<OrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<OrderDto> GetByPublicIdAsync(string publicId)
        {
            var guid = ParseOrderPublicId(publicId);
            var order = await _orderRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Order not found");

            return MapToDto(order);
        }

        /// <summary>
        /// Admin: update order/shipping status (Pending → Shipping → Delivered).
        /// </summary>
        public async Task<OrderDto> UpdateOrderStatusAsync(string orderPublicId, UpdateOrderStatusDto dto)
        {
            var guid = ParseOrderPublicId(orderPublicId);
            var order = await _orderRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Order not found");

            // Cannot update shipping status if payment is cancelled
            if (order.PaymentStatus == "Cancelled")
                throw new InvalidOperationException("Cannot update order status: payment has been cancelled.");

            ValidateOrderStatusTransition(order.OrderStatus, dto.OrderStatus);

            var previousStatus = order.OrderStatus;
            order.OrderStatus = dto.OrderStatus;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Notify customer by email
            try
            {
                if (string.IsNullOrWhiteSpace(order.User?.Email)) return MapToDto(order);
                var customerName = order.User.FullName ?? order.User.Username ?? "Khách hàng";
                await _emailService.SendOrderStatusUpdatedAsync(order.User.Email, customerName, order.Id, order.TotalAmount, "Giao hàng", previousStatus, dto.OrderStatus);
            }
            catch
            {
                // Email failure is non-fatal
            }

            return MapToDto(order);
        }

        /// <summary>
        /// Admin: update payment status (Unpaid → Paid | Cancelled, Paid → Cancelled).
        /// </summary>
        public async Task<OrderDto> UpdatePaymentStatusAsync(string orderPublicId, UpdatePaymentStatusDto dto)
        {
            var guid = ParseOrderPublicId(orderPublicId);
            var order = await _orderRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Order not found");

            ValidatePaymentStatusTransition(order.PaymentStatus, dto.PaymentStatus);

            var previousStatus = order.PaymentStatus;

            // If cancelling payment, restore stock
            if (dto.PaymentStatus == "Cancelled")
            {
                await RestoreStock(order);
            }

            order.PaymentStatus = dto.PaymentStatus;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Notify customer by email
            try
            {
                if (string.IsNullOrWhiteSpace(order.User?.Email)) return MapToDto(order);
                var customerName = order.User.FullName ?? order.User.Username ?? "Khách hàng";
                if (dto.PaymentStatus == "Cancelled")
                    await _emailService.SendOrderCancelledAsync(order.User.Email, customerName, order.Id, order.TotalAmount);
                else
                    await _emailService.SendOrderStatusUpdatedAsync(order.User.Email, customerName, order.Id, order.TotalAmount, "Thanh toán", previousStatus, dto.PaymentStatus);
            }
            catch
            {
                // Email failure is non-fatal
            }

            return MapToDto(order);
        }

        /// <summary>
        /// Customer can cancel their own Unpaid + Pending orders only.
        /// </summary>
        public async Task<OrderDto> CancelMyOrderAsync(int userId, string orderPublicId)
        {
            var guid = ParseOrderPublicId(orderPublicId);
            var order = await _orderRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Order not found");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("You can only cancel your own orders");

            if (order.PaymentStatus != "Unpaid")
                throw new InvalidOperationException(
                    $"Cannot cancel order with payment status '{order.PaymentStatus}'. Only 'Unpaid' orders can be cancelled by customers.");

            if (order.OrderStatus != "Pending")
                throw new InvalidOperationException(
                    $"Cannot cancel order with shipping status '{order.OrderStatus}'. Only 'Pending' orders can be cancelled.");

            // Restore stock
            await RestoreStock(order);

            order.PaymentStatus = "Cancelled";
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Send order cancelled email
            try
            {
                if (!string.IsNullOrWhiteSpace(order.User?.Email))
                    await _emailService.SendOrderCancelledAsync(
                        order.User.Email,
                        order.User.FullName ?? order.User.Username ?? "Khách hàng",
                        order.Id,
                        order.TotalAmount);
            }
            catch
            {
                // Email failure is non-fatal
            }

            return MapToDto(order);
        }

        /// <summary>
        /// Mock payment: simulate ~2s processing delay, then set PaymentStatus from Unpaid to Paid.
        /// </summary>
        public async Task<OrderDto> PayOrderAsync(int userId, string orderPublicId)
        {
            var guid = ParseOrderPublicId(orderPublicId);
            var order = await _orderRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Order not found");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("You can only pay for your own orders");

            if (order.PaymentStatus != "Unpaid")
                throw new InvalidOperationException(
                    $"Order cannot be paid. Current payment status: '{order.PaymentStatus}'. Only 'Unpaid' orders can be paid.");

            // Simulate payment processing delay (~2 seconds)
            await Task.Delay(2000);

            order.PaymentStatus = "Paid";
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Send payment success email (full payment flow)
            try
            {
                if (!string.IsNullOrWhiteSpace(order.User?.Email))
                    await _emailService.SendPaymentSuccessAsync(
                        order.User.Email,
                        order.User.FullName ?? order.User.Username ?? "Khách hàng",
                        order.Id,
                        order.TotalAmount);
            }
            catch
            {
                // Email failure is non-fatal
            }

            return MapToDto(order);
        }

        #region Private Helpers

        private void ValidatePaymentStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Unpaid", new[] { "Paid", "Cancelled" } },
                { "Paid", new[] { "Cancelled" } },       // refund scenario
                { "Cancelled", Array.Empty<string>() }
            };

            if (validTransitions.TryGetValue(currentStatus, out var allowed))
            {
                if (!allowed.Contains(newStatus))
                    throw new InvalidOperationException(
                        $"Cannot change payment status from '{currentStatus}' to '{newStatus}'. Allowed: {string.Join(", ", allowed)}");
            }
        }

        private void ValidateOrderStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Shipping" } },
                { "Shipping", new[] { "Delivered" } },
                { "Delivered", Array.Empty<string>() }
            };

            if (validTransitions.TryGetValue(currentStatus, out var allowed))
            {
                if (!allowed.Contains(newStatus))
                    throw new InvalidOperationException(
                        $"Cannot change order status from '{currentStatus}' to '{newStatus}'. Allowed: {string.Join(", ", allowed)}");
            }
        }

        private async Task RestoreStock(Order order)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _productRepository.Update(product);
                }
            }
        }

        /// <summary>
        /// FIX #7: Basic XSS sanitization — strip HTML tags and trim whitespace.
        /// </summary>
        private static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Remove HTML tags
            var sanitized = System.Text.RegularExpressions.Regex.Replace(input, "<[^>]*>", string.Empty);
            return sanitized.Trim();
        }

        private static Guid ParseOrderPublicId(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId) || !Guid.TryParse(publicId, out var guid))
                throw new ArgumentException("Invalid order ID format.");
            return guid;
        }

        private static OrderDto MapToDto(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                PublicId = o.PublicId.ToString(),
                UserId = o.UserId,
                Username = o.User?.Username ?? string.Empty,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                PaymentStatus = o.PaymentStatus,
                OrderStatus = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                ShippingAddress = o.ShippingAddress,
                Items = o.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    ProductImageUrl = oi.Product?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList() ?? new List<OrderItemDto>()
            };
        }

        #endregion
    }
}
