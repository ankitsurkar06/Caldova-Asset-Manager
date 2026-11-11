// Focused offline contract checks against the built website. No HTTP/DB/token calls.
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Owin;
using CaldovaAssetManager;
using CaldovaAssetManager.Areas.Admin;
using CaldovaAssetManager.Areas.Admin.Controllers;
using CaldovaAssetManager.Hubs;
using CaldovaAssetManager.Models;
using CaldovaAssetManager.ViewModels;

class AnnouncementCheck
{
    static int count;
    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("FAIL: " + description);
        count++;
        Console.WriteLine("PASS: " + description);
    }

    static void Main()
    {
        typeof(Global).GetField("UnityContainer", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, new UnityContainer());
        var builder = new RecordingBuilder();
        typeof(Startup).GetMethod("ConfigureSignalR", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { builder });
        var resolver = GlobalHost.DependencyResolver;
        Check(builder.Configuration != null, "compiled startup supplies hub configuration");
        Check(builder.MapPath == "/signalr", "compiled startup retains default /signalr route");
        Check(Object.ReferenceEquals(builder.Configuration.Resolver, resolver),
            "mapped hub and GlobalHost use same resolver");
        Check(Object.ReferenceEquals(builder.Configuration.Resolver.Resolve<IMessageBus>(),
            resolver.Resolve<IMessageBus>()), "mapped and broadcast message bus identity");
        Check(builder.Configuration.EnableJavaScriptProxies, "legacy generated proxy remains enabled");

        var manager = resolver.Resolve<IHubManager>();
        var hub = manager.GetHub("Announcement");
        Check(hub != null && hub.HubType == typeof(AnnouncementHub), "CLR AnnouncementHub / wire Announcement");
        Check(!manager.GetHubMethods("Announcement", null).Any(), "no client-invocable publication methods");
        Check(!typeof(AnnouncementHub).GetCustomAttributes(typeof(Microsoft.AspNet.SignalR.AuthorizeAttribute), true).Any(),
            "public receive access unchanged");
        var proxy = resolver.Resolve<IJavaScriptProxyGenerator>().GenerateProxy("/shop/signalr");
        Check(proxy.Contains("proxies['Announcement']"), "generated case-sensitive proxy Announcement");
        Check(proxy.Contains("/shop/signalr"), "generated proxy retains application-relative hub URL");

        var adminAuth = typeof(StoreManagerController).GetCustomAttributes(typeof(System.Web.Mvc.AuthorizeAttribute), true)
            .Cast<System.Web.Mvc.AuthorizeAttribute>().Single();
        Check(adminAuth.Roles == AdminConstants.Role, "broadcast controller inherits existing admin role");
        var authorizeCore = typeof(System.Web.Mvc.AuthorizeAttribute).GetMethod("AuthorizeCore", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(!(bool)authorizeCore.Invoke(adminAuth, new object[] { new WebContext("/") }),
            "anonymous user fails existing admin policy");
        var signedIn = new WebContext("/") { Principal = new System.Security.Principal.GenericPrincipal(
            new System.Security.Principal.GenericIdentity("offline-user", "offline"), new string[0]) };
        Check(!(bool)authorizeCore.Invoke(adminAuth, new object[] { signedIn }), "signed-in non-admin fails existing policy");
        signedIn.Principal = new System.Security.Principal.GenericPrincipal(
            new System.Security.Principal.GenericIdentity("offline-admin", "offline"), new[] { AdminConstants.Role });
        Check((bool)authorizeCore.Invoke(adminAuth, new object[] { signedIn }), "existing admin role passes policy");
        var create = typeof(StoreManagerController).GetMethod("Create", new Type[] { typeof(Product) });
        Check(create.IsDefined(typeof(HttpPostAttribute), true), "broadcast is POST only");
        Check(create.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true), "anti-forgery retained");

        var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        var data = JObject.Parse(JsonConvert.SerializeObject(new ProductData { Title = "<b>New & exact</b>", Url = "/shop/Store/Details/42" }, settings));
        Check(data.Properties().Select(p => p.Name).OrderBy(n => n).SequenceEqual(new[] { "Title", "Url" }),
            "explicit Title/Url casing survives camel-case resolver");
        Check((string)data["Title"] == "<b>New & exact</b>", "Title round trips unchanged");
        var serializer = resolver.Resolve<JsonSerializer>();
        var output = new System.IO.StringWriter();
        serializer.Serialize(output, new ProductData { Title = "exact", Url = "/Store/Details/42" });
        Check(JObject.Parse(output.ToString()).Properties().Select(p => p.Name).OrderBy(n => n)
            .SequenceEqual(new[] { "Title", "Url" }), "mapped SignalR serializer preserves payload casing");

        var pipeline = new CapturePipeline();
        resolver.Register(typeof(IHubPipelineInvoker), () => pipeline);
        foreach (var appPath in new[] { "/", "/shop" })
        {
            var context = new FakeContext();
            var controller = MakeController(context, appPath);
            var product = new Product { Title = "<b>New & exact</b>" };
            pipeline.BeforeSend = () => Check(context.Saves == 1, "save completes before broadcast");
            MemoryCache.Default["latestProduct"] = new object();
            var result = controller.Create(product).GetAwaiter().GetResult();
            Check(context.Adds == 1 && context.Saves == 1, "valid Create adds and saves once");
            Check(pipeline.Last.Invocation.Hub == "Announcement", "broadcast uses Announcement hub");
            Check(pipeline.Last.Invocation.Method == "announcement", "broadcast event remains announcement");
            Check(pipeline.Last.Invocation.Args.Length == 1, "one payload per broadcast");
            var payload = (ProductData)pipeline.Last.Invocation.Args[0];
            Check(payload.Title == product.Title, "broadcast Title unchanged");
            Check(payload.Url == (appPath == "/" ? "" : appPath) + "/Store/Details/42",
                "public Details URL clears ambient Admin area at " + appPath);
            Check(pipeline.Last.Signal == "h-Announcement", "broadcast targets all hub clients");
            var redirect = result as RedirectToRouteResult;
            Check(redirect != null && (string)redirect.RouteValues["action"] == "Index", "Create redirect preserved");
            Check(MemoryCache.Default["latestProduct"] == null, "latestProduct invalidation preserved");
        }

        var invalidDb = new FakeContext();
        var invalid = MakeController(invalidDb, "/");
        invalid.ModelState.AddModelError("Title", "invalid");
        int sends = pipeline.Sends;
        Check(invalid.Create(new Product()).GetAwaiter().GetResult() is ViewResult, "invalid Create returns view");
        Check(invalidDb.Saves == 0 && invalidDb.Adds == 0 && pipeline.Sends == sends,
            "invalid Create does not save or broadcast");
        var failedDb = new FakeContext { FailSave = true };
        bool failed = false;
        try { MakeController(failedDb, "/").Create(new Product()).GetAwaiter().GetResult(); }
        catch (InvalidOperationException) { failed = true; }
        Check(failed && pipeline.Sends == sends, "failed save cannot announce an unpersisted product");
        Console.WriteLine("ANNOUNCEMENT CHECKS PASSED: " + count);
        resolver.Dispose();
    }

    static StoreManagerController MakeController(FakeContext db, string appPath)
    {
        var routes = new RouteCollection();
        new AdminAreaRegistration().RegisterArea(new AreaRegistrationContext("Admin", routes));
        RouteConfig.RegisterRoutes(routes);
        var rd = new RouteData();
        rd.Values["controller"] = "StoreManager";
        rd.Values["action"] = "Create";
        rd.DataTokens["area"] = "Admin";
        var request = new RequestContext(new WebContext(appPath), rd);
        var controller = new StoreManagerController(db);
        controller.ControllerContext = new ControllerContext(request, controller);
        controller.Url = new UrlHelper(request, routes);
        return controller;
    }

    class WebContext : HttpContextBase
    {
        readonly string appPath;
        public WebContext(string path) { appPath = path; }
        public System.Security.Principal.IPrincipal Principal = new System.Security.Principal.GenericPrincipal(
            new System.Security.Principal.GenericIdentity(""), new string[0]);
        public override System.Security.Principal.IPrincipal User { get { return Principal; } set { Principal = value; } }
        public override object GetService(Type type) { return null; }
        public override System.Collections.IDictionary Items { get { return items; } }
        readonly System.Collections.IDictionary items = new System.Collections.Hashtable();
        public override HttpRequestBase Request { get { return new WebRequest(appPath); } }
        public override HttpResponseBase Response { get { return new WebResponse(); } }
    }
    class WebRequest : HttpRequestBase
    {
        readonly string appPath;
        public WebRequest(string path) { appPath = path; }
        public override string ApplicationPath { get { return appPath; } }
        public override string AppRelativeCurrentExecutionFilePath { get { return "~/Admin/StoreManager/Create"; } }
        public override string PathInfo { get { return ""; } }
        public override string RawUrl { get { return appPath.TrimEnd('/') + "/Admin/StoreManager/Create"; } }
        public override Uri Url { get { return new Uri("https://offline.invalid" + RawUrl); } }
        public override System.Collections.Specialized.NameValueCollection ServerVariables
        { get { return new System.Collections.Specialized.NameValueCollection(); } }
    }
    class WebResponse : HttpResponseBase
    {
        public override string ApplyAppPathModifier(string path) { return path; }
    }

    // Captures the actual compiled startup's map configuration, not a hosted transport.
    class RecordingBuilder : IAppBuilder
    {
        public HubConfiguration Configuration;
        public string MapPath;
        public IDictionary<string, object> Properties { get; private set; }
        public RecordingBuilder() { Properties = new Microsoft.Owin.Builder.AppBuilder().Properties; }
        public IAppBuilder New() { return this; }
        public IAppBuilder Use(object middleware, params object[] args)
        {
            foreach (var arg in args)
            {
                if (arg is HubConfiguration) Configuration = (HubConfiguration)arg;
                if (arg is Microsoft.Owin.Mapping.MapOptions) MapPath = ((Microsoft.Owin.Mapping.MapOptions)arg).PathMatch.Value;
            }
            return this;
        }
        public object Build(Type type)
        {
            return new Func<IDictionary<string, object>, Task>(_ => Task.FromResult(0));
        }
    }

    class CapturePipeline : IHubPipelineInvoker
    {
        public IHubOutgoingInvokerContext Last;
        public int Sends;
        public Action BeforeSend;
        public Task Send(IHubOutgoingInvokerContext context)
        {
            if (BeforeSend != null) BeforeSend();
            Last = context;
            Sends++;
            return Task.FromResult(0);
        }
        public Task<object> Invoke(IHubIncomingInvokerContext c) { throw new NotSupportedException(); }
        public Task Connect(IHub h) { throw new NotSupportedException(); }
        public Task Reconnect(IHub h) { throw new NotSupportedException(); }
        public Task Disconnect(IHub h, bool stopCalled) { throw new NotSupportedException(); }
        public bool AuthorizeConnect(HubDescriptor h, IRequest r) { throw new NotSupportedException(); }
        public IList<string> RejoiningGroups(HubDescriptor h, IRequest r, IList<string> groups) { throw new NotSupportedException(); }
    }

    class Products : DbSet<Product>
    {
        readonly FakeContext context;
        public Products(FakeContext c) { context = c; }
        public override Product Add(Product p) { context.Adds++; p.ProductId = 42; return p; }
    }
    class FakeContext : ICaldovaAssetManagerContext
    {
        public int Adds, Saves;
        public bool FailSave;
        public IDbSet<Product> Products { get; private set; }
        public FakeContext() { Products = new Products(this); }
        public Task<int> SaveChangesAsync(CancellationToken c)
        {
            if (FailSave) throw new InvalidOperationException("offline save failure");
            Saves++;
            return Task.FromResult(1);
        }
        public IDbSet<CartItem> CartItems { get { throw new NotSupportedException(); } }
        public IDbSet<Category> Categories { get { throw new NotSupportedException(); } }
        public IDbSet<OrderDetail> OrderDetails { get { throw new NotSupportedException(); } }
        public IDbSet<Order> Orders { get { throw new NotSupportedException(); } }
        public IDbSet<ApplicationUser> Users { get { throw new NotSupportedException(); } }
        public IDbSet<Raincheck> RainChecks { get { throw new NotSupportedException(); } }
        public IDbSet<Store> Stores { get { throw new NotSupportedException(); } }
        public DbEntityEntry Entry(object e) { throw new NotSupportedException(); }
        public void Dispose() { }
    }
}
