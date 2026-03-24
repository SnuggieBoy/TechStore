using System;
using TechStore.Domain.Entities;

namespace TechStore.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetByPublicIdAsync(Guid publicId);
        Task AddAsync(Order order);
        void Update(Order order);
        Task SaveChangesAsync();
        Task<IAsyncDisposable> BeginTransactionAsync();
    }
}
