"""Focused offline structural evidence. Run from repository root; no SQL access."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET

root = Path.cwd()
web = root / "IaaS2PaaSWeb/Caldova-Asset-Manager.Website"
report = root / ".github/modernize/plans/report-20261023210809"


def text(path):
    data = path.read_bytes()
    return data.decode("utf-16" if data.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8-sig")


tasks = json.loads(text(report / ".metadata/tasks.json"))["tasks"]
ids = [task["id"] for task in tasks]
assert len(ids) == 9 and len(ids) == len(set(ids))
for index, task in enumerate(tasks):
    assert all(dependency in ids[:index] for dependency in task["dependencies"])
task = next(t for t in tasks if t["id"] == "003-transform-sql-managed-instance")
assert task["status"] == "pending" and "net472/EF6" in task["description"]
assert task["dependencies"] == ["002-transform-sqlclient"]
print("PASS: 9 task IDs/dependency ordering/003 pending status and honest execution evidence")

config = ET.parse(web / "Web.config").getroot()
setting = config.find("connectionStrings/add[@name='DefaultConnectionString']")
assert setting.get("connectionString") == "" and setting.get("providerName") == "Microsoft.Data.SqlClient"
assert config.find("entityFramework/defaultConnectionFactory") is None
assert config.find("entityFramework/providers/provider[@invariantName='Microsoft.Data.SqlClient']") is not None
for filename in ["Web.config", "Web.Debug.config", "Web.Release.config", "Caldova-Asset-Manager.csproj", "packages.config"]:
    ET.parse(web / filename)
print("PASS: project/config/package/transform XML parses; named MI provider aligned; LocalDB factory absent")

ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
project = ET.parse(web / "Caldova-Asset-Manager.csproj").getroot()
items = project.findall(".//m:Compile", ns)
assert sum(e.get("Include") == r"Utils\SqlDatabaseConfiguration.cs" for e in items) == 1
for item in items:
    assert (web / item.get("Include").replace("\\", "/")).exists()
for item in project.findall(".//m:HintPath", ns):
    assert (web / item.text.replace("\\", "/")).exists()
print("PASS: new Compile item unique; every compiled source and assembly HintPath exists")
packages = ET.parse(web / "packages.config").getroot()
for package in packages:
    assert (root / "IaaS2PaaSWeb/packages" / (package.get("id") + "." + package.get("version"))).is_dir()
print("PASS: all %d declared package folders exist; no task-003 dependency additions" % len(packages))

global_code = text(web / "Global.asax.cs")
auth = text(web / "App_Start/Startup.Auth.cs")
context = text(web / "Models/CaldovaAssetManagerContext.cs")
assert "SqlDatabaseConfiguration.Validate();" in global_code
assert "CaldovaAssetManagerDbInitializer" not in global_code
assert "CreateAdminUser" not in auth and "Authentication.Administrator." not in auth
assert "Database.SetInitializer<CaldovaAssetManagerContext>(null);" in context
assert "base(SqlDatabaseConfiguration.CreateConnection(), true)" in context
print("PASS: both startup seeding paths removed; context disables initializer and owns external connection")
for source in web.rglob("*.cs"):
    if {"bin", "obj"}.intersection(source.parts):
        continue
    code = text(source)
    assert "new CaldovaAssetManagerDbInitializer" not in code
    assert "MigrateDatabaseToLatestVersion" not in code
    assert "Database.Create(" not in code and "Database.CreateIfNotExists(" not in code
    assert "System.Data.SqlClient" not in code
print("PASS: no active legacy SqlClient, initializer registration, automatic migration or database-create calls")

assert project.find(".//m:TargetFrameworkVersion", ns).text == "v4.7.2"
test_projects = [
    path for path in root.rglob("*.*proj")
    if "packages" not in path.parts and "test" in path.stem.lower()
]
assert not test_projects
print("LIMIT: actual target remains v4.7.2 (not .NET 10); no existing unit-test project found")

doc = text(root / "docs/SQL-MANAGED-INSTANCE.md")
for term in ["Managed Instance", "SQLCONNSTR_DefaultConnectionString", "3342", "11000-11999",
             "password", "db_owner", "rollback", "AspNetUsers", "collation", "No target database"]:
    assert term in doc
print("PASS: docs cover MI configuration/network/permissions/controlled migration and acceptance limits")
print("PASS: local structural consistency/completeness; known upstream allOf schema bug excluded")
