using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class ChapterBase
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string UpdatedByUserId { get; set; }
        [ForeignKey("UpdatedByUserId")]
        public AppUser UpdatedByUser { get; set; }
    }
}