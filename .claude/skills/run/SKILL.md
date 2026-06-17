---
name: run
description: Launch the TransactionFraudDetection.Api and Worker (.NET 10) locally, plus the LocalStack/Ollama dependencies they need. Applies to files under the transaction-fraud-detection project.
---

# Run TransactionFraudDetection

## Dependencies (start first)

- LocalStack (simulated SQS): `docker compose up -d`. Health check: `curl http://localhost:4566/_localstack/health` should report `sqs: available`.
- Ollama (local LLM, model `qwen3:8b`): must already be serving at `http://localhost:11434` (`ollama serve`); pull the model once with `ollama pull qwen3:8b` if missing.

## Api

```bash
dotnet run --project src/TransactionFraudDetection.Api
```

- HTTP: `http://localhost:5036`, HTTPS: `https://localhost:7065` (see `src/TransactionFraudDetection.Api/Properties/launchSettings.json`). Use the `https` launch profile to get both: `dotnet run --project src/TransactionFraudDetection.Api --launch-profile https`.
- First run may prompt to trust the local dev cert: `dotnet dev-certs https --trust`.
- In the `Development` environment the OpenAPI document is served at `/openapi/v1.json` (registered via `app.MapOpenApi()` in `Program.cs`).
- Manual requests are defined in `src/TransactionFraudDetection.Api/TransactionFraudDetection.Api.http` — usable directly with `curl` or an editor's `.http` client.
- After evaluating each request, the Api publishes a `FraudCheckNotification` to the `fraud-check-findings` SQS queue (LocalStack) before responding. If LocalStack isn't running, requests will fail.

## Worker

```bash
dotnet run --project src/TransactionFraudDetection.Worker
```

- Long-polls the `fraud-check-findings` queue. For each fraudulent finding, calls Ollama (`qwen3:8b`) to generate an explanation and writes `output/{transactionId}.json` (relative to the working directory the worker was started from). Non-fraudulent findings are consumed and dropped — no file is written.
