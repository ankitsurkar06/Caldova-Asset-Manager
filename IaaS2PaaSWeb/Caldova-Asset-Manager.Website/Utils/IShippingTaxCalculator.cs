using System.Collections.Generic;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Utils
{
	public interface IShippingTaxCalculator
	{
		OrderCostSummary CalculateCost(IEnumerable<ILineItem> items, string orderZip);
	}
}