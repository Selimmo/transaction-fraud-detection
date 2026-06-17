# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A .NET 10 solution for a transaction fraud detection service. The goal is a Web API that a payment system can call synchronously while authorizing a transaction, to get back a fraud risk verdict. Detection is rule-based by design choice (not ML): a fixed set of explicit, explainable rules (amount thresholds, transaction velocity, geo mismatch, odd hours) each contribute a score, and the scores are summed against a threshold to decide if a transaction is flagged. Explainability (which rule(s) fired and why) matters as much as the verdict itself.

## Architecture

> Status: scaffold only — the types below are the agreed design, not yet implemented. This section is the spec to build against; update it if the design changes during implementation.

Core flow: `Api` receives a transaction, builds a `FraudCheckContext`, passes it to `FraudDetectionEngine` (in `Domain`), which runs every registered `IFraudRule` and aggregates the result.

- `Models.Transaction` — a single transaction: `Id`, `AccountId`, `Amount`, `Currency`, `Timestamp`, `MerchantCountry`, `AccountHomeCountry`.
- `FraudCheckContext` — the unit of work passed to the engine: the `Transaction` under review plus `RecentTransactions` (that account's recent history, needed for velocity checks).
- `Rules.IFraudRule` — one rule per fraud signal. `Evaluate(FraudCheckContext)` returns a `FraudRuleResult(Triggered, Reason, Score)`. Each rule is independent and stateless; new signals are added by adding a new `IFraudRule` implementation, not by branching inside existing rules.
- Planned rules: `HighAmountRule` (amount over a threshold), `VelocityRule` (too many transactions in a short window), `GeoMismatchRule` (merchant country differs from the account's home country), `OddHoursRule` (transaction during a high-risk overnight window). Each owns its own threshold/score constants.
- `FraudDetectionEngine` — takes all `IFraudRule`s via DI, evaluates each against the context, sums the scores of triggered rules, and compares against a risk threshold to produce a `FraudCheckResult(IsFraudulent, RiskScore, TriggeredReasons)`.
- `Api` — registers each rule as `IFraudRule` (`AddSingleton`) plus the engine, and exposes a `POST` endpoint that accepts a `FraudCheckContext` and returns the `FraudCheckResult`. No controllers — minimal API route registration in `Program.cs`.

`Domain` has zero dependencies on `Api`, so the whole rule set is testable without spinning up a host.

## Solution layout

- `TransactionFraudDetection.slnx` — solution file (.slnx format, not the older .sln XML format)
- `src/TransactionFraudDetection.Api` — ASP.NET Core Web API host (`Microsoft.NET.Sdk.Web`), minimal API style (no controllers). References `Domain`.
- `src/TransactionFraudDetection.Domain` — class library holding the fraud detection logic (rules, models, the scoring engine). No dependencies on the Api project — keep it that way so the rules stay host-agnostic and unit-testable in isolation.
- `tests/TransactionFraudDetection.Domain.Tests` — xUnit tests for the `Domain` project.

Dependency direction is `Api -> Domain` and `Domain.Tests -> Domain`. `Domain` has no project references.

## Commands

```bash
dotnet build                                                   # build the whole solution
dotnet test                                                    # run all tests
dotnet test --filter "FullyQualifiedName~HighAmountRule"       # run a single test/class
dotnet run --project src/TransactionFraudDetection.Api         # run the API locally
```

There is no separate lint command configured; `dotnet build` surfaces compiler warnings (nullable reference warnings are enabled via `<Nullable>enable</Nullable>` in every project).

## Conventions

- Target framework is `net10.0` across all projects, with `ImplicitUsings` and `Nullable` both enabled — keep new projects consistent with this.
- The Api project uses minimal APIs (top-level route registrations in `Program.cs`), not MVC controllers.
