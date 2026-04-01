using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public class PendingChapter : ChapterBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int VolumeNumber { get; set; }

        [Required]
        public int ChapterNumber { get; set; }

        /// <summary>
        /// The full text content of this chapter awaiting moderation.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? TitleId { get; set; }
        [ForeignKey("TitleId")]
        public Title Title { get; set; }

        public int? PendingTitleId { get; set; }
        [ForeignKey("PendingTitleId")]
        public PendingTitle PendingTitle { get; set; }

        [Required]
        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        /// <summary>
        /// When set, this pending entry is an edit of an already-published chapter.
        /// Null means this is a brand-new chapter submission.
        /// </summary>
        public int? OriginalChapterId { get; set; }
        [ForeignKey("OriginalChapterId")]
        public Chapter OriginalChapter { get; set; }
    }
}
