using System;
using System.Threading.Tasks;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.ViewModels;

namespace CaldovaAssetManager.Utils
{
    public interface IOrdersQuery
    {
        Task<Order> FindOrderAsync(int id);
        Task<OrdersModel> IndexHelperAsync(string username, DateTime? start, DateTime? end, string invalidOrderSearch, bool isAdminSearch);
    }
}
