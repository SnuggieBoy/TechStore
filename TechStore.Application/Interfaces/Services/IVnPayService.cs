using TechStore.Application.DTOs.Payment;

namespace TechStore.Application.Interfaces.Services
{
    public interface IVnPayService
    {
        /// <summary>
        /// Generate VNPay sandbox payment URL for an order.
        /// </summary>
        string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string clientIpAddress);

        /// <summary>
        /// Validate VNPay callback query parameters and return parsed result.
        /// </summary>
        VnPayCallbackDto ProcessCallback(IDictionary<string, string> queryParams);
    }
}
