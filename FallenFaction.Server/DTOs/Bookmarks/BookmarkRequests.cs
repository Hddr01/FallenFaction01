// DTOs/Bookmarks/BookmarkRequests.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Bookmarks
{
    public class AddBookmarkRequest
    {
        [Required]
        public int TitleId { get; set; }

        [Required]
        public int FolderId { get; set; }
    }

    public class RemoveBookmarkRequest
    {
        [Required]
        public int BookmarkId { get; set; }
    }

    public class CreateFolderRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateFolderRequest
    {
        [Required]
        public int FolderId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class DeleteFolderRequest
    {
        [Required]
        public int FolderId { get; set; }
    }

    public class UpdateLastReadRequest
    {
        [Required]
        public int TitleId { get; set; }

        [Required]
        public int ChapterNumber { get; set; }
    }

    // NEW: Add this class for updating bookmark status
    public class UpdateBookmarkStatusRequest
    {
        [Required]
        public int TitleId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // "reading", "completed", "on-hold", "plan-to-read", "dropped"
    }
}