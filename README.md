# Transaction Fraud Detection

A .NET 10 service that gives a payment system a real-time fraud verdict on a transaction, and separately produces a plain-language explanation for every transaction it flags — without slowing down the verdict itself.

## What this is

A payment system calls this service synchronously, while it's authorizing a transaction, to ask: *is this fraudulent?* The answer has to come back fast, so detection here is rule-based rather than ML-based: a fixed set of explicit, explainable rules each contribute a score, and the scores are summed against a threshold to produce a verdict. There's no model to retrain or explain after the fact — every flagged transaction already comes with the list of rules that fired.

Current rules:

| Rule | Triggers when | Score |
|---|---|---|
| `HighAmountRule` | Amount exceeds 5,000 | 50 |
| `VelocityRule` | 3+ transactions for the same account within 5 minutes | 40 |
| `GeoMismatchRule` | Merchant country differs from the account's home country | 30 |
| `OddHoursRule` | Transaction falls between 01:00–05:00 | 20 |

A transaction is flagged fraudulent once its triggered rules sum to 50 or more. Adding a new signal means adding a new rule, not touching existing ones.

## Why sync *and* async

The fraud verdict has to be synchronous — the payment system is blocked on it mid-authorization, so it has to be cheap and fast. But once a transaction is flagged, there's a second, much more useful question: *why*? Producing a good natural-language explanation means a call to an LLM, and LLM calls are too slow (seconds, not milliseconds) to sit in that authorization path.

So the two concerns are split:

- **Sync**: `Api` evaluates the rules and returns `IsFraudulent` / `RiskScore` / `TriggeredReasons` immediately.
- **Async**: every check (not just flagged ones) is also published as an event to a queue. A separate `Worker` process consumes that queue, and — only for transactions flagged fraudulent — asks a local LLM to explain the verdict in plain language, then persists the explanation to a file keyed by transaction ID.

This isn't just a way to defer slow work. Once the verdict is on a queue, anything else that needs to react to a fraud finding can subscribe without ever touching the authorization path or adding latency to it. The explanation worker is the first consumer, but the same event could just as easily fan out to:

- A second-pass ML risk model that re-scores the transaction with a heavier, slower model than the rule engine could afford to run synchronously.
- A human-in-the-loop review queue for borderline scores — cases the rules aren't confident about, routed to an analyst instead of auto-decided.
- Audit logging, customer notifications, case-management ticket creation, or feeding a fraud-pattern analytics pipeline.

None of that requires changing how the Api responds to the payment system. The queue is the seam that keeps the synchronous path simple and lets everything downstream evolve independently.

## The stack

- **.NET 10 / C#**, built test-first (xUnit) for the rule engine and worker orchestration.
- **`Api`** — ASP.NET Core minimal API (no controllers), exposes `POST /api/fraud-check`.
- **`Domain`** — the rule engine, models, and the `FraudCheckNotification` message contract. No framework or infrastructure dependencies — it's plain C#, which is what makes it fast to unit test.
- **`Messaging`** — shared SQS plumbing (connecting, resolving/creating the queue) used by both `Api` and `Worker`, so they can't drift apart on how they talk to the queue.
- **`Worker`** — a standalone console process that consumes the queue, calls a local LLM, and writes the explanation to disk.
- **AWS SQS** for the queue. Locally this runs against **LocalStack** (no real AWS account needed); in a deployed environment it talks to real SQS over IAM-role credentials. See [Local vs. deployed credentials](#local-vs-deployed-credentials) below.
- **Ollama**, running the `qwen3:8b` model locally, generates the fraud explanations.

## Running it end to end

You'll need [.NET 10](https://dotnet.microsoft.com/), [Docker](https://www.docker.com/), and [Ollama](https://ollama.com/) installed.

**1. Pull the LLM model** (one-time):

```bash
ollama pull qwen3:8b
```

Ollama needs to be running and serving on `http://localhost:11434` (this is its default once installed).

**2. Start LocalStack** (simulates SQS):

```bash
docker compose up -d
```

**3. Run the Api** (in one terminal):

```bash
dotnet run --project src/TransactionFraudDetection.Api
```

This listens on `http://localhost:5036` and uses `ASPNETCORE_ENVIRONMENT=Development` (set in `launchSettings.json`), which is what makes it talk to LocalStack instead of real AWS.

**4. Run the Worker** (in a second terminal):

```bash
DOTNET_ENVIRONMENT=Development dotnet run --project src/TransactionFraudDetection.Worker
```

It long-polls the queue and will sit waiting for messages. Leave it running.

**5. Send a transaction:**

```bash
curl http://localhost:5036/api/fraud-check -X POST -H "Content-Type: application/json" -d '{
  "transaction": {
    "id": "txn-1",
    "accountId": "acct-1",
    "amount": 9000.00,
    "currency": "USD",
    "timestamp": "2024-01-01T03:00:00Z",
    "merchantCountry": "RU",
    "accountHomeCountry": "US"
  },
  "recentTransactions": []
}'
```

The Api responds immediately with the verdict. A few seconds later, the Worker (still running in the other terminal) will pick up the corresponding queue message and write `output/txn-1.json`, relative to wherever you started the Worker from (the repo root, if you followed step 4), containing the transaction, the verdict, and the LLM's explanation. Nothing is written for transactions that aren't flagged fraudulent.

**6. Shut down:**

```bash
# Ctrl+C both the Api and Worker terminals, then:
docker compose down
```

### Tests

```bash
dotnet build      # build everything
dotnet test       # run the full test suite (Domain + Worker)
```

Tests don't touch real LocalStack or Ollama — IO boundaries (the LLM call, the filesystem) are exercised through fakes/temp directories instead.

## Local vs. deployed credentials

`Api` and `Worker` both decide how to connect to SQS based on which environment they're running in (`IAppEnvironment`, in `Messaging`):

- **Development** (`appsettings.Development.json`): connects to LocalStack at `http://localhost:4566` using dummy credentials (`test`/`test`) checked into config — there's nothing real to protect locally.
- **Anything else** (`appsettings.Production.json`): no `ServiceUrl` or credentials are configured at all. The AWS SDK falls back to its default credential chain — an IAM role, in a real deployment — so there's no secret to manage or rotate in config.

This means the only thing that changes between local and deployed is which `appsettings.{Environment}.json` file loads — the code path is identical either way.

In Development, `SqsQueueResolver` also provisions a dead-letter queue (`{queue}-dlq`) and a `RedrivePolicy` on the main queue, so messages that fail repeatedly stop being redelivered forever and land somewhere inspectable. In a deployed environment this is expected to be provisioned via infrastructure-as-code instead.

If publishing a finding to SQS fails outright (e.g. the queue is unreachable), the Api still returns the verdict — a queue outage shouldn't fail an authorization the payment system is blocked on. The notification is archived to `failed-publishes/` instead of being lost.
