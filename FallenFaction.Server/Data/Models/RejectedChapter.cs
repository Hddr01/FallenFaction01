using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public class RejectedChapter : ChapterBase
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
        /// The full text content of this chapter that was rejected.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public int TitleId { get; set; }
        [ForeignKey("TitleId")]
        public Title Title { get; set; }

        [Required]
        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public Team Team { get; set; }
    }
}
