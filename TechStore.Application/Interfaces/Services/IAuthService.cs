using System.Threading.Tasks;
using TechStore.Application.DTOs.Auth;

namespace TechStore.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(RegisterDto request);
        Task<UserDto> LoginAsync(LoginDto request);
        Task<UserDto> VerifyOtpAsync(string emailOrUsername, string code);
        Task<UserDto> ProcessExternalLoginAsync(string email, string name, string googleId);
    }
}
