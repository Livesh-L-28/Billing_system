using System;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Application.Services;

public interface ISessionContext
{
    User? CurrentUser { get; set; }
    FinancialYear? CurrentFinancialYear { get; set; }
    Guid? CurrentLoginHistoryId { get; set; }
    int CurrentCompanyId { get; }
    int CurrentSettingId { get; }
    void Clear();

    event Action<string, object?>? NavigationRequested;
    void RequestNavigation(string viewName, object? parameter = null);
}
