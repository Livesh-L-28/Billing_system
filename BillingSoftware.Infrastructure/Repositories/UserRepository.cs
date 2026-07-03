using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Database;

namespace BillingSoftware.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(BillingDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }
}
