using System.ComponentModel.DataAnnotations;

namespace TechStore.Application.DTOs.Order
{
    /// <summary>
    /// Create order request from Flutter (Cart checkout).
    /// </summary>
    public class CreateOrderDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "Order must have at least one item")]
        public List<CreateOrderItemDto> Items { get; set; } = new();

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } = "COD";

        [Required(ErrorMessage = "Shipping address is required")]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class CreateOrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Mock payment: pay for an order (Customer). Backend simulates ~2s delay then sets status to Paid.
    /// </summary>
    public class PayOrderDto
    {
        [Required]
        public int OrderId { get; set; }
    }

    /// <summary>
    /// Update order status (Admin).
    /// </summary>
    public class UpdateOrderStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Confirmed|Shipped|Delivered|Cancelled)$",
            ErrorMessage = "Status must be: Pending, Confirmed, Shipped, Delivered, or Cancelled")]
        public string Status { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal => Quantity * UnitPrice;
    }
}
