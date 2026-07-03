using System.Collections.Generic;
using System.Threading.Tasks;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Core.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task<IEnumerable<Product>> SearchProductsAsync(string query);
}
