namespace FallenFaction.Server.Data.Models
{
    public class ApprovedTitleChange : ITitleChange
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public virtual Title Title { get; set; }
        public string AdminComment { get; set; }
        public string UpdatedByUserId { get; set; }
        public virtual AppUser UpdatedByUser { get; set; }
        public string ReviewedByUserId { get; set; }
        public virtual AppUser ReviewedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ApprovedAt { get; set; }
        public string ChangeType { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsAutoApproved { get; set; }
    }
}
