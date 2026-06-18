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
- Anthropic API key

## Setup

```powershell
cd c:\Projects\llm-evaluation-model
copy .env.example .env
# Edit .env and set ANTHROPIC_API_KEY
```

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
