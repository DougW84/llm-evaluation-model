using System.Text.Json;

namespace EvalRunner.Anthropic;

public class AnthropicApiException : Exception
{
    public int StatusCode { get; }
    public string? ApiErrorType { get; }
    public string? ApiMessage { get; }
    public string? TestCaseId { get; set; }
    public int CompletedCases { get; set; }
    public int TotalCases { get; set; }

    public bool StopsRun => StatusCode is 401 or 402 or 403 or 429;

    public AnthropicApiException(int statusCode, string? apiErrorType, string? apiMessage)
        : base(apiMessage ?? $"Anthropic API error ({statusCode})")
    {
        StatusCode = statusCode;
        ApiErrorType = apiErrorType;
        ApiMessage = apiMessage;
    }

    public static AnthropicApiException FromResponse(int statusCode, string body)
    {
        string? errorType = null;
        string? message = null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("type", out var typeElement))
                {
                    errorType = typeElement.GetString();
                }

                if (error.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString();
                }
            }
        }
        catch (JsonException)
        {
            message = body.Length > 200 ? body[..200] + "..." : body;
        }

        return new AnthropicApiException(statusCode, errorType, message);
    }

    public string UserFacingMessage => StatusCode switch
    {
        401 => "Invalid or missing API key. Check ANTHROPIC_API_KEY in your .env file.",
        402 => "Billing error — your Anthropic credit balance may be exhausted. Add credits at https://console.anthropic.com/settings/billing",
        403 => "API key does not have permission to use this model.",
        429 => "Rate limit exceeded. Wait a few minutes and try again.",
        _ => ApiMessage ?? $"Anthropic API returned HTTP {StatusCode}."
    };

    public void WriteFatalBanner(TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine("*** ANTHROPIC API ERROR — RUN STOPPED ***");
        writer.WriteLine();
        writer.WriteLine(UserFacingMessage);

        if (!string.IsNullOrWhiteSpace(TestCaseId))
        {
            writer.WriteLine($"Failed on test case: {TestCaseId}");
        }

        if (TotalCases > 0)
        {
            writer.WriteLine($"Completed {CompletedCases} of {TotalCases} cases before stopping.");
        }

        if (!string.IsNullOrWhiteSpace(ApiErrorType))
        {
            writer.WriteLine($"API error type: {ApiErrorType}");
        }

        writer.WriteLine();
        writer.WriteLine("No further API calls will be made. Remaining test cases were skipped.");
        writer.WriteLine();
    }
}
