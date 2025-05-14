using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class Chapter
    {
        public Chapter()
        {
            ImagePaths = new HashSet<ChapterImage>();
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

        public ICollection<ChapterImage> ImagePaths { get; set; }

        public ICollection<ChapterView> Views { get; set; }
    }
}
