namespace FallenFaction.Server.Data.Models
{
    public class PendingTitleChange : ITitleChange
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public virtual Title Title { get; set; }
        public string AdminComment { get; set; } = string.Empty;
        public string UpdatedByUserId { get; set; }
        public virtual AppUser UpdatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ChangeType { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool RequiresReview { get; set; }
    }
}
