using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// Each user has exactly one wallet row tracking their Gold and Silver ticket balances.
    /// Gold tickets come from Patreon/purchases, never expire.
    /// Silver tickets come from contributions, expire after 3 months.
    /// </summary>
    public class UserTicket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public AppUser User { get; set; } = null!;

        /// <summary>Gold tickets — purchased or rewarded. Never expire.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal GoldBalance { get; set; } = 0;

        /// <summary>Silver tickets — earned from contributions. Expire after 3 months.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SilverBalance { get; set; } = 0;

        /// <summary>Total spendable = Gold + Silver.</summary>
        [NotMapped]
        public decimal TotalBalance => GoldBalance + SilverBalance;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
