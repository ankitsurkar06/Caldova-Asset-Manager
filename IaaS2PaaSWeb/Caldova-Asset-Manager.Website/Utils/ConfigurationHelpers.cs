namespace CaldovaAssetManager.Utils;

public static class ConfigurationHelpers
{
    private static IConfiguration configuration;

    public static void Initialize(IConfiguration value) =>
        configuration = value ?? throw new ArgumentNullException(nameof(value));

    public static string GetString(string name) =>
        (configuration ?? throw new InvalidOperationException("Configuration has not been initialized."))[name];

    public static bool GetBool(string name) => bool.TryParse(GetString(name), out var value)
        ? value : throw new InvalidOperationException($"Setting {name} must be true or false.");

    public static int GetInt32(string name) => int.TryParse(GetString(name), out var value)
        ? value : throw new InvalidOperationException($"Setting {name} must be an integer.");

    public static Uri GetUri(string name) => new(GetString(name), UriKind.Absolute);
    public static TimeSpan GetTimeSpan(string name) => TimeSpan.Parse(GetString(name));
    public static TimeSpan GetTimeSpanMinutes(string name) => PositiveInterval(name, TimeSpan.FromMinutes);
    public static TimeSpan GetTimeSpanSeconds(string name) => PositiveInterval(name, TimeSpan.FromSeconds);
    public static Type GetType(string name) => Type.GetType(GetString(name), true, true);

    private static TimeSpan PositiveInterval(string name, Func<double, TimeSpan> create)
    {
        var value = GetInt32(name);
        return value > 0 ? create(value) : throw new InvalidOperationException($"Setting {name} must be positive.");
    }
}
