//TODO Application Insights - Uncomment
//using Microsoft.ApplicationInsights.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CaldovaAssetManager.Utils
{
    public class CaldovaAssetManagerTelemetryInitializer
    {
        string appVersion = GetApplicationVersion();

        private static string GetApplicationVersion()
        {
            return typeof(CaldovaAssetManagerTelemetryInitializer).Assembly.GetName().Version.ToString();
        }
        //TODO Application Insights - Uncomment
        //public void Initialize(ITelemetry telemetry)
        //{
        //    telemetry.Context.Component.Version = appVersion;
        //}
    }
}