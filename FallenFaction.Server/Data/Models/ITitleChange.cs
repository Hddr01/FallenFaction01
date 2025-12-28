namespace FallenFaction.Server.Data.Models
{
    public interface ITitleChange
    {
        int Id { get; set; }
        int TitleId { get; set; }
        Title Title { get; set; }
        string AdminComment { get; set; }
        string UpdatedByUserId { get; set; }
        AppUser UpdatedByUser { get; set; }
        DateTime CreatedAt { get; set; }
        string ChangeType { get; set; }
        string OldValue { get; set; }
        string NewValue { get; set; }
    }
}