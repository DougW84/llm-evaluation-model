namespace EvalRunner.Anthropic;

public static class AnthropicSettings
{
    public const string DefaultModel = "claude-sonnet-4-6";

    public static string GetModel() => EnvLoader.GetEnvValue("ANTHROPIC_MODEL") ?? DefaultModel;
}
