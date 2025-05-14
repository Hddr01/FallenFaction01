using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FallenFaction.Server.Data.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public string CreatorId { get; set; }

        public ICollection<Title> Titles { get; set; } = new List<Title>();
        public ICollection<PendingTitle> PendingTitles { get; set; } = new List<PendingTitle>();
        public ICollection<UserTeamRole> UserTeamRoles { get; set; } = new List<UserTeamRole>();
        public ICollection<AppUser> Members { get; set; } = new List<AppUser>();

        // New Collections for Chapters
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public ICollection<PendingChapter> PendingChapters { get; set; } = new List<PendingChapter>();
        public ICollection<RejectedChapter> RejectedChapters { get; set; } = new List<RejectedChapter>();
    }
}
