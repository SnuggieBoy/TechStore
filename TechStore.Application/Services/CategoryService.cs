using System;
using TechStore.Application.DTOs.Category;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Application.Interfaces.Services;
using TechStore.Domain.Entities;

namespace TechStore.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(c => MapToDto(c)).ToList();
        }

        public async Task<CategoryDto> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Category with id {id} not found");

            return MapToDto(category);
        }

        public async Task<CategoryDto> GetByPublicIdAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId) || !Guid.TryParse(publicId, out var guid))
                throw new KeyNotFoundException("Category not found");
            var category = await _categoryRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Category not found");
            return MapToDto(category);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            // Check duplicate name
            var existing = await _categoryRepository.GetByNameAsync(dto.Name.Trim());
            if (existing != null)
                throw new ArgumentException($"Category '{dto.Name}' already exists");

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return MapToDto(category);
        }

        public async Task<CategoryDto> UpdateAsync(string categoryPublicId, UpdateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(categoryPublicId) || !Guid.TryParse(categoryPublicId, out var guid))
                throw new KeyNotFoundException("Category not found");
            var category = await _categoryRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Category not found");

            // Check duplicate name (exclude current)
            var existing = await _categoryRepository.GetByNameAsync(dto.Name.Trim());
            if (existing != null && existing.Id != category.Id)
                throw new ArgumentException($"Category '{dto.Name}' already exists");

            category.Name = dto.Name.Trim();
            category.Description = dto.Description;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            return MapToDto(category);
        }

        public async Task DeleteAsync(string categoryPublicId)
        {
            if (string.IsNullOrWhiteSpace(categoryPublicId) || !Guid.TryParse(categoryPublicId, out var guid))
                throw new KeyNotFoundException("Category not found");
            var category = await _categoryRepository.GetByPublicIdAsync(guid)
                ?? throw new KeyNotFoundException("Category not found");

            if (category.Products != null && category.Products.Count > 0)
                throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it has {category.Products.Count} product(s)");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();
        }

        private static CategoryDto MapToDto(Category c)
        {
            return new CategoryDto
            {
                Id = c.Id,
                PublicId = c.PublicId.ToString(),
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products?.Count ?? 0
            };
        }
    }
}
