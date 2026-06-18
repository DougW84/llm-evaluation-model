# LLM Evaluation Pipeline (Portfolio Demo)

A small C# console app demonstrating **Evaluation-Driven Development (EDD)** for a mock StarRez-style university housing assistant. It runs a set of tests through mock RAG retrieval, scores responses with an **LLM-as-judge**, applies deterministic guardrails, and gates on quality thresholds.

## What this demonstrates

- **EDD:** acceptance criteria defined before build — ship/no-ship is a measurement, not a meeting
- **RAG evaluation:** judge scores traceability to *retrieved* context, not general model knowledge
- **Layered QA:** deterministic guardrails (escalation, PII, out-of-scope actions) + probabilistic judge (grounding, accuracy, tone)

## EDD acceptance criteria (demo thresholds)

| Dimension | Threshold | How measured |
|-----------|-----------|--------------|
| Grounding | avg >= 4.0/5 | LLM judge rubric |
| Accuracy | >= 80% pass rate | Judge + labeled test cases |
| Escalation | 100% pass rate | Guardrail: must hand off to advisor |
| Tone | avg >= 4.0/5 | LLM judge rubric |
| Latency | p95 < 3000ms | Stopwatch per case |

Exit code `1` on threshold breach — same mental model as a failing xUnit test blocking merge.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- **Anthropic API key** (required to run evaluations — see Setup below)

## Setup

### 1. Clone and open the project

```powershell
git clone https://github.com/DougW84/llm-evaluation-model.git
cd llm-evaluation-model
```

### 2. Add your Anthropic API key

This project calls the **Anthropic API** to run an **LLM-as-judge** - Claude scores each test response on grounding, accuracy, tone, and escalation. In `--live` mode, Claude also generates the assistant responses.

This is **not** the same as a Claude.ai chat subscription. You need a separate API key from the Anthropic developer console.

1. Go to [console.anthropic.com](https://console.anthropic.com/) and sign in (or create an account).
2. Open **API Keys** and create a new key.
3. Copy the key - it is only shown once.

API usage is billed per request. Replay mode makes ~16 judge calls per run; `--live` mode makes additional calls to generate responses.

### 3. Save the key locally

```powershell
copy .env.example .env
```

Edit `.env` and replace the placeholder with your key:

```
ANTHROPIC_API_KEY=sk-ant-api03-your-key-here
```

`.env` is gitignored and will not be committed. Never push your API key to GitHub.

## Run

```powershell
# Replay mode — uses pre-baked AI responses (cheaper, good for demos)
dotnet run --project src/EvalRunner

# Live mode — mock RAG + Claude generates responses, then judge scores them
dotnet run --project src/EvalRunner -- --live

# Filter by category
dotnet run --project src/EvalRunner -- --category escalation

# Unit tests (no API key needed)
dotnet test
```

### Exit codes

| Code | Meaning |
|------|---------|
| `0` | All eval cases passed detection (judge/guardrails behaved as expected) |
| `1` | One or more eval cases failed detection, or missing API key / data files |
| `2` | **Anthropic API error** — run stopped early (billing, auth, rate limit). Remaining cases skipped. |

### Reading the report

Each case has two layers:

- **Detection** — did the eval pipeline correctly flag (or accept) the response? e.g. `Test passed — grounding response failure detected OK`
- **Response quality** — did the AI answer meet score thresholds? e.g. `Response quality: FAIL`

Negative golden cases (deliberately bad `aiResponse` values) should show detection **passed** even when response quality **failed**.

Example output for test 001:

```
TEST 001 [grounding] givenStudentNotEligibleForSingleRoom_whenAiResponseStatesSingleRoomOk_thenGroundingFailureDetected: Test passed — grounding response failure detected OK
  Response quality: FAIL
  Grounding: 1/5 — ...
```

If credits run out or your API key is invalid, you'll see a banner like:

```
*** ANTHROPIC API ERROR — RUN STOPPED ***

Billing error — your Anthropic credit balance may be exhausted. Add credits at https://console.anthropic.com/settings/billing
Failed on test case: 005
Completed 4 of 16 cases before stopping.

No further API calls will be made. Remaining test cases were skipped.
```

## Project structure

```
src/EvalRunner/
  Data/           TestCases.json + KnowledgeBase.json (mock RAG source)
  Rag/            MockRagRetriever
  Assistant/      AssistantClient (--live mode)
  Judge/          LlmJudge + rubric prompt
  Guardrails/     Deterministic safety checks
  Reporting/      Threshold gates + console report
```

## Notes

- **EDD:** Grounding, accuracy, escalation, tone, and latency thresholds are defined before building. Evaluation is then run when new features are ready.
- **RAG eval:** The judge scores against retrieved context. Test 001 is deliberately designed to contradict the context, to prove the evaluator catches hallucinations.
- Replay mode still calls the **judge** API once per test case (~16 calls per run).
- Results are written to a timestamped file under `results/` (e.g. `eval-results-2026-06-18_143022.json`) for drift tracking later. Override with `--output path/to/file.json`.
- Stretch goals: GitHub Actions CI gate, judge consistency checks, mock tool-call assertions.
- This project also contains tests for the AnthropicApiExceptionHandler. To run the tests in Powershell, change to `llm-evaluation-model\tests\EvalRunner.Tests` and run `dotnet test --filter "FullyQualifiedName~AnthropicApiExceptionTests"`
