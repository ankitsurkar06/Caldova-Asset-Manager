// Focused offline checks against the real compiled website. No hosted or cloud calls.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using CaldovaAssetManager.Controllers;
using CaldovaAssetManager.Recommendations;
using CaldovaAssetManager.Utils;

internal static class VerifyConfiguration
{
    private const string EndpointKey = "MachineLearning.ScoringEndpoint";
    private const string ModelKey = "MachineLearning.ModelName";
    private const string OriginalEndpoint = "https://api.datamarket.azure.com/data.ashx/amla/mba/v1/Score";
    private const string TestEndpoint = "https://scoring.example.invalid/v1/Score";
    private static int assertions;

    private sealed class Client : IHttpClient
    {
        public int Calls;
        public string Url;
        public string Body = "{\"ItemSet\":[\"2\",\"3\"],\"Value\":1}";
        public bool Fail;
        public Task<string> GetStringAsync(string uri)
        {
            Calls++;
            Url = uri;
            if (Fail) throw new HttpRequestException("offline transport marker");
            return Task.FromResult(Body);
        }
    }
    private sealed class Telemetry : ITelemetryProvider
    {
        public int Exceptions;
        public void TrackTrace(string message) { }
        public void TrackEvent(string message) { }
        public void TrackEvent(string message, Dictionary<string, string> p, Dictionary<string, double> m) { }
        public void TrackException(Exception exception) { Exceptions++; }
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        assertions++;
        Console.WriteLine("PASS: " + label);
    }
    private static void SetHost(string name, string value)
    {
        Environment.SetEnvironmentVariable("CALDOVA_" + name.Replace('.', '_').ToUpperInvariant(), value);
    }
    private static void Reject(AzureMLFrequentlyBoughtTogetherRecommendationEngine engine,
        Client client, Telemetry telemetry, string setting, string label)
    {
        int calls = client.Calls;
        int exceptions = telemetry.Exceptions;
        try { engine.GetRecommendationsAsync("42").GetAwaiter().GetResult(); }
        catch (ConfigurationErrorsException e)
        {
            Check(e.Message.Contains(setting) && e.InnerException == null &&
                !e.ToString().Contains("offline-private-marker") &&
                !e.ToString().Contains("example.invalid"), label + " (explicit, sanitized)");
            Check(client.Calls == calls && telemetry.Exceptions == exceptions,
                label + " (no HTTP or catch-and-empty)");
            return;
        }
        throw new Exception("FAILED: expected configuration rejection: " + label);
    }
    public static int Main(string[] args)
    {
        try
        {
            // Isolate from inherited deployment settings, only in this child process.
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                if (((string)entry.Key).StartsWith("CALDOVA_", StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable((string)entry.Key, null);
            var client = new Client();
            var telemetry = new Telemetry();
            var engine = new AzureMLFrequentlyBoughtTogetherRecommendationEngine(client, telemetry);
            if (args.Length != 0)
            {
                // Runner supplies missing/empty file variants in generated exe.config, never Web.config.
                Reject(engine, client, telemetry, args[0], "missing/empty file setting");
                SetHost(EndpointKey, TestEndpoint);
                SetHost(ModelKey, "catalog");
                engine.GetRecommendationsAsync("42").GetAwaiter().GetResult();
                Check(client.Url == TestEndpoint + "?Id=%27catalog%27&Item=%2742%27",
                    "valid overrides work with missing/empty file settings");
                Console.WriteLine("PASS: " + assertions + " file-variant assertions; no network calls.");
                return 0;
            }
            Check(ConfigurationManager.AppSettings[EndpointKey] == OriginalEndpoint, "external file binds original endpoint");
            Check(ConfigurationManager.AppSettings[ModelKey] == "sampletest", "existing file model retained");
            Check(!ConfigurationHelpers.GetBool("ShowRecommendations"), "recommendations default remains disabled");
            var result = engine.GetRecommendationsAsync("42").GetAwaiter().GetResult();
            Check(client.Url == OriginalEndpoint + "?Id=%27sampletest%27&Item=%2742%27",
                "ordinary default request matches original complete URL");
            Check(result.SequenceEqual(new[] { "2", "3" }), "nonempty JSON recommendation behavior retained");

            SetHost(EndpointKey, TestEndpoint);
            SetHost(ModelKey, "catalog");
            engine.GetRecommendationsAsync("42").GetAwaiter().GetResult();
            Check(client.Url == TestEndpoint + "?Id=%27catalog%27&Item=%2742%27",
                "endpoint and model host overrides win over file");
            SetHost(EndpointKey, "https://other.example.invalid/prefix/Score");
            engine.GetRecommendationsAsync("43").GetAwaiter().GetResult();
            Check(client.Url == "https://other.example.invalid/prefix/Score?Id=%27catalog%27&Item=%2743%27",
                "subsequent request reads changed override without hardcoded host/path");

            SetHost(EndpointKey, TestEndpoint);
            SetHost(ModelKey, "a b&c#d?e%f'g");
            engine.GetRecommendationsAsync("x&y=z#q?'").GetAwaiter().GetResult();
            Check(client.Url == TestEndpoint +
                "?Id=%27a%20b%26c%23d%3Fe%25f%27%27g%27&Item=%27x%26y%3Dz%23q%3F%27%27%27",
                "query data escaped and internal OData apostrophes doubled");
            SetHost(ModelKey, "\u6a21\u578b");
            engine.GetRecommendationsAsync("42").GetAwaiter().GetResult();
            Check(client.Url == TestEndpoint + "?Id=%27%E6%A8%A1%E5%9E%8B%27&Item=%2742%27",
                "Unicode model encoded as query data");
            SetHost(ModelKey, "catalog");
            engine.GetRecommendationsAsync(null).GetAwaiter().GetResult();
            Check(client.Url.EndsWith("&Item=%27%27"), "null product retains original empty-string semantics");
            engine.GetRecommendationsAsync("").GetAwaiter().GetResult();
            Check(client.Url.EndsWith("&Item=%27%27"), "empty product retains original empty-string semantics");

            foreach (var bad in new[] { " ", "/Score", "offline-private-marker",
                "http://scoring.example.invalid/Score", "ftp://scoring.example.invalid/Score",
                "https://user:offline-private-marker@scoring.example.invalid/Score",
                TestEndpoint + "?token=offline-private-marker", TestEndpoint + "#offline-private-marker",
                TestEndpoint + "?", TestEndpoint + "#" })
            {
                SetHost(EndpointKey, bad);
                Reject(engine, client, telemetry, EndpointKey, "invalid endpoint case " + assertions);
            }
            SetHost(EndpointKey, TestEndpoint);
            SetHost(ModelKey, " ");
            Reject(engine, client, telemetry, ModelKey, "blank host model overrides valid file");
            SetHost(EndpointKey, null);
            SetHost(ModelKey, null);
            engine.GetRecommendationsAsync("42").GetAwaiter().GetResult();
            Check(client.Url == OriginalEndpoint + "?Id=%27sampletest%27&Item=%2742%27",
                "removing overrides restores file fallback");
            client.Body = "{}";
            Check(!engine.GetRecommendationsAsync("42").GetAwaiter().GetResult().Any(),
                "missing ItemSet remains legitimate empty response");
            client.Body = "{\"ItemSet\":[]}";
            Check(!engine.GetRecommendationsAsync("42").GetAwaiter().GetResult().Any(),
                "empty ItemSet retained");
            client.Fail = true;
            Check(!engine.GetRecommendationsAsync("42").GetAwaiter().GetResult().Any() &&
                telemetry.Exceptions == 1, "existing task009 HTTP catch/telemetry behavior unchanged (not endorsed)");

            SetHost(EndpointKey, " ");
            SetHost(ModelKey, " ");
            SetHost("KeyVault.Uri", "offline-private-marker");
            SetHost("MachineLearning.AccountKeySecretName", "offline-private-marker");
            int beforeDisabled = client.Calls;
            var disabledEngine = new AzureMLFrequentlyBoughtTogetherRecommendationEngine(client, telemetry);
            Check(disabledEngine != null, "constructor remains lazy with invalid endpoint/model");
            var controller = new RecommendationsController(null, disabledEngine);
            Check(controller.GetRecommendations("42").GetAwaiter().GetResult() is EmptyResult &&
                client.Calls == beforeDisabled, "disabled controller returns without database or recommendation call");
            using (var http = new HttpClientWrapper())
                Check(http.DefaultRequestHeaders.Authorization == null, "disabled HTTP wrapper performs no secret lookup");
            SetHost("ShowRecommendations", "true");
            Check(ConfigurationHelpers.GetBool("ShowRecommendations"), "existing feature environment override retained");
            SetHost("ShowRecommendations", "false");
            Check(!ConfigurationHelpers.GetBool("ShowRecommendations"), "feature can be disabled through environment");
            SetHost("ShowRecommendations", "offline-private-marker");
            try { ConfigurationHelpers.GetBool("ShowRecommendations"); throw new Exception("invalid switch accepted"); }
            catch (ConfigurationErrorsException e)
            {
                Check(!e.ToString().Contains("offline-private-marker"), "invalid feature switch still fails sanitized");
            }
            Console.WriteLine("PASS: " + assertions + " offline assertions against compiled website; no network calls.");
            return 0;
        }
        catch (Exception e)
        {
            // Tests use synthetic values only; avoid printing arbitrary config or response exceptions.
            Console.Error.WriteLine("FAIL: " + e.GetType().Name + (e.Message.StartsWith("FAILED:") ? " " + e.Message : ""));
            Console.Error.WriteLine(e.StackTrace);
            return 1;
        }
    }
}
