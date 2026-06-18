using EvalRunner.Judge;
using EvalRunner.Models;
using EvalRunner.Reporting;

namespace EvalRunner.Tests;

public class ThresholdGateTests
{
    [Fact]
    public void CasePassed_FailsWhenGuardrailViolated()
    {
        var result = new EvalRunResult
        {
            TestCase = new TestCase { Id = "001", Category = "grounding" },
            Score = new EvalScore { Grounding = 5, Accuracy = 5, Tone = 5, EscalationHandling = 5 },
            GuardrailViolations = ["Out of scope action"]
        };

        Assert.False(ThresholdGate.CasePassed(result, new ThresholdConfig()));
    }

    [Fact]
    public void CasePassed_FailsWhenScoreBelowThreshold()
    {
        var result = new EvalRunResult
        {
            TestCase = new TestCase { Id = "001", Category = "grounding" },
            Score = new EvalScore { Grounding = 2, Accuracy = 5, Tone = 5, EscalationHandling = 5 }
        };

        Assert.False(ThresholdGate.CasePassed(result, new ThresholdConfig()));
    }

    [Fact]
    public void ParseScore_ExtractsJsonFromFencedResponse()
    {
        var response = """
            ```json
            {
              "grounding": 4,
              "accuracy": 5,
              "tone": 4,
              "escalationHandling": 5,
              "groundingReason": "Mostly grounded."
            }
            ```
            """;

        var score = LlmJudge.ParseScore(response);

        Assert.Equal(4, score.Grounding);
        Assert.Equal("Mostly grounded.", score.GroundingReason);
    }

    [Fact]
    public void ThresholdSummary_DetectsGroundingBreach()
    {
        var results = new List<EvalRunResult>
        {
            new()
            {
                Passed = false,
                TestCase = new TestCase { Category = "grounding" },
                Score = new EvalScore { Grounding = 2, Accuracy = 2, Tone = 2, EscalationHandling = 2 },
                ElapsedMs = 100
            },
            new()
            {
                Passed = false,
                TestCase = new TestCase { Category = "grounding" },
                Score = new EvalScore { Grounding = 2, Accuracy = 2, Tone = 2, EscalationHandling = 2 },
                ElapsedMs = 200
            }
        };

        var summary = ThresholdGate.Evaluate(results, new ThresholdConfig());

        Assert.False(summary.AllPassed);
        Assert.Contains(summary.Breaches, b => b.Contains("grounding", StringComparison.OrdinalIgnoreCase));
    }
}
