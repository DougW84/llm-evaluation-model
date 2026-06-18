using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EvalRunner.Models;

namespace EvalRunner.Judge;

public class LlmJudge
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public LlmJudge(HttpClient httpClient, string apiKey, string model = "claude-sonnet-4-20250514")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<EvalScore> EvaluateAsync(TestCase testCase, string retrievedContext, string aiResponse, CancellationToken cancellationToken = default)
    {
        var prompt = JudgePromptBuilder.Build(testCase, retrievedContext, aiResponse);
        var responseText = await CallAnthropicAsync(prompt, cancellationToken);
        return ParseScore(responseText);
    }

    private async Task<string> CallAnthropicAsync(string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            temperature = 0,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic API error ({(int)response.StatusCode}): {body}");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content ?? throw new InvalidOperationException("Empty response from Anthropic API.");
    }

    public static EvalScore ParseScore(string responseText)
    {
        var json = ExtractJson(responseText);
        var score = JsonSerializer.Deserialize<EvalScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (score is null)
        {
            throw new InvalidOperationException($"Failed to parse judge response: {responseText}");
        }

        ValidateScore(score.Grounding, nameof(score.Grounding));
        ValidateScore(score.Accuracy, nameof(score.Accuracy));
        ValidateScore(score.Tone, nameof(score.Tone));
        ValidateScore(score.EscalationHandling, nameof(score.EscalationHandling));

        return score;
    }

    private static string ExtractJson(string text)
    {
        var fenceMatch = Regex.Match(text, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
        if (fenceMatch.Success)
        {
            return fenceMatch.Groups[1].Value;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text[start..(end + 1)];
        }

        return text;
    }

    private static void ValidateScore(int value, string name)
    {
        if (value is < 1 or > 5)
        {
            throw new InvalidOperationException($"{name} score must be between 1 and 5, got {value}.");
        }
    }
}
