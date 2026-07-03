using System;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Application.Services;

public interface ISessionContext
{
    User? CurrentUser { get; set; }
    FinancialYear? CurrentFinancialYear { get; set; }
    Guid? CurrentLoginHistoryId { get; set; }
    void Clear();
}
