using TechStore.Application.DTOs.Product;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Application.Interfaces.Services;
using TechStore.Domain.Entities;

namespace TechStore.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<PagedResult<ProductDto>> GetAllAsync(ProductFilterDto filter)
        {
            var products = await _productRepository.GetAllAsync();
            var query = products.AsQueryable();

            // Search by name or description
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)));
            }

            // Filter by category
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            // Filter by price range
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                "price" => filter.SortDescending
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),
                _ => filter.SortDescending
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt)
            };

            var totalCount = query.Count();

            // Pagination
            var items = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => MapToDto(p))
                .ToList();

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProductDto> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with id {id} not found");

            return MapToDto(product);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId)
                ?? throw new KeyNotFoundException($"Category with id {dto.CategoryId} not found");

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            // Add specs
            if (dto.Specs != null && dto.Specs.Count > 0)
            {
                product.Specs = dto.Specs.Select(s => new ProductSpec
                {
                    SpecKey = s.SpecKey.Trim(),
                    SpecValue = s.SpecValue.Trim()
                }).ToList();
            }

            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            // Reload to get Category name
            var created = await _productRepository.GetByIdAsync(product.Id);
            return MapToDto(created!);
        }

        public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with id {id} not found");

            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId)
                ?? throw new KeyNotFoundException($"Category with id {dto.CategoryId} not found");

            product.Name = dto.Name.Trim();
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.ImageUrl = dto.ImageUrl;

            // Update specs: remove old, add new
            if (product.Specs != null && product.Specs.Count > 0)
            {
                _productRepository.RemoveSpecs(product.Specs);
            }

            if (dto.Specs != null && dto.Specs.Count > 0)
            {
                product.Specs = dto.Specs.Select(s => new ProductSpec
                {
                    ProductId = id,
                    SpecKey = s.SpecKey.Trim(),
                    SpecValue = s.SpecValue.Trim()
                }).ToList();
            }

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            var updated = await _productRepository.GetByIdAsync(id);
            return MapToDto(updated!);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Product with id {id} not found");

            // FIX #9: Block deletion if product has order history
            if (await _productRepository.HasOrderItemsAsync(id))
                throw new InvalidOperationException(
                    $"Cannot delete product '{product.Name}' because it has existing order records. Consider setting stock to 0 instead.");

            _productRepository.Delete(product);
            await _productRepository.SaveChangesAsync();
        }

        private static ProductDto MapToDto(Product p)
        {
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                Specs = p.Specs?.Select(s => new ProductSpecDto
                {
                    Id = s.Id,
                    SpecKey = s.SpecKey,
                    SpecValue = s.SpecValue
                }).ToList() ?? new List<ProductSpecDto>()
            };
        }
    }
}
