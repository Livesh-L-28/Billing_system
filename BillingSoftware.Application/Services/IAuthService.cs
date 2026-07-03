using System;
using System.Threading.Tasks;

namespace BillingSoftware.Application.Services;

public interface IAuthService
{
    Task<bool> AuthenticateAsync(string username, string password, Guid financialYearId);
    Task LogoutAsync();
    bool HasPermission(string permission);
}
