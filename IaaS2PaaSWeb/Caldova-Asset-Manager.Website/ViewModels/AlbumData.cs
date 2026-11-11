using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CaldovaAssetManager.ViewModels
{
    public class ProductData
    {
        // Browser announcement callbacks depend on these case-sensitive wire names.
        [JsonProperty("Title", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string Title { get; set; }

        [JsonProperty("Url", NamingStrategyType = typeof(DefaultNamingStrategy))]
        public string Url { get; set; }
    }
}