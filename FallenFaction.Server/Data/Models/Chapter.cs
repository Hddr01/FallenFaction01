using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class Chapter
    {
        public Chapter()
        {
            Views = new HashSet<ChapterView>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public int VolumeNumber { get; set; }

        public int ChapterNumber { get; set; }

        [Required]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; }

        public int? TeamId { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ReleaseDate { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        [Required]
        public string UpdatedByUserId { get; set; }

        [ForeignKey("UpdatedByUserId")]
        public AppUser UpdatedByUser { get; set; }

        /// <summary>
        /// The full text content of this chapter.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// True = this chapter's AI translation requires tickets to read.
        /// False = free for everyone (either non-AI title, or already unlocked).
        /// Only applies to titles with TitleCategory.AITranslation.
        /// </summary>
        public bool IsAILocked { get; set; } = false;

        /// <summary>
        /// Character count of the raw content. Used to compute unlock cost:
        /// Cost = (CharacterCount + 500) × 0.0012, minimum 1 ticket.
        /// </summary>
        public int CharacterCount { get; set; } = 0;

        public ICollection<ChapterView> Views { get; set; }
    }
}
