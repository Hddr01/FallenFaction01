// DTOs/Ratings/RatingRequests.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Ratings
{
    public class AddRatingRequest
    {
        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int Value { get; set; }

        [Required]
        public int TitleId { get; set; }
    }

    public class UpdateRatingRequest
    {
        [Required]
        public int RatingId { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10")]
        public int Value { get; set; }
    }

    public class DeleteRatingRequest
    {
        [Required]
        public int RatingId { get; set; }
    }

    public class GetUserRatingRequest
    {
        [Required]
        public int TitleId { get; set; }
    }

    public class GetRatingStatsRequest
    {
        [Required]
        public int TitleId { get; set; }
    }

    public class GetRatingsRequest
    {
        [Required]
        public int TitleId { get; set; }

        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "newest"; // newest, oldest, highest, lowest
    }
}