# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A .NET 10 solution for a transaction fraud detection service. The core is a Web API that a payment system can call synchronously while authorizing a transaction, to get back a fraud risk verdict. Detection is rule-based by design choice (not ML): a fixed set of explicit, explainable rules (amount thresholds, transaction velocity, geo mismatch, odd hours) each contribute a score, and the scores are summed against a threshold to decide if a transaction is flagged. Explainability (which rule(s) fired and why) matters as much as the verdict itself.

Beyond the synchronous verdict, every fraud check is published as an event to a queue. A separate worker consumes that queue and, for transactions flagged fraudulent, asks a local LLM to explain in plain language why it was flagged, then persists that explanation to a local file named by transaction ID. This is async and decoupled from the request path on purpose — the LLM call is too slow to sit in the synchronous fraud-check response.

## Architecture

Core flow: `Api` receives a transaction, builds a `FraudCheckContext`, passes it to `FraudDetectionEngine` (in `Domain`), which runs every registered `IFraudRule` and aggregates the result. The Api then publishes a `FraudCheckNotification` (the transaction plus the result) to an SQS queue before returning the HTTP response. `Worker` is a separate, independently-run process that consumes that queue and produces LLM explanations for fraudulent findings.

### Domain (`src/TransactionFraudDetection.Domain`)

- `Models.Transaction` — a single transaction: `Id`, `AccountId`, `Amount`, `Currency`, `Timestamp`, `MerchantCountry`, `AccountHomeCountry`.
- `FraudCheckContext` — the unit of work passed to the engine: the `Transaction` under review plus `RecentTransactions` (that account's recent history, needed for velocity checks).
- `FraudCheckResult` — the verdict: `IsFraudulent`, `RiskScore`, `TriggeredReasons`.
- `FraudCheckNotification` — `Transaction` + `FraudCheckResult` combined. This is the shared contract serialized as the SQS message body; both `Api` (producer) and `Worker` (consumer) depend on it living in `Domain` so the message shape can't drift between the two processes.
- `Rules.IFraudRule` — one rule per fraud signal. `Evaluate(FraudCheckContext)` returns a `FraudRuleResult(Triggered, Reason, Score)`. Each rule is independent and stateless; new signals are added by adding a new `IFraudRule` implementation, not by branching inside existing rules.
- Rules: `HighAmountRule` (amount over a threshold), `VelocityRule` (too many transactions in a short window), `GeoMismatchRule` (merchant country differs from the account's home country), `OddHoursRule` (transaction during a high-risk overnight window). Each owns its own threshold/score constants.
- `FraudDetectionEngine` — takes all `IFraudRule`s via DI, evaluates each against the context, sums the scores of triggered rules, and compares against a risk threshold to produce the `FraudCheckResult`.

`Domain` has zero dependencies on `Api` or `Worker`, so the rule set and the message contract are testable without spinning up a host or a queue.

### Messaging (`src/TransactionFraudDetection.Messaging`)

Shared SQS plumbing used by both `Api` and `Worker`, so the queue-connection and queue-URL-resolution behavior can't drift between the producer and the consumer.

- `SqsOptions` — `ServiceUrl`, `Region`, `AccessKey`, `SecretKey`, `QueueName`. Bound from each process's own `appsettings.json` `"Sqs"` section (see Configuration below) — the shape is shared, the config file is not.
- `SqsClientFactory.Create(SqsOptions)` — builds an `IAmazonSQS` pointed at LocalStack with the configured dummy credentials.
- `SqsQueueResolver` — wraps the idempotent "create queue, cache its URL" pattern (SQS `CreateQueue` is a no-op if the queue already exists) so neither `SqsFindingsPublisher` nor `FindingsQueueListener` duplicate that caching logic.

### Api (`src/TransactionFraudDetection.Api`)

- Registers each rule as `IFraudRule` (`AddSingleton`) plus the engine, and exposes `POST /api/fraud-check`: accepts a `FraudCheckContext`, evaluates it, publishes the resulting `FraudCheckNotification` via `SqsFindingsPublisher`, then returns the `FraudCheckResult`. No controllers — minimal API route registration in `Program.cs`.
- `SqsFindingsPublisher` takes the shared `IAmazonSQS` + `SqsQueueResolver` and sends the serialized notification to the resolved queue URL.
- The SQS client is configured to talk to LocalStack (`http://localhost:4566`) with dummy credentials — there is no real AWS account involved. If LocalStack isn't running, publishing (and therefore the whole request) fails.

### Worker (`src/TransactionFraudDetection.Worker`)

A standalone console app, run as its own process — not embedded in the Api.

- `FindingsQueueListener` long-polls the `fraud-check-findings` queue, deserializes each message into a `FraudCheckNotification`, hands it to `FraudExplanationProcessor`, then deletes the message.
- `FraudExplanationProcessor` is the orchestration core: if `Result.IsFraudulent` is false, it does nothing (no LLM call, no file). If true, it builds a prompt via `FraudExplanationPromptBuilder`, calls `IFraudExplainer.ExplainAsync`, and writes the result via `ExplanationFileWriter`.
- `IFraudExplainer` is the one seam in the Worker with an interface — it exists so `FraudExplanationProcessor` can be unit tested with a fake instead of a real LLM call. `OllamaFraudExplainer` is the real implementation: calls a local Ollama instance (model and base URL from `OllamaOptions`, `"think": false` to skip reasoning output) and returns the `response` field.
- `ExplanationFileWriter` and `FindingsQueueListener` are concrete classes, not interfaces — they're IO boundaries (filesystem, SQS) that get exercised directly against a temp directory or real LocalStack rather than mocked.
- Output: `{OutputDirectory}/{transactionId}.json` (`OutputDirectory` from config, relative to the working directory the worker is started from), containing the `FraudCheckNotification` plus the explanation text. Only written for fraudulent findings.
- Unlike the Api, `Worker` isn't an ASP.NET Core host, so it builds its own `IConfiguration` by hand in `Program.cs` (`ConfigurationBuilder` + `AddJsonFile("appsettings.json")` + `AddEnvironmentVariables()`) to get the same appsettings-plus-env-var-override behavior the Api gets for free from `WebApplication.CreateBuilder`.

### Configuration

Both `Api` and `Worker` read settings from their own `appsettings.json` (never hardcoded in source) with environment-variable overrides via the standard ASP.NET Core double-underscore convention, e.g. `Sqs__ServiceUrl=...`, `Ollama__Model=...` — no `.env` file or extra parsing library needed, since `Microsoft.Extensions.Configuration`'s environment-variables provider already does this.

- Api's `appsettings.json`: `"Sqs"` section (`SqsOptions` shape).
- Worker's `appsettings.json`: `"Sqs"` section, plus `"Ollama"` (`OllamaOptions`: `BaseUrl`, `Model`) and `"OutputDirectory"`.

## Solution layout

- `TransactionFraudDetection.slnx` — solution file (.slnx format, not the older .sln XML format)
- `src/TransactionFraudDetection.Api` — ASP.NET Core Web API host (`Microsoft.NET.Sdk.Web`), minimal API style (no controllers). References `Domain` and `Messaging`.
- `src/TransactionFraudDetection.Domain` — class library holding the fraud detection logic (rules, models, the scoring engine, the `FraudCheckNotification` message contract). No dependencies on `Api`, `Worker`, or `Messaging` — keep it that way so it stays host-agnostic and unit-testable in isolation.
- `src/TransactionFraudDetection.Messaging` — class library holding the shared SQS plumbing (`SqsOptions`, `SqsClientFactory`, `SqsQueueResolver`). No dependency on `Domain` — it knows nothing about fraud checks, only about resolving/connecting to a named queue.
- `src/TransactionFraudDetection.Worker` — console app that consumes the findings queue and produces LLM explanations. References `Domain` and `Messaging`.
- `tests/TransactionFraudDetection.Domain.Tests` — xUnit tests for the `Domain` project.
- `tests/TransactionFraudDetection.Worker.Tests` — xUnit tests for the `Worker` project (prompt building and explanation orchestration, via a fake `IFraudExplainer`).
- `docker-compose.yml` — LocalStack (SQS only), pinned to `localstack/localstack:3.0.2` (the unauthenticated community image; `:latest` requires a license token and fails to start without one).

Dependency direction is `Api -> Domain`, `Api -> Messaging`, `Worker -> Domain`, `Worker -> Messaging`, `Domain.Tests -> Domain`, `Worker.Tests -> Worker`. `Domain` and `Messaging` have no project references between them or to anything else.

## Commands

```bash
dotnet build                                                   # build the whole solution
dotnet test                                                    # run all tests
dotnet test --filter "FullyQualifiedName~HighAmountRule"       # run a single test/class
docker compose up -d                                           # start LocalStack (SQS) before running the Api or Worker
dotnet run --project src/TransactionFraudDetection.Api         # run the API locally
dotnet run --project src/TransactionFraudDetection.Worker      # run the worker locally (needs Ollama serving qwen3:8b at localhost:11434)
```

There is no separate lint command configured; `dotnet build` surfaces compiler warnings (nullable reference warnings are enabled via `<Nullable>enable</Nullable>` in every project).

## Conventions

- Target framework is `net10.0` across all projects, with `ImplicitUsings` and `Nullable` both enabled — keep new projects consistent with this.
- The Api project uses minimal APIs (top-level route registrations in `Program.cs`), not MVC controllers.
