---
name: test
description: Run the xUnit test suite for the transaction-fraud-detection solution, including filtering to a single test or class. Applies to files under the transaction-fraud-detection project.
---

# Test TransactionFraudDetection

```bash
dotnet test                                                       # whole solution
dotnet test tests/TransactionFraudDetection.Domain.Tests          # just the Domain tests
dotnet test --filter "FullyQualifiedName~HighAmountRule"          # by class name substring
dotnet test --filter "DisplayName~should_flag_when_amount_exceeds_threshold"  # by exact test name
```

Tests live in `tests/TransactionFraudDetection.Domain.Tests`, mirroring the namespace structure of `src/TransactionFraudDetection.Domain` (e.g. `Rules/HighAmountRuleTests.cs` tests `Domain/Rules/HighAmountRule.cs`). The `Api` project has no tests of its own — it's a thin host over `Domain`, so behavior is tested at the `Domain` layer.
