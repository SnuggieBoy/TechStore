using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using TechStore.API.Models;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Services
{
    /// <summary>
    /// Cloudinary implementation for image uploads.
    /// </summary>
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> options)
        {
            var config = options.Value;
            _cloudinary = new Cloudinary(new Account(config.CloudName, config.ApiKey, config.ApiSecret));
        }

        public async Task<string> UploadImageAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl.ToString();
        }
    }
}
