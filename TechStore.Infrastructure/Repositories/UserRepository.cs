using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Domain.Entities;
using TechStore.Infrastructure.Persistence;

namespace TechStore.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly TechStoreDbContext _context;

        public UserRepository(TechStoreDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
