using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Application.Interfaces.Services;
using TechStore.Infrastructure.Persistence;
using TechStore.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly TechStoreDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;

        public OtpController(TechStoreDbContext context, IEmailService emailService, IAuthService authService)
        {
            _context = context;
            _emailService = emailService;
            _authService = authService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
        {
            // Tìm user bằng Email hoặc Username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Email);
            if (user == null)
            {
                return NotFound(new { message = "Email hoặc Tên đăng nhập không tồn tại." });
            }

            // Dùng email thật của user để gửi
            string targetEmail = user.Email;

            // Generate 6-digit OTP
            string otpCode = new Random().Next(100000, 999999).ToString();
            
            user.OtpCode = otpCode;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);

            await _context.SaveChangesAsync();

            try 
            {
                await _emailService.SendOtpEmailAsync(targetEmail, otpCode);
                return Ok(new { 
                    message = "Mã OTP đã được gửi đến email của bạn.",
                    email = targetEmail // Trả về email thật để UI hiển thị nếu muốn
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gửi email: " + ex.Message });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _authService.VerifyOtpAsync(request.Email, request.Code);
                return Ok(new { 
                    message = "Xác thực OTP thành công. Tài khoản của bạn đã được kích hoạt.",
                    data = result,
                    token = result.Token
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    public class OtpRequest { public string Email { get; set; } = string.Empty; }
    public class VerifyOtpRequest 
    { 
        public string Email { get; set; } = string.Empty; 
        public string Code { get; set; } = string.Empty;
    }
}
