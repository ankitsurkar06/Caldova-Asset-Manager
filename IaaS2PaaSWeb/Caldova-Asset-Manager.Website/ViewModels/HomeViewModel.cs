using System.Collections.Generic;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> NewProducts { get; set; }
        public List<Product> TopSellingProducts { get; set; }
        public List<CommunityPost> CommunityPosts { get; set; }
    }
}