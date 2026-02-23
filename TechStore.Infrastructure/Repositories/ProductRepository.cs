using Microsoft.EntityFrameworkCore;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Domain.Entities;
using TechStore.Infrastructure.Persistence;

namespace TechStore.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly TechStoreDbContext _context;

        public ProductRepository(TechStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Specs)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Specs)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }

        public void Delete(Product product)
        {
            _context.Products.Remove(product);
        }

        public void RemoveSpecs(IEnumerable<ProductSpec> specs)
        {
            _context.ProductSpecs.RemoveRange(specs);
        }

        /// <summary>
        /// FIX #9: Check if a product has any order items (prevent deletion).
        /// </summary>
        public async Task<bool> HasOrderItemsAsync(int productId)
        {
            return await _context.OrderItems.AsNoTracking().AnyAsync(oi => oi.ProductId == productId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
