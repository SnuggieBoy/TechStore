using System.Threading.Tasks;
using TechStore.Domain.Entities;

namespace TechStore.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
}
