namespace CaldovaAssetManager.Utils;

public class TelemetryProvider(ILogger<TelemetryProvider> logger) : ITelemetryProvider
{
    public void TrackTrace(string message) => logger.LogInformation("{Event}", message);
    public void TrackEvent(string message) => logger.LogInformation("{Event}", message);

    public void TrackEvent(string message, Dictionary<string, string> properties, Dictionary<string, double> measurements) =>
        logger.LogInformation("{Event} {Properties} {Measurements}", message, properties, measurements);

    public void TrackException(Exception exception) =>
        logger.LogError("External operation failed ({ExceptionType}).", exception.GetType().Name);
}
