using EvalRunner.Models;

namespace EvalRunner.Reporting;

public class ThresholdSummary
{
    public bool AllPassed { get; set; }
    public List<string> Breaches { get; set; } = [];
    public double AvgGrounding { get; set; }
    public double AvgTone { get; set; }
    public double AccuracyPassRate { get; set; }
    public double EscalationPassRate { get; set; }
    public long P95LatencyMs { get; set; }
    public int TotalPassed { get; set; }
    public int TotalCases { get; set; }
}

public static class ThresholdGate
{
    public static ThresholdSummary Evaluate(IReadOnlyList<EvalRunResult> results, ThresholdConfig config)
    {
        var summary = new ThresholdSummary
        {
            TotalCases = results.Count,
            TotalPassed = results.Count(r => r.Passed)
        };

        var scored = results.Where(r => r.Score is not null).ToList();
        if (scored.Count > 0)
        {
            summary.AvgGrounding = scored.Average(r => r.Score!.Grounding);
            summary.AvgTone = scored.Average(r => r.Score!.Tone);
        }

        var accuracyCases = results.Where(r => r.TestCase.Category.Equals("accuracy", StringComparison.OrdinalIgnoreCase)).ToList();
        if (accuracyCases.Count > 0)
        {
            summary.AccuracyPassRate = (double)accuracyCases.Count(r => r.Passed) / accuracyCases.Count;
        }
        else
        {
            summary.AccuracyPassRate = 1.0;
        }

        var escalationCases = results.Where(r => r.TestCase.Category.Equals("escalation", StringComparison.OrdinalIgnoreCase)).ToList();
        if (escalationCases.Count > 0)
        {
            summary.EscalationPassRate = (double)escalationCases.Count(r => r.Passed) / escalationCases.Count;
        }
        else
        {
            summary.EscalationPassRate = 1.0;
        }

        var latencies = results.Select(r => r.ElapsedMs).OrderBy(x => x).ToList();
        summary.P95LatencyMs = latencies.Count > 0
            ? latencies[(int)Math.Ceiling(latencies.Count * 0.95) - 1]
            : 0;

        var breaches = new List<string>();

        if (scored.Count > 0 && summary.AvgGrounding < config.MinAvgGrounding)
        {
            breaches.Add($"Avg grounding {summary.AvgGrounding:F1} (min {config.MinAvgGrounding:F1} required)");
        }

        if (scored.Count > 0 && summary.AvgTone < config.MinAvgTone)
        {
            breaches.Add($"Avg tone {summary.AvgTone:F1} (min {config.MinAvgTone:F1} required)");
        }

        if (summary.AccuracyPassRate < config.MinAccuracyPassRate)
        {
            breaches.Add($"Accuracy pass rate {summary.AccuracyPassRate:P0} (min {config.MinAccuracyPassRate:P0} required)");
        }

        if (summary.EscalationPassRate < config.MinEscalationPassRate)
        {
            breaches.Add($"Escalation pass rate {summary.EscalationPassRate:P0} (min {config.MinEscalationPassRate:P0} required)");
        }

        if (summary.P95LatencyMs > config.MaxP95LatencyMs)
        {
            breaches.Add($"P95 latency {summary.P95LatencyMs}ms (max {config.MaxP95LatencyMs}ms allowed)");
        }

        summary.Breaches = breaches;
        summary.AllPassed = breaches.Count == 0;
        return summary;
    }

    public static bool CasePassed(EvalRunResult result, ThresholdConfig config)
    {
        if (result.GuardrailViolations.Count > 0)
        {
            return false;
        }

        if (result.Score is null)
        {
            return false;
        }

        var minScore = config.MinPassingScore;
        return result.Score.Grounding >= minScore
            && result.Score.Accuracy >= minScore
            && result.Score.Tone >= minScore
            && result.Score.EscalationHandling >= minScore;
    }
}
