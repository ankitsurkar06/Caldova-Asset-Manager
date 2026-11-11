using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.ViewModels
{
    public class EditProductViewModel
    {
        public Product Product { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
    }
}