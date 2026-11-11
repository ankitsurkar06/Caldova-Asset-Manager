using System.Collections.Generic;
using System.Threading.Tasks;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.ProductSearch
{
    public interface IProductSearch
    {
        Task<IEnumerable<Product>> Search(string query);
    }
}
