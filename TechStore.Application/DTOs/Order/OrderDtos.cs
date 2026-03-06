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

    /// <summary>ProductId is Product.PublicId (GUID string) from product list.</summary>
    public class CreateOrderItemDto
    {
        [Required]
        public string ProductId { get; set; } = string.Empty; // PublicId (GUID)

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Mock payment: pay for an order (Customer). Use PublicId (GUID) to avoid exposing sequential ID.
    /// </summary>
    public class PayOrderDto
    {
        [Required]
        public string OrderId { get; set; } = string.Empty; // PublicId (GUID string), e.g. "a1b2c3d4-e5f6-..."
    }

    /// <summary>
    /// Update order/shipping status (Admin). Values: Pending, Shipping, Delivered.
    /// </summary>
    public class UpdateOrderStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Shipping|Delivered)$",
            ErrorMessage = "Order status must be: Pending, Shipping, or Delivered")]
        public string OrderStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update payment status (Admin). Values: Unpaid, Paid, Cancelled.
    /// </summary>
    public class UpdatePaymentStatusDto
    {
        [Required]
        [RegularExpression("^(Unpaid|Paid|Cancelled)$",
            ErrorMessage = "Payment status must be: Unpaid, Paid, or Cancelled")]
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public int Id { get; set; }
        /// <summary>Use this in API routes (cancel, pay, get by id). Not guessable.</summary>
        public string PublicId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
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
