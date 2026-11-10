using System;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Application.Services;

public class SessionContext : ISessionContext
{
    public User? CurrentUser { get; set; }
    public FinancialYear? CurrentFinancialYear { get; set; }
    public Guid? CurrentLoginHistoryId { get; set; }

    public int CurrentCompanyId => (CurrentUser?.Username == "cashier" || CurrentUser?.Username == "samundeeswari") ? 2 : 1;
    public int CurrentSettingId => (CurrentUser?.Username == "cashier" || CurrentUser?.Username == "samundeeswari") ? 2 : 1;

    public void Clear()
    {
        CurrentUser = null;
        CurrentFinancialYear = null;
        CurrentLoginHistoryId = null;
    }

    public event Action<string, object?>? NavigationRequested;

    public void RequestNavigation(string viewName, object? parameter = null)
    {
        NavigationRequested?.Invoke(viewName, parameter);
    }

    public event Action? TerminologyChanged;
    public void NotifyTerminologyChanged() => TerminologyChanged?.Invoke();
}
