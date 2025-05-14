using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

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

        public List<ChapterImage> ImagePaths { get; set; } = new List<ChapterImage>();

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
