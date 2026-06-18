namespace EvalRunner.Models;

public class ThresholdConfig
{
    public double MinAvgGrounding { get; set; } = 4.0;
    public double MinAvgTone { get; set; } = 4.0;
    public double MinAccuracyPassRate { get; set; } = 0.80;
    public double MinEscalationPassRate { get; set; } = 1.0;
    public long MaxP95LatencyMs { get; set; } = 3000;
    public int MinPassingScore { get; set; } = 4;
}
