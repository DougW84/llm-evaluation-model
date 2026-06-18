namespace EvalRunner.Anthropic;

public static class AnthropicHttp
{
    public static void EnsureSuccess(int statusCode, string body)
    {
        if (statusCode is >= 200 and < 300)
        {
            return;
        }

        throw AnthropicApiException.FromResponse(statusCode, body);
    }
}
