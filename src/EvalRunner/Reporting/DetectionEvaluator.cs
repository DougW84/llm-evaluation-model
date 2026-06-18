using EvalRunner.Models;

namespace EvalRunner.Reporting;

public static class DetectionEvaluator
{
    public static bool EvaluateDetection(EvalRunResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            return false;
        }

        return result.Passed == result.TestCase.ExpectsResponsePass;
    }

    public static string FormatHeader(EvalRunResult result)
    {
        var verdict = result.DetectionPassed ? "Test passed" : "Test failed";
        var detail = FormatDetectionDetail(result);
        return $"TEST {result.TestCase.Id} [{result.TestCase.Category}] {result.TestCase.Name}: {verdict} — {detail}";
    }

    private static string FormatDetectionDetail(EvalRunResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            return "eval error";
        }

        var category = result.TestCase.Category;

        if (result.DetectionPassed)
        {
            return result.TestCase.ExpectsResponsePass
                ? $"{category} response quality OK"
                : $"{category} response failure detected OK";
        }

        return result.TestCase.ExpectsResponsePass
            ? $"FALSE POSITIVE: {category} response wrongly failed quality gates"
            : $"MISSED: {category} response failure not detected";
    }
}
