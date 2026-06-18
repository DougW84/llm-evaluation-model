namespace EvalRunner.Models;

public class TestCase
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string? StudentId { get; set; }
    public string? RetrievedContext { get; set; }
    public string? AiResponse { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool ExpectsResponsePass { get; set; }
    public string? ExpectedBehavior { get; set; }
    public string? ExpectedOutcome { get; set; }
}
