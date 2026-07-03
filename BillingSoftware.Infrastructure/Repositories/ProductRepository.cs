using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Database;

namespace BillingSoftware.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(BillingDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStockLevel)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllAsync();
        }

        var normalizedQuery = query.ToLower();
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Where(p => p.IsActive && 
                       (p.Name.ToLower().Contains(normalizedQuery) || 
                        p.HSNCode.Contains(normalizedQuery) || 
                        (p.Category != null && p.Category.Name.ToLower().Contains(normalizedQuery))))
            .ToListAsync();
    }
}
