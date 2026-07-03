using System;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Application.Services;

public class SessionContext : ISessionContext
{
    public User? CurrentUser { get; set; }
    public FinancialYear? CurrentFinancialYear { get; set; }
    public Guid? CurrentLoginHistoryId { get; set; }

    public void Clear()
    {
        CurrentUser = null;
        CurrentFinancialYear = null;
        CurrentLoginHistoryId = null;
    }
}
