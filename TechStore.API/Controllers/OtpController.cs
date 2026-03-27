using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.Application.Interfaces.Services;
using TechStore.Infrastructure.Persistence;
using TechStore.Domain.Entities;
using TechStore.Application.DTOs.Auth;
using TechStore.Application.DTOs.Common;
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

        /// <summary>
        /// Send OTP to user.
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
        {
            // Tìm user bằng Email hoặc Username
            string requestEmail = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == requestEmail || u.Username == requestEmail);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Email hoặc Tên đăng nhập không tồn tại."));
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
                Console.WriteLine($"🔑 DEBUG_OTP (Resend): Mã OTP cho {targetEmail} là: {otpCode}");
                await _emailService.SendOtpEmailAsync(targetEmail, otpCode);
                return Ok(ApiResponse<object>.SuccessResponse(new { email = targetEmail }, "Mã OTP đã được gửi đến email của bạn."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi khi gửi email: " + ex.Message));
            }
        }

        /// <summary>
        /// Verify OTP.
        /// </summary>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var result = await _authService.VerifyOtpAsync(request.Email, request.Code);
                return Ok(ApiResponse<UserDto>.SuccessResponse(result, "Xác thực OTP thành công. Tài khoản của bạn đã được kích hoạt."));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi hệ thống: " + ex.Message));
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
