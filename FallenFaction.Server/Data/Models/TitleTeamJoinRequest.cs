using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    public enum JoinRequestStatus
    {
        Pending = 0,
        Approved = 1,
        RejectedByAdmin = 2,
        RejectedByTeam = 3,
        AutoRejected = 4   // Active translator found — system rejected immediately
    }

    public class TitleTeamJoinRequest
    {
        [Key]
        public int Id { get; set; }

        // Which title the team wants to translate
        public int TitleId { get; set; }
        [ForeignKey("TitleId")]
        public Title? Title { get; set; }

        // Which team is requesting
        public int RequestingTeamId { get; set; }
        [ForeignKey("RequestingTeamId")]
        public Team? RequestingTeam { get; set; }

        // Who submitted the request (a member of the requesting team)
        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;
        [ForeignKey("RequestedByUserId")]
        public AppUser? RequestedByUser { get; set; }

        // Reason provided by the requester
        [StringLength(1000)]
        public string? Message { get; set; }

        public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;

        // Filled in on auto-reject: explains why
        [StringLength(500)]
        public string? AutoRejectedReason { get; set; }

        // Filled in on manual reject
        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Who reviewed it (admin or existing team admin)
        public string? ReviewedByUserId { get; set; }
        [ForeignKey("ReviewedByUserId")]
        public AppUser? ReviewedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
