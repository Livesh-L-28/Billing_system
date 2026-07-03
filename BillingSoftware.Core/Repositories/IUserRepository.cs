using System.Threading.Tasks;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Core.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}
