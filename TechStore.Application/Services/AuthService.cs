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
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
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
