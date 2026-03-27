using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_configuration["EmailSettings:FromName"], _configuration["EmailSettings:FromEmail"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpHost"],
                    int.Parse(_configuration["EmailSettings:SmtpPort"]),
                    MailKit.Security.SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:SmtpUser"],
                    _configuration["EmailSettings:SmtpPassword"]
                );
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log detailed error to console to help the user
                Console.WriteLine($"❌ EMAIL_ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ INNER_ERROR: {ex.InnerException.Message}");
                }
                throw; // Rethrow to let the middleware handle it
            }
        }

        public async Task SendOtpEmailAsync(string to, string otpCode)
        {
            string subject = "Xác nhận mã OTP - TechStore";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Chào mừng bạn đến với TechStore</h2>
                    <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #4A90E2;'>{otpCode}</strong></p>
                    <p>Mã này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";
            
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendOrderConfirmationAsync(string to, string fullName, int orderId, decimal amount, string paymentStatus, string orderStatus)
        {
            string subject = $"Xác nhận đơn hàng #{orderId} - TechStore";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Cảm ơn bạn đã đặt hàng tại TechStore</h2>
                    <p>Chào <strong>{fullName}</strong>,</p>
                    <p>Đơn hàng <strong>#{orderId}</strong> của bạn đã được tiếp nhận.</p>
                    <p>Tổng tiền: <strong>{amount:N0} VNĐ</strong></p>
                    <p>Trạng thái thanh toán: <strong>{paymentStatus}</strong></p>
                    <p>Trạng thái đơn hàng: <strong>{orderStatus}</strong></p>
                    <p>Chúng tôi sẽ sớm giao hàng cho bạn.</p>
                </div>";
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendOrderStatusUpdatedAsync(string to, string fullName, int orderId, decimal amount, string type, string fromStatus, string toStatus)
        {
            string subject = $"Cập nhật trạng thái {type} đơn hàng #{orderId}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Cập nhật thông tin đơn hàng</h2>
                    <p>Chào <strong>{fullName}</strong>,</p>
                    <p>Trạng thái <strong>{type}</strong> của đơn hàng <strong>#{orderId}</strong> đã thay đổi:</p>
                    <p>Từ: <span style='color: #888;'>{fromStatus}</span> &rarr; <strong>{toStatus}</strong></p>
                    <p>Tổng tiền: <strong>{amount:N0} VNĐ</strong></p>
                </div>";
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendOrderCancelledAsync(string to, string fullName, int orderId, decimal amount)
        {
            string subject = $"Đơn hàng #{orderId} đã bị hủy - TechStore";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #E74C3C;'>Thông báo hủy đơn hàng</h2>
                    <p>Chào <strong>{fullName}</strong>,</p>
                    <p>Đơn hàng <strong>#{orderId}</strong> của bạn đã được hủy thành công.</p>
                    <p>Số tiền hoàn lại/giải phóng: <strong>{amount:N0} VNĐ</strong></p>
                </div>";
            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPaymentSuccessAsync(string to, string fullName, int orderId, decimal amount)
        {
            string subject = $"Thanh toán thành công đơn hàng #{orderId} - TechStore";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #27AE60;'>Xác nhận thanh toán thành công</h2>
                    <p>Chào <strong>{fullName}</strong>,</p>
                    <p>Chúng tôi đã nhận được thanh toán cho đơn hàng <strong>#{orderId}</strong>.</p>
                    <p>Số tiền: <strong>{amount:N0} VNĐ</strong></p>
                    <p>Đơn hàng đang trong quá trình xử lý để vận chuyển.</p>
                </div>";
            await SendEmailAsync(to, subject, body);
        }
    }
}

