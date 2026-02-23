using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    /// <summary>
    /// Email service using SMTP, configured via EmailSettings (same pattern as Synergy_BE).
    /// Professional HTML templates for order and payment emails.
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
            var totalFormatted = totalAmount.ToString("N0");
            var statusBadge = status == "Pending" ? "#f59e0b" : "#6b7280"; // amber for Pending
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;font-family:""Segoe UI"",Arial,sans-serif;background:#f1f5f9;padding:24px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
    <div style='background:linear-gradient(135deg,#1e40af 0%,#3b82f6 100%);color:#fff;padding:28px 24px;text-align:center'>
      <h1 style='margin:0;font-size:24px;font-weight:700'>🛒 TechStore</h1>
      <p style='margin:8px 0 0;opacity:0.95;font-size:14px'>Xác nhận đơn hàng</p>
    </div>
    <div style='padding:28px 24px'>
      <p style='margin:0 0 20px;font-size:16px;color:#334155'>Xin chào <strong>{EscapeHtml(customerName)}</strong>,</p>
      <p style='margin:0 0 24px;font-size:15px;line-height:1.6;color:#475569'>Đơn hàng của bạn đã được tiếp nhận thành công. Dưới đây là thông tin đơn hàng:</p>
      <table style='width:100%;border-collapse:collapse;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden'>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b;width:140px'>Mã đơn hàng</td><td style='padding:14px 16px;font-size:14px;font-weight:600;color:#1e293b'>#{orderId}</td></tr>
        <tr><td style='padding:14px 16px;font-size:13px;color:#64748b'>Tổng tiền</td><td style='padding:14px 16px;font-size:16px;font-weight:700;color:#1e40af'>{totalFormatted} VNĐ</td></tr>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b'>Trạng thái</td><td style='padding:14px 16px'><span style='display:inline-block;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;background:{statusBadge};color:#fff'>{EscapeHtml(status)}</span></td></tr>
      </table>
      <p style='margin:24px 0 0;font-size:14px;color:#64748b'>Bạn có thể thanh toán đơn hàng này trong app để chuyển trạng thái sang <strong>Paid</strong>.</p>
    </div>
    <div style='background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0'>
      <p style='margin:0;font-size:12px;color:#94a3b8'>© TechStore. Email tự động, vui lòng không trả lời.</p>
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
        }

        public async Task SendPaymentSuccessAsync(string toEmail, string customerName, int orderId, decimal totalAmount, CancellationToken cancellationToken = default)
        {
            var subject = $"[TechStore] Thanh toán thành công - Đơn hàng #{orderId}";
            var totalFormatted = totalAmount.ToString("N0");
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;font-family:""Segoe UI"",Arial,sans-serif;background:#f1f5f9;padding:24px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
    <div style='background:linear-gradient(135deg,#047857 0%,#10b981 100%);color:#fff;padding:28px 24px;text-align:center'>
      <h1 style='margin:0;font-size:24px;font-weight:700'>✓ TechStore</h1>
      <p style='margin:8px 0 0;opacity:0.95;font-size:14px'>Thanh toán thành công</p>
    </div>
    <div style='padding:28px 24px'>
      <p style='margin:0 0 20px;font-size:16px;color:#334155'>Xin chào <strong>{EscapeHtml(customerName)}</strong>,</p>
      <p style='margin:0 0 24px;font-size:15px;line-height:1.6;color:#475569'>Đơn hàng <strong>#{orderId}</strong> của bạn đã được thanh toán thành công. Cảm ơn bạn đã tin tưởng TechStore!</p>
      <table style='width:100%;border-collapse:collapse;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden'>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b;width:140px'>Mã đơn hàng</td><td style='padding:14px 16px;font-size:14px;font-weight:600;color:#1e293b'>#{orderId}</td></tr>
        <tr><td style='padding:14px 16px;font-size:13px;color:#64748b'>Số tiền đã thanh toán</td><td style='padding:14px 16px;font-size:16px;font-weight:700;color:#047857'>{totalFormatted} VNĐ</td></tr>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b'>Trạng thái</td><td style='padding:14px 16px'><span style='display:inline-block;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;background:#10b981;color:#fff'>Paid</span></td></tr>
      </table>
      <p style='margin:24px 0 0;font-size:14px;color:#64748b'>Chúng tôi sẽ xử lý và giao hàng đến bạn trong thời gian sớm nhất.</p>
    </div>
    <div style='background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0'>
      <p style='margin:0;font-size:12px;color:#94a3b8'>© TechStore. Email tự động, vui lòng không trả lời.</p>
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
        }

        public async Task SendOrderCancelledAsync(string toEmail, string customerName, int orderId, decimal totalAmount, CancellationToken cancellationToken = default)
        {
            var subject = $"[TechStore] Đơn hàng #{orderId} đã được hủy";
            var totalFormatted = totalAmount.ToString("N0");
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;font-family:""Segoe UI"",Arial,sans-serif;background:#f1f5f9;padding:24px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
    <div style='background:linear-gradient(135deg,#b91c1c 0%,#dc2626 100%);color:#fff;padding:28px 24px;text-align:center'>
      <h1 style='margin:0;font-size:24px;font-weight:700'>✕ TechStore</h1>
      <p style='margin:8px 0 0;opacity:0.95;font-size:14px'>Thông báo hủy đơn hàng</p>
    </div>
    <div style='padding:28px 24px'>
      <p style='margin:0 0 20px;font-size:16px;color:#334155'>Xin chào <strong>{EscapeHtml(customerName)}</strong>,</p>
      <p style='margin:0 0 24px;font-size:15px;line-height:1.6;color:#475569'>Đơn hàng <strong>#{orderId}</strong> của bạn đã được hủy thành công. Tồn kho sản phẩm đã được hoàn lại.</p>
      <table style='width:100%;border-collapse:collapse;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden'>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b;width:140px'>Mã đơn hàng</td><td style='padding:14px 16px;font-size:14px;font-weight:600;color:#1e293b'>#{orderId}</td></tr>
        <tr><td style='padding:14px 16px;font-size:13px;color:#64748b'>Tổng tiền (đã hủy)</td><td style='padding:14px 16px;font-size:16px;font-weight:700;color:#64748b'>{totalFormatted} VNĐ</td></tr>
        <tr style='background:#fef2f2'><td style='padding:14px 16px;font-size:13px;color:#64748b'>Trạng thái</td><td style='padding:14px 16px'><span style='display:inline-block;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;background:#dc2626;color:#fff'>Cancelled</span></td></tr>
      </table>
      <p style='margin:24px 0 0;font-size:14px;color:#64748b'>Nếu bạn không thực hiện hủy đơn, vui lòng liên hệ hỗ trợ. Cảm ơn bạn đã quan tâm đến TechStore!</p>
    </div>
    <div style='background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0'>
      <p style='margin:0;font-size:12px;color:#94a3b8'>© TechStore. Email tự động, vui lòng không trả lời.</p>
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
        }

        public async Task SendOrderStatusUpdatedAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string previousStatus, string newStatus, CancellationToken cancellationToken = default)
        {
            var subject = $"[TechStore] Cập nhật đơn hàng #{orderId} - {EscapeHtml(newStatus)}";
            var totalFormatted = totalAmount.ToString("N0");
            // Badge color by new status (real-world: Confirmed=blue, Shipped=purple, Delivered=green)
            var newBadgeColor = newStatus switch
            {
                "Confirmed" => "#2563eb",
                "Shipped" => "#7c3aed",
                "Delivered" => "#059669",
                _ => "#64748b"
            };
            var statusMessage = newStatus switch
            {
                "Confirmed" => "Đơn hàng đã được xác nhận. Chúng tôi đang chuẩn bị và sẽ sớm giao đến bạn.",
                "Shipped" => "Đơn hàng đang được giao. Bạn sẽ nhận hàng trong thời gian sớm nhất.",
                "Delivered" => "Đơn hàng đã giao thành công. Cảm ơn bạn đã mua sắm tại TechStore!",
                _ => "Trạng thái đơn hàng của bạn đã được cập nhật."
            };
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;font-family:""Segoe UI"",Arial,sans-serif;background:#f1f5f9;padding:24px'>
  <div style='max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07)'>
    <div style='background:linear-gradient(135deg,#4f46e5 0%,#6366f1 100%);color:#fff;padding:28px 24px;text-align:center'>
      <h1 style='margin:0;font-size:24px;font-weight:700'>📦 TechStore</h1>
      <p style='margin:8px 0 0;opacity:0.95;font-size:14px'>Cập nhật đơn hàng</p>
    </div>
    <div style='padding:28px 24px'>
      <p style='margin:0 0 20px;font-size:16px;color:#334155'>Xin chào <strong>{EscapeHtml(customerName)}</strong>,</p>
      <p style='margin:0 0 24px;font-size:15px;line-height:1.6;color:#475569'>{EscapeHtml(statusMessage)}</p>
      <table style='width:100%;border-collapse:collapse;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden'>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b;width:140px'>Mã đơn hàng</td><td style='padding:14px 16px;font-size:14px;font-weight:600;color:#1e293b'>#{orderId}</td></tr>
        <tr><td style='padding:14px 16px;font-size:13px;color:#64748b'>Tổng tiền</td><td style='padding:14px 16px;font-size:16px;font-weight:700;color:#1e293b'>{totalFormatted} VNĐ</td></tr>
        <tr style='background:#f8fafc'><td style='padding:14px 16px;font-size:13px;color:#64748b'>Trạng thái cũ</td><td style='padding:14px 16px'><span style='display:inline-block;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;background:#94a3b8;color:#fff'>{EscapeHtml(previousStatus)}</span></td></tr>
        <tr><td style='padding:14px 16px;font-size:13px;color:#64748b'>Trạng thái mới</td><td style='padding:14px 16px'><span style='display:inline-block;padding:6px 12px;border-radius:6px;font-size:13px;font-weight:600;background:{newBadgeColor};color:#fff'>{EscapeHtml(newStatus)}</span></td></tr>
      </table>
      <p style='margin:24px 0 0;font-size:14px;color:#64748b'>Bạn có thể theo dõi đơn hàng trong app. Nếu cần hỗ trợ, vui lòng liên hệ chúng tôi.</p>
    </div>
    <div style='background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0'>
      <p style='margin:0;font-size:12px;color:#94a3b8'>© TechStore. Email tự động, vui lòng không trả lời.</p>
    </div>
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
        }

        private static string EscapeHtml(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
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

                // Sanitize recipient to prevent header injection (newlines, multiple addresses)
                var sanitizedEmail = SanitizeEmailAddress(toEmail);
                if (string.IsNullOrWhiteSpace(sanitizedEmail))
                {
                    _logger.LogWarning("Email skipped: invalid or empty recipient.");
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
                mailMessage.To.Add(sanitizedEmail);

                await smtpClient.SendMailAsync(mailMessage, ct);
                _logger.LogInformation("Email sent successfully to {Email}", sanitizedEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Email}. Subject: {Subject}", sanitizedEmail, subject);
            }
        }

        /// <summary>
        /// Sanitize email address to prevent header injection: strip newlines, trim, single address only.
        /// </summary>
        private static string? SanitizeEmailAddress(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var s = email.Trim();
            // Remove newlines/carriage returns and restrict to single line (no comma = single address)
            s = string.Join("", s.Split('\r', '\n')).Trim();
            if (string.IsNullOrEmpty(s) || s.Length > 254 || s.Contains(',') || !s.Contains('@')) return null;
            return s;
        }
    }
}
