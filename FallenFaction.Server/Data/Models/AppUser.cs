using Microsoft.AspNetCore.Identity;

namespace FallenFaction.Server.Data.Models
{
    public class AppUser : IdentityUser
    {
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

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ProfilePicturePath { get; set; }
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
    }
}