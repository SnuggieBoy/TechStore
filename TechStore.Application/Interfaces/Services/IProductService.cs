using TechStore.Application.DTOs.Product;

namespace TechStore.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductDto> GetByIdAsync(int id);
        Task<ProductDto> GetByPublicIdAsync(string publicId);
        Task<ProductDto> CreateAsync(CreateProductDto dto);
        Task<ProductDto> UpdateAsync(string productPublicId, UpdateProductDto dto);
        Task DeleteAsync(string productPublicId);
    }
}
