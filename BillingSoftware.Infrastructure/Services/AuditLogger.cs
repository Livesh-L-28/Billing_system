using System;
using System.Threading.Tasks;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;

namespace BillingSoftware.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly BillingDbContext _context;
    private readonly ISessionContext _sessionContext;

    public AuditLogger(BillingDbContext context, ISessionContext sessionContext)
    {
        _context = context;
        _sessionContext = sessionContext;
    }

    public async Task LogActivityAsync(string action, string moduleName, string details)
    {
        var userId = _sessionContext.CurrentUser?.Id ?? Guid.Empty;
        var username = _sessionContext.CurrentUser?.Username ?? "System";

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Timestamp = DateTime.UtcNow,
            Action = action,
            ModuleName = moduleName,
            Details = details
        };

        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
