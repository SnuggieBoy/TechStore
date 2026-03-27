using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechStore.Application.DTOs.Common;
using TechStore.Application.DTOs.Product;
using TechStore.Application.DTOs.User;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[Backend] Claim NameIdentifier: '{userIdStr}'");
            
            // Nếu không thấy NameIdentifier, thử tìm "sub" (tên gốc của JWT)
            if (string.IsNullOrEmpty(userIdStr))
            {
                userIdStr = User.FindFirstValue("sub");
                Console.WriteLine($"[Backend] Claim 'sub': '{userIdStr}'");
            }

            if (string.IsNullOrEmpty(userIdStr))
            {
                throw new UnauthorizedAccessException("Không tìm thấy User ID trong Token.");
            }

            return int.Parse(userIdStr);
        }

        // ======================== CUSTOMER ENDPOINTS ========================

        /// <summary>
        /// Get my own profile (any authenticated user).
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile()
        {
            var profile = await _userService.GetMyProfileAsync(GetUserId());
            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile));
        }

        /// <summary>
        /// Update my own profile (any authenticated user). Cannot change role or username.
        /// </summary>
        [HttpPut("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto dto)
        {
            try
            {
                var profile = await _userService.UpdateMyProfileAsync(GetUserId(), dto);
                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Profile updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        // ======================== ADMIN ENDPOINTS ========================

        /// <summary>
        /// Get all users with pagination and search (Admin only).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _userService.GetAllAsync(page, pageSize, search);
            return Ok(ApiResponse<PagedResult<UserProfileDto>>.SuccessResponse(result));
        }

        /// <summary>
        /// Get user by PublicId (Admin only).
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var user = await _userService.GetByPublicIdAsync(id);
                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(user));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Create a new user (Admin only). Can assign role.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var user = await _userService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = user.PublicId },
                    ApiResponse<UserProfileDto>.SuccessResponse(user, "User created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Update a user (Admin only). Password is optional.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _userService.UpdateAsync(id, dto);
                return Ok(ApiResponse<UserProfileDto>.SuccessResponse(user, "User updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Delete a user (Admin only). Cannot delete yourself or users with orders.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _userService.DeleteAsync(id, GetUserId());
                return Ok(ApiResponse<object>.SuccessResponse(null!, "User deleted successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }
    }
}
