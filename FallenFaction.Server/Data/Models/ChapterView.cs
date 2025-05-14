using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class ChapterView
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public DateTime ViewDate { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }
    }
}
