using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CaldovaAssetManager.Utils;

namespace CaldovaAssetManager.Recommendations
{
    /// <summary>
    /// This class implements Azure ML Frequently Bought Together recommendation engine
    /// Details can be found at https://datamarket.azure.com/dataset/amla/mba
    /// </summary>
    public class AzureMLFrequentlyBoughtTogetherRecommendationEngine : IRecommendationEngine
    {
        private readonly IHttpClient client;
        private readonly ITelemetryProvider telemetry;

        private class AzureMLFrequentlyBoughtTogetherServiceResponse
        {
            public List<string> ItemSet { get; set; }
            public int Value { get; set; }
        }

        public AzureMLFrequentlyBoughtTogetherRecommendationEngine(IHttpClient httpClient, ITelemetryProvider telemetryProvider)
        {
            client = httpClient;
            telemetry = telemetryProvider;
        }

        public async Task<IEnumerable<string>> GetRecommendationsAsync(string productId)
        {
            string modelName = GetRequiredSetting("MachineLearning.ModelName");
            string endpoint = GetRequiredSetting("MachineLearning.ScoringEndpoint");
            Uri scoringEndpoint;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out scoringEndpoint) ||
                scoringEndpoint.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrEmpty(scoringEndpoint.Host) ||
                !string.IsNullOrEmpty(scoringEndpoint.UserInfo) ||
                !string.IsNullOrEmpty(scoringEndpoint.Query) ||
                !string.IsNullOrEmpty(scoringEndpoint.Fragment))
            {
                throw new InvalidOperationException(
                    "MachineLearning.ScoringEndpoint must be an absolute HTTPS URL without credentials, query or fragment.");
            }
            //The Azure ML service takes in a recommendation model name (trained ahead of time) and a product id
            string uri = string.Format("{0}?Id=%27{1}%27&Item=%27{2}%27",
                scoringEndpoint.AbsoluteUri, EscapeQueryValue(modelName), EscapeQueryValue(productId));

            try
            {
                //The Azure ML service returns a set of numbers, which indicate the recommended product id
                var response = await client.GetStringAsync(uri);
                AzureMLFrequentlyBoughtTogetherServiceResponse deserializedResponse = JsonConvert.DeserializeObject<AzureMLFrequentlyBoughtTogetherServiceResponse>(response);
                //When there is no recommendation, The Azure ML service returns a JSON object that does not contain ItemSet
                var recommendation = (deserializedResponse
                    ?? throw new InvalidOperationException("The recommendation service returned an empty response.")).ItemSet;
                if (recommendation == null)
                {
                    return Enumerable.Empty<string>();
                }
                else
                {
                    return recommendation;
                }
            }
            catch (HttpRequestException e)
            {
                telemetry.TrackException(e);

                throw;
            }
        }

        private static string GetRequiredSetting(string name)
        {
            // Disabled recommendations must not require endpoint configuration.
            var value = ConfigurationHelpers.GetString(name.Replace('.', ':'));
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " is required when recommendations are requested.");
            return value;
        }

        private static string EscapeQueryValue(string value)
        {
            // Preserve the service's quoted OData string contract without allowing data to add query parameters.
            // Older Framework URI compatibility modes leave apostrophes unescaped.
            return Uri.EscapeDataString((value ?? string.Empty).Replace("'", "''")).Replace("'", "%27");
        }
    }
}