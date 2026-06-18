using System.Text.Json;
using EvalRunner.Models;

namespace EvalRunner.Reporting;

public static class ConsoleReporter
{
    public static void PrintResults(IReadOnlyList<EvalRunResult> results, ThresholdSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("=== Housing Assistant Evaluation Report ===");
        Console.WriteLine();

        foreach (var result in results)
        {
            PrintCase(result);
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {summary.TotalPassed}/{summary.TotalCases} passed");
        Console.WriteLine($"  Avg grounding: {summary.AvgGrounding:F1} (min 4.0) {(summary.AvgGrounding >= 4.0 ? "PASS" : "FAIL")}");
        Console.WriteLine($"  Avg tone: {summary.AvgTone:F1} (min 4.0) {(summary.AvgTone >= 4.0 ? "PASS" : "FAIL")}");
        Console.WriteLine($"  Accuracy cases: {summary.AccuracyPassRate:P0} pass rate");
        Console.WriteLine($"  Escalation cases: {summary.EscalationPassRate:P0} pass rate");
        Console.WriteLine($"  P95 latency: {summary.P95LatencyMs}ms");

        if (summary.Breaches.Count > 0)
        {
            Console.WriteLine();
            foreach (var breach in summary.Breaches)
            {
                Console.WriteLine($"THRESHOLD BREACH: {breach}");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("All thresholds PASSED.");
        }
    }

    private static void PrintCase(EvalRunResult result)
    {
        var status = result.Passed ? "PASS" : "FAIL";
        Console.WriteLine($"Test {result.TestCase.Id} [{result.TestCase.Category}]: {status}");

        if (!string.IsNullOrEmpty(result.Error))
        {
            Console.WriteLine($"  Error: {result.Error}");
            return;
        }

        if (result.Score is not null)
        {
            Console.WriteLine($"  Grounding: {result.Score.Grounding}/5 — {result.Score.GroundingReason}");
            Console.WriteLine($"  Accuracy: {result.Score.Accuracy}/5 | Tone: {result.Score.Tone}/5 | Escalation: {result.Score.EscalationHandling}/5");
        }

        if (result.GuardrailViolations.Count > 0)
        {
            Console.WriteLine($"  Guardrails: {string.Join("; ", result.GuardrailViolations)}");
        }
        else
        {
            Console.WriteLine("  Guardrails: none");
        }

        Console.WriteLine($"  Latency: {result.ElapsedMs}ms");
        Console.WriteLine();
    }

    public static void WriteJsonResults(string outputPath, IReadOnlyList<EvalRunResult> results, ThresholdSummary summary)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var payload = new
        {
            runAt = DateTime.UtcNow,
            summary,
            results = results.Select(r => new
            {
                r.TestCase.Id,
                r.TestCase.Category,
                r.Passed,
                r.ElapsedMs,
                r.GuardrailViolations,
                score = r.Score,
                r.Error
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Results written to {outputPath}");
    }
}
