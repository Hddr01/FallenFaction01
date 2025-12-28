using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Tracks user reading progress for titles independently of bookmark status.
    /// This allows users to maintain their reading position even if they unbookmark a title.
    /// </summary>
    public class ReadingProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TitleId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The last chapter the user read (chapter number)
        /// </summary>
        public int LastReadChapter { get; set; }

        /// <summary>
        /// When the user last read this title
        /// </summary>
        public DateTime LastReadDate { get; set; }

        // Navigation properties
        [ForeignKey("TitleId")]
        public virtual Title? Title { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }
}