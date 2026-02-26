using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Application.DTOs.Common;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public ImagesController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// Upload an image to Cloudinary. Returns the image URL. Admin only.
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid file type. Allowed: JPEG, PNG, GIF, WebP"));

            if (file.Length > 5 * 1024 * 1024) // 5MB
                return BadRequest(ApiResponse<object>.ErrorResponse("File too large. Max 5MB"));

            await using var stream = file.OpenReadStream();
            var imageUrl = await _cloudinaryService.UploadImageAsync(stream, file.FileName);
            return Ok(imageUrl);
        }
    }
}
