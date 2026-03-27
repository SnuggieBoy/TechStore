using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using TechStore.Application.DTOs.Auth;
using TechStore.Application.Interfaces.Services;
using TechStore.Domain.Entities;
using TechStore.Application.Interfaces.Repositories;

namespace TechStore.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(IUserRepository userRepository, IConfiguration configuration, IEmailService emailService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<UserDto> RegisterAsync(RegisterDto request)
        {
            // Case-insensitive check
            if (await _userRepository.GetByUsernameAsync(request.Username.Trim()) != null)
            {
                throw new ArgumentException("Username already exists");
            }

            if (await _userRepository.GetByEmailAsync(request.Email.Trim().ToLower()) != null)
            {
                throw new ArgumentException("Email already exists");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Generate 6-digit OTP
            string otpCode = new Random().Next(100000, 999999).ToString();

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = passwordHash,
                FullName = request.FullName,
                Phone = request.Phone,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow,
                OtpCode = otpCode,
                OtpExpiry = DateTime.UtcNow.AddMinutes(5),
                IsEmailConfirmed = false
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Gửi email OTP ngay sau khi đăng ký
            Console.WriteLine($"🔑 DEBUG_OTP: Mã OTP cho {user.Email} là: {otpCode}");
            await _emailService.SendOtpEmailAsync(user.Email, otpCode);

            // Không cần generate token ở bước này vì user chưa verify
            return MapToDto(user, string.Empty);
        }

        public async Task<UserDto> VerifyOtpAsync(string emailOrUsername, string code)
        {
            var user = await _userRepository.GetByEmailAsync(emailOrUsername) 
                       ?? await _userRepository.GetByUsernameAsync(emailOrUsername);
                       
            if (user == null || user.OtpCode != code)
            {
                throw new ArgumentException("Mã OTP không chính xác.");
            }

            if (user.OtpExpiry < DateTime.UtcNow)
            {
                throw new ArgumentException("Mã OTP đã hết hạn.");
            }

            // Success: Activate user and clear OTP
            user.IsEmailConfirmed = true;
            user.OtpCode = null;
            user.OtpExpiry = null;
            
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return MapToDto(user, token);
        }

        public async Task<UserDto> LoginAsync(LoginDto request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username.Trim());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var token = GenerateJwtToken(user);

            return MapToDto(user, token);
        }

        public async Task<UserDto> ProcessExternalLoginAsync(string email, string name, string googleId)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // Create new user for first-time Google login
                user = new User
                {
                    Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString().Substring(0, 5),
                    Email = email,
                    FullName = name,
                    Role = "Customer",
                    IsEmailConfirmed = true, // Google emails are already verified
                    GoogleId = googleId,
                    Provider = "Google",
                    PasswordHash = Guid.NewGuid().ToString() // Dummy password
                };

                await _userRepository.AddAsync(user);
            }
            else
            {
                // Update existing user with Google info if missing
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    user.Provider = "Google";
                    user.IsEmailConfirmed = true;
                    await _userRepository.UpdateAsync(user);
                }
            }

            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return MapToDto(user, token);
        }

        private UserDto MapToDto(User user, string token)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username?.Trim() ?? "",
                Email = user.Email?.Trim() ?? "",
                FullName = user.FullName?.Trim(),
                Role = user.Role?.Trim() ?? "",
                Phone = user.Phone?.Trim(),
                Token = token,
                IsEmailConfirmed = user.IsEmailConfirmed
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not set. Set JwtSettings:SecretKey in appsettings or JWT_SECRET env var.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username?.Trim() ?? ""),
                new Claim(ClaimTypes.Email, user.Email?.Trim() ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.Trim() ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["AccessTokenExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
