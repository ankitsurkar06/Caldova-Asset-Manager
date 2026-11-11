"""Task006 local scope/consistency evidence; not the unavailable MCP validators."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET

root = Path.cwd()
web = root / "IaaS2PaaSWeb/Caldova-Asset-Manager.Website"
report = root / ".github/modernize/plans/report-20261023210809"
def text(path):
    return path.read_text(encoding="utf-8-sig")

tasks = json.loads(text(report / ".metadata/tasks.json"))
task = next(t for t in tasks["tasks"] if t["id"] == "006-transform-real-time")
assert task["status"] == "pending"
assert task["dependencies"] == ["005-transform-credentials-keyvault"]
assert "ASP.NET Core/.NET10 port is missing" in task["description"]
task.pop("description")
assert hashlib.sha256(json.dumps(tasks, sort_keys=True).encode()).hexdigest() == (
    "d2732ba99b65e2553d4a92c4d3f140362d0eb5dab74d36fae566f7cf9112f298"
)
print("PASS: only task006 description updated; every other metadata value preserved, status pending")

ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
project = ET.parse(web / "Caldova-Asset-Manager.csproj").getroot()
assert project.find("m:PropertyGroup/m:TargetFrameworkVersion", ns).text == "v4.7.2"
packages = {p.get("id"): p.get("version") for p in ET.parse(web / "packages.config").getroot()}
assert len(packages) == 81
assert packages["Microsoft.AspNet.SignalR.Core"] == "2.2.2"
assert packages["Newtonsoft.Json"] == "10.0.3"
assert not any("Azure.SignalR" in p or "ServiceBus" in p for p in packages)
print("PASS: actual net472, existing SignalR2.2.2/JSON10.0.3, no messaging package additions")

startup = text(web / "App_Start/Startup.Auth.cs")
assert startup.index("ConfigureCookieAuth(app)") < startup.index("ConfigureSignalR(app)")
assert "GlobalHost.DependencyResolver = new SignalRDependencyResolver();" in startup
assert "Resolver = GlobalHost.DependencyResolver" in startup
assert "app.MapSignalR(hubConfig);" in startup
hub = text(web / "Hubs/AnnouncementHub.cs")
assert '[HubName("Announcement")]' in hub and "public class AnnouncementHub : Hub" in hub
controller = text(web / "Areas/Admin/Controllers/StoreManagerController.cs")
assert controller.count(".Clients.All.announcement(") == 1
create = controller[controller.index("public async Task<ActionResult> Create(Product product)"):
                    controller.index("// GET: /StoreManager/Edit/5")]
assert create.index("await db.SaveChangesAsync") < create.index(".Clients.All.announcement(")
assert 'new { Area = string.Empty, id = product.ProductId }' in create
assert create.index(".Clients.All.announcement(") < create.index('MemoryCache.Default.Remove("latestProduct")')
assert "[Authorize(Roles = AdminConstants.Role)]" in text(web / "Areas/Admin/Controllers/AdminController.cs")
print("PASS: hub names, shared resolver, auth ordering, save/broadcast/cache ordering and public URL retained")

model = text(web / "ViewModels/AlbumData.cs")
for name in ("Title", "Url"):
    assert f'[JsonProperty("{name}", NamingStrategyType = typeof(DefaultNamingStrategy))]' in model
print("PASS: explicit Title/Url names protected from camel-case resolver override")
print("LIMIT: local checks only; no ASP.NET Core port, hosted transport/browser, cloud or unit-test-project pass")
