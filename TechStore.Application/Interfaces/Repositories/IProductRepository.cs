using TechStore.Domain.Entities;

namespace TechStore.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        void Update(Product product);
        void Delete(Product product);

        // Specs management
        void RemoveSpecs(IEnumerable<ProductSpec> specs);

        Task SaveChangesAsync();
    }
}
