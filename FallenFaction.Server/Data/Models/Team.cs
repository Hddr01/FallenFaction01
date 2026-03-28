// Data/Models/Team.cs - Updated with Avatar and Background images
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FallenFaction.Server.Data.Models
{
    public enum GroupType
    {
        Personal = 1,     // Auto-created on registration — represents the user's personal studio
        Translation = 2,  // Scanlation / translation group
        Creator = 3       // Original content studio / doujin circle
    }

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

        // Image paths
        [StringLength(255)]
        public string? AvatarImagePath { get; set; }

        [StringLength(255)]
        public string? BackgroundImagePath { get; set; }

        // Add creation date for better tracking
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // ── Group classification ─────────────────────────────────────────────────
        public GroupType GroupType { get; set; } = GroupType.Translation;

        // Quick filter flag — Personal groups are hidden from the public "Groups" listing
        public bool IsPersonal { get; set; } = false;

        // Navigation properties
        public ICollection<Title> Titles { get; set; } = new List<Title>();
        public ICollection<PendingTitle> PendingTitles { get; set; } = new List<PendingTitle>();
        public ICollection<UserTeamRole> UserTeamRoles { get; set; } = new List<UserTeamRole>();

        // This should be configured as a many-to-many through UserTeamRoles
        public ICollection<AppUser> Members { get; set; } = new List<AppUser>();

        // Chapter collections
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public ICollection<PendingChapter> PendingChapters { get; set; } = new List<PendingChapter>();
        public ICollection<RejectedChapter> RejectedChapters { get; set; } = new List<RejectedChapter>();
    }
}