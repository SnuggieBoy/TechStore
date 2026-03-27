using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using TechStore.Application.DTOs.Auth;
using TechStore.Application.DTOs.Common;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new user account.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(ApiResponse<UserDto>.SuccessResponse(result, "Registration successful"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Login with username and password.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(ApiResponse<UserDto>.SuccessResponse(result, "Login successful"));
            }
            catch (Exception ex)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Login Google.
        /// </summary>
        [HttpGet("google-login")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult GoogleLogin()
        {
            // Trình duyệt sẽ mở link này, sau đó .NET sẽ dẫn sang Google
            // Đảm bảo RedirectUri khớp chính xác với những gì đã đăng ký ở Google Console (thường là lowercase)
            var properties = new AuthenticationProperties { RedirectUri = "/api/auth/google-callback" };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Login Google Callback.
        /// </summary>
        [HttpGet("google-callback")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)] // Returns HTML Success Page
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GoogleCallback()
        {
            // Sau khi user login Google xong, Google gọi về đây
            var authenticateResult = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return BadRequest("Google authentication failed.");

            var email = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var googleId = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email not found in Google account.");

            // Lưu/Cập nhật user vào Database và tạo JWT Token
            var userDto = await _authService.ProcessExternalLoginAsync(email, name ?? "", googleId ?? "");

            Console.WriteLine($"🔑 [Backend] Token generated: {userDto.Token?.Substring(0, 10)}...");

            // RẤT QUAN TRỌNG: Điều hướng về App Flutter thông qua Deep Link
            var redirectUrl = $"techstore://auth?token={userDto.Token}";
            Console.WriteLine($"🚀 [Backend] Sending Success Page with link to: {redirectUrl.Substring(0, 30)}...");
            
            // Trả về trang HTML để "ép" trình duyệt mở app hoặc cho user nhấn thủ công
            return Content($@"
                <html>
                    <head>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: sans-serif; text-align: center; padding: 50px; background: #f4f7fe; }}
                            .card {{ background: white; padding: 30px; border-radius: 20px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); display: inline-block; }}
                            .icon {{ font-size: 50px; color: #4CAF50; margin-bottom: 20px; }}
                            h2 {{ color: #333; }}
                            p {{ color: #666; margin-bottom: 30px; }}
                            .btn {{ background: #5c6bc0; color: white; padding: 15px 30px; text-decoration: none; border-radius: 12px; font-weight: bold; display: inline-block; transition: 0.3s; }}
                            .btn:hover {{ background: #3f51b5; }}
                        </style>
                        <script>
                            // Tự động chuyển hướng sau 1 giây
                            setTimeout(function() {{
                                window.location.href = '{redirectUrl}';
                            }}, 1000);
                        </script>
                    </head>
                    <body>
                        <div class='card'>
                            <div class='icon'>✅</div>
                            <h2>Đăng nhập thành công!</h2>
                            <p>Đang quay trở lại ứng dụng TechStore...</p>
                            <a href='{redirectUrl}' class='btn'>Mở TechStore ngay</a>
                            <p style='font-size: 12px; color: #999; margin-top: 20px;'>Nếu ứng dụng không tự mở, vui lòng nhấn nút phía trên.</p>
                        </div>
                    </body>
                </html>", "text/html; charset=utf-8");
        }
    }
}
