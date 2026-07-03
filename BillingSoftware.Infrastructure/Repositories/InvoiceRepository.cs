using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Database;

namespace BillingSoftware.Infrastructure.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(BillingDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.FinancialYear)
            .Include(i => i.InvoiceItems)
                .ThenInclude(item => item.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByFinancialYearAsync(Guid financialYearId)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Where(i => i.FinancialYearId == financialYearId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .ToListAsync();
    }

    public async Task<int> GetNextInvoiceNumberAsync(Guid financialYearId)
    {
        var maxNum = await _dbSet
            .Where(i => i.FinancialYearId == financialYearId)
            .Select(i => (int?)i.InvoiceNumber)
            .MaxAsync();

        return (maxNum ?? 0) + 1;
    }
}
