using TechStore.Application.DTOs.Product;
using TechStore.Application.DTOs.User;

namespace TechStore.Application.Interfaces.Services
{
    public interface IUserService
    {
        /// <summary>Get all users with pagination (Admin).</summary>
        Task<PagedResult<UserProfileDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);

        /// <summary>Get user by PublicId (Admin).</summary>
        Task<UserProfileDto> GetByPublicIdAsync(string publicId);

        /// <summary>Admin creates a new user.</summary>
        Task<UserProfileDto> CreateAsync(CreateUserDto dto);

        /// <summary>Admin updates any user.</summary>
        Task<UserProfileDto> UpdateAsync(string userPublicId, UpdateUserDto dto);

        /// <summary>Admin deletes a user (cannot delete self).</summary>
        Task DeleteAsync(string userPublicId, int currentUserId);

        /// <summary>Get own profile (any authenticated user).</summary>
        Task<UserProfileDto> GetMyProfileAsync(int userId);

        /// <summary>Update own profile (any authenticated user, cannot change role).</summary>
        Task<UserProfileDto> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto);
    }
}
