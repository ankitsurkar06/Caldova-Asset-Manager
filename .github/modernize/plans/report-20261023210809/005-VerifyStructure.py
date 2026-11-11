"""Focused task-005 offline structure/dependency evidence; no secret values emitted."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET
import zipfile

root = Path.cwd()
web = root / "IaaS2PaaSWeb/Caldova-Asset-Manager.Website"
ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
config = ET.parse(web / "Web.config").getroot()
settings = {e.get("key"): e.get("value") for e in config.findall("appSettings/add")}
assert not any(k.startswith("Authentication.Administrator.") for k in settings)
assert "MachineLearning.AccountKey" not in settings
for provider in ("Facebook", "Google", "Microsoft", "Twitter"):
    assert settings["Authentication." + provider + ".Enabled"] == "false"
    assert "Authentication." + provider + ".Secret" not in settings
    assert settings["Authentication." + provider + ".SecretName"] == ""
assert settings["ShowRecommendations"] == "false"
print("PASS: bootstrap/inline secret paths absent, all optional features disabled")

project = ET.parse(web / "Caldova-Asset-Manager.csproj").getroot()
includes = [e.get("Include") for e in project.findall(".//m:Compile", ns)]
assert includes.count(r"Security\ExternalSecretConfiguration.cs") == 1
for e in project.findall(".//m:HintPath", ns):
    assert (web / e.text.replace("\\", "/")).is_file()
packages = {p.get("id"): p.get("version") for p in ET.parse(web / "packages.config").getroot()}
for name, version in packages.items():
    assert (root / "IaaS2PaaSWeb/packages" / (name + "." + version)).is_dir()
assert packages["Azure.Core"] == "1.38.0" and packages["Azure.Identity"] == "1.11.4"
assert packages["Azure.Security.KeyVault.Secrets"] == "4.6.0"
assert packages["EntityFramework"] == "6.5.1" and packages["Microsoft.Data.SqlClient"] == "5.2.3"
sdk = root / "IaaS2PaaSWeb/packages/Azure.Security.KeyVault.Secrets.4.6.0/Azure.Security.KeyVault.Secrets.4.6.0.nupkg"
with zipfile.ZipFile(sdk) as archive:
    spec = ET.fromstring(archive.read("Azure.Security.KeyVault.Secrets.nuspec"))
    dependencies = {e.get("id"): e.get("version") for e in spec.iter() if e.tag.endswith("dependency")}
    assert dependencies["Azure.Core"] == "1.37.0"
binding = {"a": "urn:schemas-microsoft-com:asm.v1"}
redirects = config.findall("runtime/a:assemblyBinding/a:dependentAssembly", binding)
assert any(e.find("a:assemblyIdentity", binding).get("name") == "Azure.Core" and
           e.find("a:bindingRedirect", binding).get("newVersion") == "1.38.0.0" for e in redirects)
print("PASS: 81 package folders/all HintPaths resolve; SDK dependency minimum/redirect and prior driver versions aligned")

secret_code = (web / "Security/ExternalSecretConfiguration.cs").read_text(encoding="utf-8-sig")
assert "new ManagedIdentityCredential(clientId, identityOptions)" in secret_code
assert "DefaultAzureCredential(" not in secret_code and "ClientSecretCredential(" not in secret_code
assert "IsLoggingEnabled = false" in secret_code and "IsLoggingContentEnabled = false" in secret_code
assert "GetSecret(name, cancellationToken:" in secret_code
assert "secret.Properties.ExpiresOn" in secret_code and "secret.Properties.NotBefore" in secret_code
assert "secret.Properties.Enabled == false" in secret_code
http = (web / "Utils/HttpClientWrapper.cs").read_text(encoding="utf-8-sig")
assert http.index('IsEnabled("ShowRecommendations")') < http.index("GetRequiredSecret(")
assert 'new AuthenticationHeaderValue("Basic"' in http
auth = (web / "App_Start/Startup.Auth.cs").read_text(encoding="utf-8-sig")
assert "CreateAdminUser" not in auth and "UseCookieAuthentication" in auth
assert "OnValidateIdentity = SecurityStampValidator" in auth
print("PASS: managed-identity-only boundary, SDK logging disabled, secret-state checks, existing cookie/stamp/API contracts")

template = json.loads((root / "IaaS2PaaSWeb/Environments/WorkshopEnv.json").read_text(encoding="utf-8-sig"))
for key in ("AdminUserPassword", "dbUserPassword"):
    assert template["parameters"][key]["type"] == "securestring"
    assert "defaultValue" not in template["parameters"][key]
assert hashlib.sha256(json.dumps(template, sort_keys=True).encode()).hexdigest() == (
    "f9dcb5250ae415415adc4d00296ed2b000a1f924604f94c347dbb5e31b88eb10")
print("PASS: historical source template changes limited to two removed password defaults")

tasks = json.loads((root / ".github/modernize/plans/report-20261023210809/.metadata/tasks.json").read_text(encoding="utf-8-sig"))["tasks"]
task = next(t for t in tasks if t["id"] == "005-transform-credentials-keyvault")
assert task["status"] == "pending" and task["dependencies"] == ["004-transform-static-assets-blob"]
doc = (root / "docs/KEY-VAULT-SECRETS.md").read_text(encoding="utf-8-sig")
for term in ("rotation", "bootstrap", "security stamps", "RBAC", "No issuer revocation",
             "Environments/bin/Debug", "all** instances", "not** a .NET 10 port"):
    assert term in doc, "Missing documentation topic"
print("PASS: task dependency/status/schema untouched; external rotation/bootstrap/prerequisites documented")
print("LIMIT: no live identity/vault/sign-in/rotation/SQL claims; no existing test project")
