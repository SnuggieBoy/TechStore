namespace TechStore.Application.DTOs.Payment
{
    /// <summary>
    /// Request to create VNPay payment URL for an order.
    /// </summary>
    public class CreateVnPayUrlDto
    {
        /// <summary>Order PublicId (GUID string).</summary>
        public string OrderId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing VNPay payment URL for redirect.
    /// </summary>
    public class VnPayUrlResponseDto
    {
        public string PaymentUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// VNPay callback result after payment.
    /// </summary>
    public class VnPayCallbackDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public decimal Amount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
