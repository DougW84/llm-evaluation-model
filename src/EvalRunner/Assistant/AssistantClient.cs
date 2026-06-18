using System.Text;
using System.Text.Json;
using EvalRunner.Anthropic;

namespace EvalRunner.Assistant;

public class AssistantClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public AssistantClient(HttpClient httpClient, string apiKey, string? model = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model ?? AnthropicSettings.GetModel();
    }

    public async Task<string> GenerateResponseAsync(string query, string retrievedContext, CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
            You are a helpful university housing assistant. Answer only using the retrieved context below.
            If the context is insufficient or the question involves ambiguous eligibility, recommend speaking with a housing advisor.
            Do not make up student data. Be professional and concise.
            """;

        var userPrompt = $"""
            Retrieved context:
            {retrievedContext}

            Student question: {query}
            """;

        var requestBody = new
        {
            model = _model,
            max_tokens = 1024,
            temperature = 0.3,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        AnthropicHttp.EnsureSuccess((int)response.StatusCode, body);

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
