using System.Threading.Tasks;

namespace BillingSoftware.Application.Services;

public interface IAuditLogger
{
    Task LogActivityAsync(string action, string moduleName, string details);
}
