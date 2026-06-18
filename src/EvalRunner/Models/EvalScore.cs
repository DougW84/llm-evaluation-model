namespace EvalRunner.Models;

public class EvalScore
{
    public int Grounding { get; set; }
    public int Accuracy { get; set; }
    public int Tone { get; set; }
    public int EscalationHandling { get; set; }
    public string GroundingReason { get; set; } = string.Empty;
}
