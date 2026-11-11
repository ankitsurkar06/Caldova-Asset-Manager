"""Task008 local consistency/completeness evidence, not the unavailable MCP checks."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET

root = Path.cwd()
web = root / "IaaS2PaaSWeb/Caldova-Asset-Manager.Website"
report = root / ".github/modernize/plans/report-20261023210809"

def text(path):
    return path.read_text(encoding="utf-8-sig")

# Exact checks of inspected unchanged dependencies, not a whole-tree Git-status claim.
unchanged = {
    web / "Utils/ConfigurationHelpers.cs": "3dc81d341c706b6dab0a71b0551d7864460e5688f60fa2d69d758862e2857786",
    web / "Utils/HttpClientWrapper.cs": "13bc02b104b3ffe2066788b72df66509ce7bba9ea0cdbf246d1d6b77016f1ad5",
    web / "Security/ExternalSecretConfiguration.cs": "292b6271a69509d2c5159b95854d41897a9b0aa700918c9eda19ccf77b912596",
    web / "Controllers/RecommendationsController.cs": "dcff0a0c78a63708eace18e3e34e747baec832df3407f4f6aa665ba57fe213e8",
    web / "Caldova-Asset-Manager.csproj": "babaaeccd0ee5c8c3725f32d371a89cdb5695c8699ae06c0c22694c1515cd155",
    web / "packages.config": "5ac214a5feeca2f04c164f7ec83deef6699ef63802958b6136f951f249dfbfc0",
    report / "plan.md": "aa34bca448b652e627839c1cd8bd1b9e0908f60a88045dbf3ec52d33f886bed0",
    report / ".metadata/tasks.json": "6ff7d7c394f1a3b931cdc0659fc9dc0b3229bef0111715eb305e86941c5fa488",
}
for path, expected in unchanged.items():
    assert hashlib.sha256(path.read_bytes()).hexdigest() == expected, str(path)
print("PASS: original plan/task metadata/project/packages/helpers/credential and controller code fingerprints unchanged")

tasks = json.loads(text(report / ".metadata/tasks.json"))["tasks"]
task = next(t for t in tasks if t["id"] == "008-transform-configuration")
assert task["status"] == "pending"
assert task["dependencies"] == ["007-transform-redis-cache"]
assert len(tasks) == 9
print("PASS: nine-task graph/status retained; no schema rewrite or upstream completion claims")

config = ET.parse(web / "Web.config").getroot()
settings = {e.get("key"): e.get("value") for e in config.findall("appSettings/add")}
original = "https://api.datamarket.azure.com/data.ashx/amla/mba/v1/Score"
assert settings["MachineLearning.ScoringEndpoint"] == original
assert settings["MachineLearning.ModelName"] == "sampletest"
assert settings["ShowRecommendations"] == "false"
assert settings["MachineLearning.AccountKeySecretName"] == ""
assert "MachineLearning.AccountKey" not in settings
engine = text(web / "Recommendations/AzureMLFrequentlyBoughtTogetherRecommendationEngine.cs")
assert original not in engine
assert "https://datamarket.azure.com/dataset/amla/mba" in engine  # documentation, not executable
assert 'GetRequiredSetting("MachineLearning.ScoringEndpoint")' in engine
assert 'GetRequiredSetting("MachineLearning.ModelName")' in engine
assert "ExternalSecretConfiguration.GetSetting(name)" in engine
assert "Uri.UriSchemeHttps" in engine
for part in ("UserInfo", "Query", "Fragment"):
    assert f"scoringEndpoint.{part}" in engine
assert 'string.Format("{0}?Id=%27{1}%27&Item=%27{2}%27"' in engine
assert engine.index('GetRequiredSetting("MachineLearning.ScoringEndpoint")') < engine.index("try\n")
resolver = text(web / "Security/ExternalSecretConfiguration.cs")
assert '?? ConfigurationManager.AppSettings[name]' in resolver
assert 'name.Replace(\'.\', \'_\').ToUpperInvariant()' in resolver
print("PASS: scoring URL externalized, defaults preserved, shared host precedence/lazy validation and query contract present")

ns = {"m": "http://schemas.microsoft.com/developer/msbuild/2003"}
project = ET.parse(web / "Caldova-Asset-Manager.csproj").getroot()
assert project.find("m:PropertyGroup/m:TargetFrameworkVersion", ns).text == "v4.7.2"
for item in project.findall(".//m:Compile", ns):
    assert (web / item.get("Include").replace("\\", "/")).is_file()
for item in project.findall(".//m:HintPath", ns):
    assert (web / item.text.replace("\\", "/")).is_file()
for name in ("Web.Debug.config", "Web.Release.config"):
    transform = ET.parse(web / name).getroot()
    assert transform.find("appSettings") is None
test_projects = [
    p for p in (root / "IaaS2PaaSWeb").rglob("*.*proj")
    if p.suffix in (".csproj", ".vbproj", ".fsproj")
    and ("test" in p.name.lower() or any(s in text(p) for s in
         ("Microsoft.NET.Test.Sdk", "MSTest", "NUnit", "xunit", "<IsTestProject>true")))
]
assert not test_projects
print("PASS: actual net472, Compile/HintPaths resolve, transforms retain settings; no existing unit-test project found")

doc = text(root / "docs/RECOMMENDATION-CONFIGURATION.md")
for term in ("CALDOVA_MACHINELEARNING_SCORINGENDPOINT", "CALDOVA_MACHINELEARNING_MODELNAME",
             "CALDOVA_SHOWRECOMMENDATIONS", "Key Vault", "task009", "not** a .NET10",
             "absolute HTTPS", "empty environment", "no configuration service"):
    assert term in doc, term
for name in ("008-plan.md", "008-progress.md", "008-VerifyConfiguration.cs", "008-RunConfiguration.ps1"):
    assert (report / name).is_file()
print("PASS: documentation/tracking cover overrides, validation, gating, secrets and acceptance limits")
print("LIMIT: no MCP validator/CVE/live service/unit-test-project acceptance claim")
