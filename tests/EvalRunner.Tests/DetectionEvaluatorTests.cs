using EvalRunner.Models;
using EvalRunner.Reporting;

namespace EvalRunner.Tests;

public class DetectionEvaluatorTests
{
    [Fact]
    public void NegativeCase_WhenResponseFailsQuality_DetectionPasses()
    {
        var result = new EvalRunResult
        {
            TestCase = new TestCase
            {
                Id = "001",
                Name = "givenStudentNotEligibleForSingleRoom_whenAiResponseStatesSingleRoomOk_thenGroundingFailureDetected",
                Category = "grounding",
                ExpectsResponsePass = false
            },
            Passed = false,
            Score = new EvalScore { Grounding = 1, Accuracy = 1, Tone = 2, EscalationHandling = 2, GroundingReason = "Contradicts context." }
        };
        result.DetectionPassed = DetectionEvaluator.EvaluateDetection(result);

        Assert.True(result.DetectionPassed);
        Assert.Contains("Test passed", DetectionEvaluator.FormatHeader(result));
        Assert.Contains("grounding response failure detected OK", DetectionEvaluator.FormatHeader(result));
    }

    [Fact]
    public void PositiveCase_WhenResponsePassesQuality_DetectionPasses()
    {
        var result = new EvalRunResult
        {
            TestCase = new TestCase
            {
                Id = "002",
                Name = "givenStudentWithZeroBalance_whenAiResponseStatesRentAndBalance_thenAccuracyPasses",
                Category = "accuracy",
                ExpectsResponsePass = true
            },
            Passed = true,
            Score = new EvalScore { Grounding = 5, Accuracy = 5, Tone = 5, EscalationHandling = 5, GroundingReason = "Fully grounded." }
        };
        result.DetectionPassed = DetectionEvaluator.EvaluateDetection(result);

        Assert.True(result.DetectionPassed);
        Assert.Contains("accuracy response quality OK", DetectionEvaluator.FormatHeader(result));
    }

    [Fact]
    public void NegativeCase_WhenResponseWronglyPasses_DetectionFails()
    {
        var result = new EvalRunResult
        {
            TestCase = new TestCase
            {
                Id = "001",
                Name = "givenStudentNotEligibleForSingleRoom_whenAiResponseStatesSingleRoomOk_thenGroundingFailureDetected",
                Category = "grounding",
                ExpectsResponsePass = false
            },
            Passed = true,
            Score = new EvalScore { Grounding = 5, Accuracy = 5, Tone = 5, EscalationHandling = 5, GroundingReason = "OK" }
        };
        result.DetectionPassed = DetectionEvaluator.EvaluateDetection(result);

        Assert.False(result.DetectionPassed);
        Assert.Contains("MISSED", DetectionEvaluator.FormatHeader(result));
    }
}
