namespace FallenFaction.Server.Data.Models
{
public class TitleChangeLog
{
    public int Id { get; set; }
    public int TitleId { get; set; }
    public string UpdatedByUserId { get; set; }
    public string? ReviewedByUserId { get; set; }  // Changed to nullable
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string ChangeType { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public string AdminComment { get; set; } = string.Empty; // Default value to prevent NULL errors
    public ChangeLogStatus Status { get; set; }
    public string RejectionReason { get; set; } = string.Empty; // Default value to prevent NULL errors
    // Navigation properties
    public virtual Title Title { get; set; }
    public virtual AppUser UpdatedByUser { get; set; }
    public virtual AppUser ReviewedByUser { get; set; }
}

public enum ChangeLogStatus
{
    Pending,
    AutoApproved,
    Approved,
    Rejected
}
}
