using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FallenFaction.Server.Data.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Value { get; set; }

        [Required]
        public int TitleId { get; set; }

        [ForeignKey("TitleId")]
        public Title Title { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
