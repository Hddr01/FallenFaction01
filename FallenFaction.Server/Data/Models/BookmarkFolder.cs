namespace FallenFaction.Server.Data.Models
{
    public class BookmarkFolder
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public bool IsDefault { get; set; } // For system default folders
        public int DisplayOrder { get; set; } // For ordering folders
        public DateTime CreatedAt { get; set; } // Added missing CreatedAt property

        // Navigation properties
        public AppUser User { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    }
}