using System.Diagnostics;
using System.Text.Json;
using EvalRunner.Assistant;
using EvalRunner.Guardrails;
using EvalRunner.Judge;
using EvalRunner.Models;
using EvalRunner.Rag;
using EvalRunner.Reporting;

namespace EvalRunner;

public class EvalPipeline
{
    private readonly MockRagRetriever _retriever;
    private readonly LlmJudge _judge;
    private readonly AssistantClient? _assistant;
    private readonly ResponseGuardrails _guardrails = new();
    private readonly ThresholdConfig _thresholdConfig;
    private readonly bool _liveMode;

    public EvalPipeline(
        MockRagRetriever retriever,
        LlmJudge judge,
        ThresholdConfig thresholdConfig,
        bool liveMode,
        AssistantClient? assistant = null)
    {
        _retriever = retriever;
        _judge = judge;
        _thresholdConfig = thresholdConfig;
        _liveMode = liveMode;
        _assistant = assistant;
    }

    public async Task<IReadOnlyList<EvalRunResult>> RunAsync(IReadOnlyList<TestCase> testCases, CancellationToken cancellationToken = default)
    {
        var results = new List<EvalRunResult>();

        foreach (var testCase in testCases)
        {
            var result = await RunCaseAsync(testCase, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<EvalRunResult> RunCaseAsync(TestCase testCase, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = new EvalRunResult { TestCase = testCase };

        try
        {
            result.RetrievedContext = !string.IsNullOrWhiteSpace(testCase.RetrievedContext)
                ? testCase.RetrievedContext
                : _retriever.Retrieve(testCase.Query, testCase.StudentId);

            if (_liveMode)
            {
                if (_assistant is null)
                {
                    throw new InvalidOperationException("Live mode requires AssistantClient.");
                }

                result.AiResponse = await _assistant.GenerateResponseAsync(testCase.Query, result.RetrievedContext, cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(testCase.AiResponse))
                {
                    throw new InvalidOperationException($"Test case {testCase.Id} has no aiResponse for replay mode.");
                }

                result.AiResponse = testCase.AiResponse;
            }

            var validation = _guardrails.Validate(testCase, result.AiResponse, result.RetrievedContext, testCase.StudentId);
            result.GuardrailViolations = validation.Violations;

            result.Score = await _judge.EvaluateAsync(testCase, result.RetrievedContext, result.AiResponse, cancellationToken);
            result.Passed = ThresholdGate.CasePassed(result, _thresholdConfig);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Passed = false;
        }
        finally
        {
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
        }

        return result;
    }
}

public static class DataLoader
{
    public static List<TestCase> LoadTestCases(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<TestCase>>(json, JsonOptions())
            ?? throw new InvalidOperationException($"Failed to load test cases from {path}");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public static class EnvLoader
{
    public static string? GetApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".env"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"))
        };

        foreach (var path in candidates.Select(Path.GetFullPath).Distinct())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("ANTHROPIC_API_KEY=", StringComparison.Ordinal))
                {
                    return line["ANTHROPIC_API_KEY=".Length..].Trim().Trim('"');
                }
            }
        }

        return null;
    }
}
