using EvalRunner.Anthropic;
using EvalRunner.Assistant;
using EvalRunner.Judge;
using EvalRunner.Models;
using EvalRunner.Rag;
using EvalRunner.Reporting;

namespace EvalRunner;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var liveMode = args.Contains("--live", StringComparer.OrdinalIgnoreCase);
        var categoryFilter = GetArgValue(args, "--category");
        var outputPath = GetArgValue(args, "--output") ?? GetDefaultResultsPath();

        var apiKey = EnvLoader.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.Error.WriteLine("ANTHROPIC_API_KEY not set. Copy .env.example to .env and add your key.");
            return 1;
        }

        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        var testCasesPath = Path.Combine(dataDir, "TestCases.json");
        var knowledgeBasePath = Path.Combine(dataDir, "KnowledgeBase.json");

        if (!File.Exists(testCasesPath) || !File.Exists(knowledgeBasePath))
        {
            Console.Error.WriteLine($"Data files not found in {dataDir}. Run from project root or rebuild.");
            return 1;
        }

        var testCases = DataLoader.LoadTestCases(testCasesPath);
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            testCases = testCases
                .Where(t => t.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Console.WriteLine($"Running evaluation: {testCases.Count} cases | Mode: {(liveMode ? "LIVE" : "REPLAY")}");

        var retriever = MockRagRetriever.LoadFromFile(knowledgeBasePath);
        using var httpClient = new HttpClient();
        var judge = new LlmJudge(httpClient, apiKey);
        AssistantClient? assistant = liveMode ? new AssistantClient(httpClient, apiKey) : null;

        var pipeline = new EvalPipeline(retriever, judge, new ThresholdConfig(), liveMode, assistant);

        IReadOnlyList<EvalRunResult> results;
        try
        {
            results = await pipeline.RunAsync(testCases);
        }
        catch (AnthropicApiException ex) when (ex.StopsRun)
        {
            ex.WriteFatalBanner(Console.Error);
            return 2;
        }

        var summary = ThresholdGate.Evaluate(results, new ThresholdConfig());

        ConsoleReporter.PrintResults(results, summary);
        ConsoleReporter.WriteJsonResults(outputPath, results, summary);

        return summary.AllPassed ? 0 : 1;
    }

    private static string GetDefaultResultsPath()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        return $"results/eval-results-{timestamp}.json";
    }

    private static string? GetArgValue(string[] args, string flag)
    {
        var index = Array.FindIndex(args, a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        if (index >= 0 && index + 1 < args.Length)
        {
            return args[index + 1];
        }

        return null;
    }
}
