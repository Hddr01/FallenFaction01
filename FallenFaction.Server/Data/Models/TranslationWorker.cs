using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.Data.Models
{
    /// <summary>
    /// A machine allowed to claim translation jobs. Authenticates with an API key
    /// (header X-Worker-Key); only the SHA-256 hash of the key is stored.
    /// </summary>
    public class TranslationWorker
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(64)]
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>First characters of the key, so admins can tell keys apart.</summary>
        [StringLength(12)]
        public string KeyPrefix { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
    }
}
