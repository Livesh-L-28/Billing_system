using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Application.Services;
using BillingSoftware.Common;
using BillingSoftware.Database;

namespace BillingSoftware.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionContext _sessionContext;
    private readonly IAuditLogger _auditLogger;
    private readonly BillingDbContext _context;

    public AuthService(
        IUserRepository userRepository,
        ISessionContext sessionContext,
        IAuditLogger auditLogger,
        BillingDbContext context)
    {
        _userRepository = userRepository;
        _sessionContext = sessionContext;
        _auditLogger = auditLogger;
        _context = context;
    }

    public async Task<bool> AuthenticateAsync(string username, string password, Guid financialYearId)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        bool isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);
        if (!isValid)
        {
            await _auditLogger.LogActivityAsync("Login Failed", "Auth", $"Failed login attempt for username: {username}");
            return false;
        }

        var financialYear = await _context.FinancialYears.FindAsync(financialYearId);
        if (financialYear == null)
        {
            return false;
        }

        // Set session
        _sessionContext.CurrentUser = user;
        _sessionContext.CurrentFinancialYear = financialYear;

        // Insert Login History
        var history = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Username = user.Username,
            LoginTime = DateTime.UtcNow
        };
        await _context.LoginHistories.AddAsync(history);
        await _context.SaveChangesAsync();

        _sessionContext.CurrentLoginHistoryId = history.Id;

        await _auditLogger.LogActivityAsync("Login Success", "Auth", $"User {username} logged into financial year {financialYear.Name}");

        return true;
    }

    public async Task LogoutAsync()
    {
        if (_sessionContext.CurrentUser == null)
        {
            return;
        }

        string username = _sessionContext.CurrentUser.Username;
        var historyId = _sessionContext.CurrentLoginHistoryId;

        if (historyId.HasValue)
        {
            var history = await _context.LoginHistories.FindAsync(historyId.Value);
            if (history != null)
            {
                // Calculate today's shift summary for this user
                var today = DateTime.Today;
                var userInvoices = await _context.Invoices
                    .Where(i => i.CreatedBy == _sessionContext.CurrentUser.Id && i.InvoiceDate.Date == today)
                    .ToListAsync();

                history.LogoutTime = DateTime.UtcNow;
                history.TodayBillsCount = userInvoices.Count(i => !i.IsCancelled);
                history.TodaySalesSum = userInvoices.Where(i => !i.IsCancelled).Sum(i => i.GrandTotal);
                history.TodayGSTCollected = userInvoices.Where(i => !i.IsCancelled).Sum(i => i.CGSTTotal + i.SGSTTotal + i.IGSTTotal);

                _context.LoginHistories.Update(history);
                await _context.SaveChangesAsync();
            }
        }

        await _auditLogger.LogActivityAsync("Logout", "Auth", $"User {username} logged out.");
        _sessionContext.Clear();
    }

    public bool HasPermission(string permission)
    {
        if (_sessionContext.CurrentUser?.Role == null)
        {
            return false;
        }

        var role = _sessionContext.CurrentUser.Role;
        if (string.IsNullOrEmpty(role.Permissions))
        {
            return false;
        }

        try
        {
            var permissionsList = JsonSerializer.Deserialize<string[]>(role.Permissions);
            if (permissionsList == null) return false;
            
            return permissionsList.Contains("All") || permissionsList.Contains(permission);
        }
        catch
        {
            return false;
        }
    }
}
