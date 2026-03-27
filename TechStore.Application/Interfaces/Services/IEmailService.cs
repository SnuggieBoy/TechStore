using System.Threading.Tasks;

namespace TechStore.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendOtpEmailAsync(string to, string otpCode);
        Task SendOrderConfirmationAsync(string to, string fullName, int orderId, decimal amount, string paymentStatus, string orderStatus);
        Task SendOrderStatusUpdatedAsync(string to, string fullName, int orderId, decimal amount, string type, string fromStatus, string toStatus);
        Task SendOrderCancelledAsync(string to, string fullName, int orderId, decimal amount);
        Task SendPaymentSuccessAsync(string to, string fullName, int orderId, decimal amount);

    }
}
