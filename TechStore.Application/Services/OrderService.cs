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

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        /// <summary>
        /// Create order with: duplicate merge, input sanitize, transaction, stock validation.
        /// </summary>
        public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            // FIX #7: Sanitize string inputs
            var shippingAddress = SanitizeString(dto.ShippingAddress);
            var paymentMethod = SanitizeString(dto.PaymentMethod ?? "COD");

            // FIX #2: Merge duplicate ProductId items (combine quantities)
            var mergedItems = dto.Items
                .GroupBy(i => i.ProductId)
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

                // Validate all products and stock
                foreach (var item in mergedItems)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId)
                        ?? throw new KeyNotFoundException($"Product with id {item.ProductId} not found");

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
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentMethod = paymentMethod,
                    ShippingAddress = shippingAddress,
                    OrderItems = orderItems
                };

                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload with navigation properties
                var created = await _orderRepository.GetByIdAsync(order.Id);
                return MapToDto(created!);
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

        public async Task<OrderDto> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Order with id {id} not found");

            return MapToDto(order);
        }

        /// <summary>
        /// Admin update status with validated transitions.
        /// </summary>
        public async Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Order with id {id} not found");

            ValidateStatusTransition(order.Status, dto.Status);

            // If cancelling, restore stock
            if (dto.Status == "Cancelled")
            {
                await RestoreStock(order);
            }

            order.Status = dto.Status;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return MapToDto(order);
        }

        /// <summary>
        /// FIX #6: Customer can cancel their own Pending orders only.
        /// </summary>
        public async Task<OrderDto> CancelMyOrderAsync(int userId, int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException($"Order with id {orderId} not found");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("You can only cancel your own orders");

            if (order.Status != "Pending")
                throw new InvalidOperationException(
                    $"Cannot cancel order in '{order.Status}' status. Only 'Pending' orders can be cancelled by customers.");

            // Restore stock
            await RestoreStock(order);

            order.Status = "Cancelled";
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return MapToDto(order);
        }

        /// <summary>
        /// Mock payment: simulate ~2s processing delay, then set order status from Pending to Paid.
        /// </summary>
        public async Task<OrderDto> PayOrderAsync(int userId, int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException($"Order with id {orderId} not found");

            if (order.UserId != userId)
                throw new UnauthorizedAccessException("You can only pay for your own orders");

            if (order.Status != "Pending")
                throw new InvalidOperationException(
                    $"Order cannot be paid. Current status: '{order.Status}'. Only 'Pending' orders can be paid.");

            // Simulate payment processing delay (~2 seconds)
            await Task.Delay(2000);

            order.Status = "Paid";
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return MapToDto(order);
        }

        #region Private Helpers

        private void ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Confirmed", "Cancelled" } },
                { "Confirmed", new[] { "Shipped", "Cancelled" } },
                { "Shipped", new[] { "Delivered" } },
                { "Delivered", Array.Empty<string>() },
                { "Cancelled", Array.Empty<string>() }
            };

            if (validTransitions.TryGetValue(currentStatus, out var allowed))
            {
                if (!allowed.Contains(newStatus))
                    throw new InvalidOperationException(
                        $"Cannot change status from '{currentStatus}' to '{newStatus}'. Allowed: {string.Join(", ", allowed)}");
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

        private static OrderDto MapToDto(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                Username = o.User?.Username ?? string.Empty,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
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
