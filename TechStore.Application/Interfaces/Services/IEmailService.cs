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

        /// <summary>
        /// Send payment success email when order is paid (POST /api/orders/pay).
        /// </summary>
        Task SendPaymentSuccessAsync(string toEmail, string customerName, int orderId, decimal totalAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send order cancelled email when customer cancels a Pending order (PUT /api/orders/{id}/cancel).
        /// </summary>
        Task SendOrderCancelledAsync(string toEmail, string customerName, int orderId, decimal totalAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send order status updated email when admin updates order status (e.g. Confirmed, Shipped, Delivered).
        /// Not used when new status is Cancelled (use SendOrderCancelledAsync for that).
        /// </summary>
        Task SendOrderStatusUpdatedAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string previousStatus, string newStatus, CancellationToken cancellationToken = default);
    }
}
