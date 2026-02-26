namespace TechStore.Application.Interfaces.Services
{
    /// <summary>
    /// Service for uploading images to Cloudinary.
    /// </summary>
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads an image stream to Cloudinary and returns the secure URL.
        /// </summary>
        Task<string> UploadImageAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    }
}
