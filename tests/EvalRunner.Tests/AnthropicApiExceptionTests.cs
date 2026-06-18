using EvalRunner.Anthropic;

namespace EvalRunner.Tests;

public class AnthropicApiExceptionTests
{
    [Fact]
    public void FromResponse_ParsesBillingError()
    {
        var body = """
            {
              "type": "error",
              "error": {
                "type": "billing_error",
                "message": "Your credit balance is too low to access the API."
              }
            }
            """;

        var ex = AnthropicApiException.FromResponse(402, body);

        Assert.Equal(402, ex.StatusCode);
        Assert.Equal("billing_error", ex.ApiErrorType);
        Assert.True(ex.StopsRun);
        Assert.Contains("credit balance", ex.UserFacingMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromResponse_AuthenticationError_StopsRun()
    {
        var body = """{"type":"error","error":{"type":"authentication_error","message":"invalid x-api-key"}}""";

        var ex = AnthropicApiException.FromResponse(401, body);

        Assert.True(ex.StopsRun);
        Assert.Contains("API key", ex.UserFacingMessage);
    }

    [Fact]
    public void FromResponse_ModelNotFound_StopsRun()
    {
        var body = """{"type":"error","error":{"type":"not_found_error","message":"model: claude-sonnet-4-20250514"}}""";

        var ex = AnthropicApiException.FromResponse(404, body);

        Assert.True(ex.StopsRun);
        Assert.Contains("claude-sonnet-4-6", ex.UserFacingMessage);
    }

    [Fact]
    public void WriteFatalBanner_IncludesTestCaseContext()
    {
        var ex = AnthropicApiException.FromResponse(402, """{"error":{"type":"billing_error","message":"low balance"}}""");
        ex.TestCaseId = "005";
        ex.CompletedCases = 4;
        ex.TotalCases = 16;

        using var writer = new StringWriter();
        ex.WriteFatalBanner(writer);
        var output = writer.ToString();

        Assert.Contains("RUN STOPPED", output);
        Assert.Contains("test case: 005", output);
        Assert.Contains("4 of 16", output);
        Assert.Contains("skipped", output);
    }
}
