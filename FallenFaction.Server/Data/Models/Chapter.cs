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

        public ICollection<ChapterView> Views { get; set; }
    }
}
