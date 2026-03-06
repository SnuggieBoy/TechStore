using System.Threading.Tasks;
using TechStore.Domain.Entities;

namespace TechStore.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByPublicIdAsync(Guid publicId);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task<bool> HasOrdersAsync(int userId);
        Task SaveChangesAsync();
    }
}
