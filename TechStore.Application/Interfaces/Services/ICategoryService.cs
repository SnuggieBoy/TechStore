using TechStore.Application.DTOs.Category;

namespace TechStore.Application.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<CategoryDto> GetByIdAsync(int id);
        Task<CategoryDto> GetByPublicIdAsync(string publicId);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto> UpdateAsync(string categoryPublicId, UpdateCategoryDto dto);
        Task DeleteAsync(string categoryPublicId);
    }
}
