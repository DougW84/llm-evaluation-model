using System.Text.Json;
using EvalRunner.Models;

namespace EvalRunner.Rag;

public class MockRagRetriever
{
    private readonly KnowledgeBase _knowledgeBase;

    public MockRagRetriever(KnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    public static MockRagRetriever LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var kb = JsonSerializer.Deserialize<KnowledgeBase>(json, JsonOptions()) 
            ?? throw new InvalidOperationException($"Failed to load knowledge base from {path}");
        return new MockRagRetriever(kb);
    }

    public string Retrieve(string query, string? studentId)
    {
        var sections = new List<string>();
        var queryLower = query.ToLowerInvariant();

        if (!string.IsNullOrEmpty(studentId))
        {
            var student = _knowledgeBase.Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student is not null)
            {
                sections.Add(FormatStudentRecord(student));
            }
        }

        var matchedPolicies = _knowledgeBase.Policies
            .Where(p => p.Keywords.Any(k => queryLower.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matchedPolicies.Count == 0 && sections.Count == 0)
        {
            matchedPolicies = _knowledgeBase.Policies.Take(1).ToList();
        }

        foreach (var policy in matchedPolicies)
        {
            sections.Add($"[Policy: {policy.Id}] {policy.Content}");
        }

        return string.Join("\n\n", sections);
    }

    private static string FormatStudentRecord(StudentRecord student)
    {
        var maintenance = student.OpenMaintenanceTickets.Count > 0
            ? string.Join("; ", student.OpenMaintenanceTickets)
            : "None";

        return $"""
            [Student: {student.StudentId}]
            Name: {student.Name}
            Room: {student.RoomAssignment}
            Contract: {student.ContractType}
            Rent due: {student.RentDueDate}
            Current balance: ${student.CurrentBalance:F2}
            Transfer eligibility: {student.TransferEligibility}
            Open maintenance: {maintenance}
            """;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };
}
