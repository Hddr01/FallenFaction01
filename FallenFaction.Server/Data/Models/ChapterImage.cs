using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class ChapterImage
    {
        public int Id { get; set; }

        [Required]
        public string ImagePath { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        // Make navigation properties virtual for lazy loading
        public int? ChapterId { get; set; }
        [ForeignKey("ChapterId")]
        public virtual Chapter? Chapter { get; set; }

        public int? PendingChapterId { get; set; }
        [ForeignKey("PendingChapterId")]
        public virtual PendingChapter? PendingChapter { get; set; }

        public int? RejectedChapterId { get; set; }
        [ForeignKey("RejectedChapterId")]
        public virtual RejectedChapter? RejectedChapter { get; set; }

        // Add a check constraint to ensure only one ID is set
        [NotMapped]
        public bool IsValid => (ChapterId.HasValue ? 1 : 0) +
                              (PendingChapterId.HasValue ? 1 : 0) +
                              (RejectedChapterId.HasValue ? 1 : 0) <= 1;
    }
}
