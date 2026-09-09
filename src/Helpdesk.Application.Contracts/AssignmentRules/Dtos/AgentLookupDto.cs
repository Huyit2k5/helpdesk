using System;

namespace Helpdesk.AssignmentRules.Dtos;

public class AgentLookupDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? $"{Name} ({UserName})" : UserName;
}
