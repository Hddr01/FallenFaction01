using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BannerImagePath { get; set; }
        public bool IsBannedFromComments { get; set; } = false;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; } = false;

        public AppUser()
        {
            Teams = new HashSet<Team>();
            UserTeamRoles = new HashSet<UserTeamRole>();
            Bookmarks = new HashSet<Bookmark>();
            Ratings = new HashSet<Rating>();
            ChapterViews = new HashSet<ChapterView>();
        }

        public void AddLastActive()
        {
            LastActive = DateTime.UtcNow;
            IsOnline = true;
        }

        public DateTime? DateOfBirth { get; set; }

        public string ProfilePicturePath { get; set; } = "/img/default-avatar.png";

        public string? Bio { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public string? SocialMediaLinks { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }

        // Navigation properties
        public ICollection<Team> Teams { get; set; }
        public ICollection<UserTeamRole> UserTeamRoles { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public ICollection<Rating> Ratings { get; set; } = new HashSet<Rating>();
        public ICollection<ChapterView> ChapterViews { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<CommentReaction> CommentReactions { get; set; }

        // ── XP & Level system ────────────────────────────────────────────────
        /// <summary>
        /// Cumulative XP earned across all activities (reading, commenting, rating, etc.).
        /// Level thresholds: L1=0, L2=100, L3=300, L4=700, L5=1500.
        /// Level 2+ or Patreon supporter can vote on translation requests.
        /// </summary>
        public int XpPoints { get; set; } = 0;

        /// <summary>
        /// Cached level computed from XpPoints. Updated whenever XP is awarded.
        /// 1=Newcomer, 2=Reader, 3=Regular, 4=Veteran, 5=Champion.
        /// </summary>
        public int UserLevel { get; set; } = 1;

        /// <summary>Computes and returns the correct level for a given XP value.</summary>
        public static int ComputeLevel(int xp) => xp switch
        {
            < 100  => 1,
            < 300  => 2,
            < 700  => 3,
            < 1500 => 4,
            _      => 5
        };

        /// <summary>Returns true if this user can vote on translation requests.</summary>
        public bool CanVote => UserLevel >= 2 || PatreonUserId != null;

        // ── Ticket system ────────────────────────────────────────────────────
        public UserTicket? Wallet { get; set; }
        public ICollection<TicketTransaction> TicketTransactions { get; set; } = new HashSet<TicketTransaction>();
        public ICollection<TranslationRequest> TranslationRequests { get; set; } = new HashSet<TranslationRequest>();
        public ICollection<AIChapterUnlock> AIChapterUnlocks { get; set; } = new HashSet<AIChapterUnlock>();
        public ICollection<TranslationRequestVote> TranslationRequestVotes { get; set; } = new HashSet<TranslationRequestVote>();

        // ── Patreon integration ──────────────────────────────────────────────
        public string? PatreonUserId { get; set; }
        public string? PatreonAccessToken { get; set; }
        public string? PatreonRefreshToken { get; set; }
        public DateTime? PatreonLinkedAt { get; set; }
        public string? PatreonTierName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PatreonMonthlyAmount { get; set; } = 0;
    }
}
