using Microsoft.EntityFrameworkCore;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Domain.Entities;
using TechStore.Infrastructure.Persistence;

namespace TechStore.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly TechStoreDbContext _context;

        public OrderRepository(TechStoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public void Update(Order order)
        {
            _context.Orders.Update(order);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Fix #8: Begin a DB transaction for atomic stock operations.
        /// </summary>
        public async Task<IAsyncDisposable> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
