//TODO Application Insights - Uncomment
//using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Owin;
using Owin;
using CaldovaAssetManager;
using System.Web.Configuration;

[assembly: OwinStartup(typeof(Startup))]

//comment
namespace CaldovaAssetManager
{
	// bellevue comment!!
	// second commit
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            //TODO Application Insights - Uncomment
            //TelemetryConfiguration.Active.InstrumentationKey = WebConfigurationManager.AppSettings["Keys:ApplicationInsights:InstrumentationKey"];

        }
    }
}
