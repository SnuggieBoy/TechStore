using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    /// <summary>
    /// Email service using SMTP, configured via EmailSettings (same pattern as Synergy_BE).
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string? _smtpHost;
        private readonly int _smtpPort;
        private readonly string? _smtpUser;
        private readonly string? _smtpPassword;
        private readonly string? _fromEmail;
        private readonly string? _fromName;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _smtpHost = _configuration["EmailSettings:SmtpHost"];
            _smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
            _smtpUser = _configuration["EmailSettings:SmtpUser"];
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            _fromEmail = _configuration["EmailSettings:FromEmail"];
            _fromName = _configuration["EmailSettings:FromName"] ?? "TechStore";
            _enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var ssl) ? ssl : true;
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string status, CancellationToken cancellationToken = default)
        {
            var subject = $"[TechStore] Xác nhận đơn hàng #{orderId}";
            var body = $@"
Xin chào {customerName},

Đơn hàng #{orderId} của bạn đã được đặt thành công.

Tổng tiền: {totalAmount:N0} VNĐ
Trạng thái: {status}

Cảm ơn bạn đã mua sắm tại TechStore!
";
            await SendEmailAsync(toEmail, subject, body.Trim(), false, cancellationToken);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, bool isBodyHtml, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser))
                {
                    _logger.LogWarning("Email service not configured. Email would be sent to: {Email}", toEmail);
                    _logger.LogInformation("Email Subject: {Subject}. Configure EmailSettings in appsettings (SmtpHost, SmtpUser).", subject);
                    return;
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogDebug("Email skipped: no recipient.");
                    return;
                }

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail ?? _smtpUser ?? "noreply@techstore.com", _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isBodyHtml
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage, ct);
                _logger.LogInformation("Email sent successfully to {Email} for order confirmation", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Email}. Subject: {Subject}", toEmail, subject);
            }
        }
    }
}
