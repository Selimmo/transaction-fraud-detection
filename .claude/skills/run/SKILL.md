---
name: run
description: Launch the TransactionFraudDetection.Api (ASP.NET Core Web API on .NET 10) locally. Applies to files under the transaction-fraud-detection project.
---

# Run TransactionFraudDetection.Api

```bash
dotnet run --project src/TransactionFraudDetection.Api
```

- HTTP: `http://localhost:5036`, HTTPS: `https://localhost:7065` (see `src/TransactionFraudDetection.Api/Properties/launchSettings.json`). Use the `https` launch profile to get both: `dotnet run --project src/TransactionFraudDetection.Api --launch-profile https`.
- First run may prompt to trust the local dev cert: `dotnet dev-certs https --trust`.
- In the `Development` environment the OpenAPI document is served at `/openapi/v1.json` (registered via `app.MapOpenApi()` in `Program.cs`).
- Manual requests are defined in `src/TransactionFraudDetection.Api/TransactionFraudDetection.Api.http` — usable directly with `curl` or an editor's `.http` client.
