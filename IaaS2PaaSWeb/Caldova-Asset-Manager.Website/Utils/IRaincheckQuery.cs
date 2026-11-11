using System.Collections.Generic;
using System.Threading.Tasks;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Utils
{
    public interface IRaincheckQuery
    {
        Task<int> AddAsync(Raincheck raincheck);
        Task<Raincheck> FindAsync(int id);
        Task<IEnumerable<Raincheck>> GetAllAsync();
    }
}
