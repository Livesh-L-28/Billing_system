using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Core.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByIdWithDetailsAsync(Guid id);
    Task<IEnumerable<Invoice>> GetInvoicesByFinancialYearAsync(Guid financialYearId);
    Task<int> GetNextInvoiceNumberAsync(Guid financialYearId);
}
