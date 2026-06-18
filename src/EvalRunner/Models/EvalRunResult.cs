namespace EvalRunner.Models;

public class EvalRunResult
{
    public TestCase TestCase { get; set; } = null!;
    public string RetrievedContext { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public EvalScore? Score { get; set; }
    public List<string> GuardrailViolations { get; set; } = [];
    public long ElapsedMs { get; set; }
    public bool Passed { get; set; }
    public bool DetectionPassed { get; set; }
    public string? Error { get; set; }
}
