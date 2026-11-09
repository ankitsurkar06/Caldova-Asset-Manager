# Windows Compatibility Guard

Apply after base input validation and before tooling checks.
This is native-tool compatibility, not CCA execution detection.

```powershell
if (-not $IsWindows) {
  throw "[recommend-sql-sku-sizing] UNSUPPORTED_HOST: Local SKU sizing requires Windows. Use a Windows machine with prepared tooling and source access or completed performance data."
}
```

Report an unsupported environment with the safe `UNSUPPORTED_HOST` diagnostic,
not the raw thrown error.
Stop without discovery, installation, sign-in or asking for more inputs.
Guaranteed cloud rejection remains deferred.
