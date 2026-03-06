using System;
using BCrypt.Net;
using TechStore.Application.DTOs.Product;
using TechStore.Application.DTOs.User;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Application.Interfaces.Services;
using TechStore.Domain.Entities;

namespace TechStore.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<PagedResult<UserProfileDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            page = Math.Max(1, page);

            var users = await _userRepository.GetAllAsync();
            var query = users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(s)));
            }

            var totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResult<UserProfileDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserProfileDto> GetByPublicIdAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId) || !Guid.TryParse(publicId, out var guid))
                throw new KeyNotFoundException("User not found");

            var user = await _userRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("User not found");

            return MapToDto(user);
        }

        public async Task<UserProfileDto> CreateAsync(CreateUserDto dto)
        {
            // Check duplicate username (case-insensitive)
            if (await _userRepository.GetByUsernameAsync(dto.Username.Trim()) != null)
                throw new ArgumentException("Username already exists");

            // Check duplicate email
            if (await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLower()) != null)
                throw new ArgumentException("Email already exists");

            var user = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                Role = dto.Role,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Reload to get navigation properties
            var created = await _userRepository.GetByIdAsync(user.Id);
            return MapToDto(created!);
        }

        public async Task<UserProfileDto> UpdateAsync(string userPublicId, UpdateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(userPublicId) || !Guid.TryParse(userPublicId, out var guid))
                throw new KeyNotFoundException("User not found");

            var user = await _userRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("User not found");

            // Check duplicate username (exclude current)
            var existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username.Trim());
            if (existingByUsername != null && existingByUsername.Id != user.Id)
                throw new ArgumentException("Username already exists");

            // Check duplicate email (exclude current)
            var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLower());
            if (existingByEmail != null && existingByEmail.Id != user.Id)
                throw new ArgumentException("Email already exists");

            user.Username = dto.Username.Trim();
            user.Email = dto.Email.Trim().ToLower();
            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.Phone = dto.Phone;
            user.Address = dto.Address;

            // Only update password if provided
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task DeleteAsync(string userPublicId, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(userPublicId) || !Guid.TryParse(userPublicId, out var guid))
                throw new KeyNotFoundException("User not found");

            var user = await _userRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("User not found");

            // Cannot delete yourself
            if (user.Id == currentUserId)
                throw new InvalidOperationException("You cannot delete your own account");

            // Cannot delete if user has orders
            if (await _userRepository.HasOrdersAsync(user.Id))
                throw new InvalidOperationException(
                    $"Cannot delete user '{user.Username}' because they have existing orders. Consider disabling the account instead.");

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<UserProfileDto> GetMyProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found");

            return MapToDto(user);
        }

        public async Task<UserProfileDto> UpdateMyProfileAsync(int userId, UpdateMyProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found");

            // Check duplicate email (exclude current)
            var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email.Trim().ToLower());
            if (existingByEmail != null && existingByEmail.Id != user.Id)
                throw new ArgumentException("Email already exists");

            user.Email = dto.Email.Trim().ToLower();
            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.Address = dto.Address;

            // Only update password if provided
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return MapToDto(user);
        }

        private static UserProfileDto MapToDto(User u)
        {
            return new UserProfileDto
            {
                Id = u.Id,
                PublicId = u.PublicId.ToString(),
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                Phone = u.Phone,
                Address = u.Address,
                CreatedAt = u.CreatedAt,
                OrderCount = u.Orders?.Count ?? 0
            };
        }
    }
}
