using System.ComponentModel.DataAnnotations;

namespace TechStore.Application.DTOs.User
{
    /// <summary>
    /// User profile returned by admin CRUD (no token, no password).
    /// </summary>
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// Admin creates a new user (can assign role).
    /// </summary>
    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(50)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [Required]
        [RegularExpression("^(Customer|Admin)$", ErrorMessage = "Role must be Customer or Admin")]
        public string Role { get; set; } = "Customer";

        [Phone]
        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }
    }

    /// <summary>
    /// Admin updates user info. Password is optional (only set if provided).
    /// </summary>
    public class UpdateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Leave null or empty to keep current password.</summary>
        [MinLength(6)]
        [MaxLength(50)]
        public string? Password { get; set; }

        [MaxLength(100)]
        public string? FullName { get; set; }

        [Required]
        [RegularExpression("^(Customer|Admin)$", ErrorMessage = "Role must be Customer or Admin")]
        public string Role { get; set; } = "Customer";

        [Phone]
        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }
    }

    /// <summary>
    /// Customer updates their own profile (cannot change role).
    /// </summary>
    public class UpdateMyProfileDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FullName { get; set; }

        [Phone]
        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        /// <summary>Leave null or empty to keep current password.</summary>
        [MinLength(6)]
        [MaxLength(50)]
        public string? NewPassword { get; set; }
    }
}
