// Services/Interfaces/ITrustService.cs
using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.Services.Interfaces
{
    public interface ITrustService
    {
        /// <summary>
        /// Returns true if the user is trusted to auto-submit the given action.
        /// </summary>
        Task<bool> IsTrustedAsync(string userId, TrustActionType action);

        /// <summary>
        /// Called when an admin approves a submission.
        /// Increments the approval count and promotes to trusted if threshold reached.
        /// </summary>
        Task RecordApprovalAsync(string userId, TrustActionType action);

        /// <summary>
        /// Called when an admin rejects a submission.
        /// Resets the approval counter and revokes trusted status.
        /// </summary>
        Task RecordRejectionAsync(string userId, TrustActionType action);

        /// <summary>
        /// Returns the full trust record for display in admin panels.
        /// </summary>
        Task<UserTrustRecord?> GetRecordAsync(string userId, TrustActionType action);

        /// <summary>
        /// How many admin approvals are required before a user is trusted.
        /// </summary>
        int TrustThreshold { get; }
    }
}