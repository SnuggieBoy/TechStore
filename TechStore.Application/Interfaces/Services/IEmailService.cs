namespace TechStore.Application.Interfaces.Services
{
    /// <summary>
    /// Sends emails (e.g. order confirmation). If SMTP is not configured, operations may no-op or log.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send order confirmation email to the customer after order is placed successfully.
        /// </summary>
        Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string status, CancellationToken cancellationToken = default);
    }
}
