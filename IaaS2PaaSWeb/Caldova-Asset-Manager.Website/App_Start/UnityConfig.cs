using Microsoft.Practices.Unity;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.ProductSearch;
using CaldovaAssetManager.Recommendations;
using CaldovaAssetManager.Utils;

namespace CaldovaAssetManager
{
    public class UnityConfig
    {
        public static UnityContainer BuildContainer()
        {
            var container = new UnityContainer();

            container.RegisterType<ICaldovaAssetManagerContext, CaldovaAssetManagerContext>();
            container.RegisterType<IOrdersQuery, OrdersQuery>();
            container.RegisterType<IRaincheckQuery, RaincheckQuery>();
            container.RegisterType<IRecommendationEngine, AzureMLFrequentlyBoughtTogetherRecommendationEngine>();
            container.RegisterType<ITelemetryProvider, TelemetryProvider>();
            container.RegisterType<IProductSearch, StringContainsProductSearch>();
            container.RegisterType<IShippingTaxCalculator, DefaultShippingTaxCalculator>();

			container.RegisterInstance<IHttpClient>(container.Resolve<HttpClientWrapper>());

            return container;
        }
    }
}