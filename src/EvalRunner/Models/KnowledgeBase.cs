namespace EvalRunner.Models;

public class KnowledgeBase
{
    public List<StudentRecord> Students { get; set; } = [];
    public List<PolicyRecord> Policies { get; set; } = [];
}

public class StudentRecord
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomAssignment { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string RentDueDate { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public string TransferEligibility { get; set; } = string.Empty;
    public List<string> OpenMaintenanceTickets { get; set; } = [];
}

public class PolicyRecord
{
    public string Id { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public string Content { get; set; } = string.Empty;
}
