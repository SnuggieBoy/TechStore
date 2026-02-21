using TechStore.Application.DTOs.Order;
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
        /// Create order: validate stock → deduct stock → create order + items.
        /// </summary>
        public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            // Validate all products and stock
            foreach (var item in dto.Items)
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
                PaymentMethod = dto.PaymentMethod ?? "COD",
                ShippingAddress = dto.ShippingAddress,
                OrderItems = orderItems
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _orderRepository.GetByIdAsync(order.Id);
            return MapToDto(created!);
        }

        public async Task<List<OrderDto>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Order with id {id} not found");

            return MapToDto(order);
        }

        public async Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Order with id {id} not found");

            // Validate status transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Confirmed", "Cancelled" } },
                { "Confirmed", new[] { "Shipped", "Cancelled" } },
                { "Shipped", new[] { "Delivered" } },
                { "Delivered", Array.Empty<string>() },
                { "Cancelled", Array.Empty<string>() }
            };

            if (validTransitions.TryGetValue(order.Status, out var allowed))
            {
                if (!allowed.Contains(dto.Status))
                    throw new InvalidOperationException(
                        $"Cannot change status from '{order.Status}' to '{dto.Status}'. Allowed: {string.Join(", ", allowed)}");
            }

            // If cancelling, restore stock
            if (dto.Status == "Cancelled" && order.Status != "Cancelled")
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

            order.Status = dto.Status;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return MapToDto(order);
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
    }
}
