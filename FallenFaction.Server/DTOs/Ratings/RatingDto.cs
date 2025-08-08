// DTOs/Ratings/RatingDto.cs
using System.ComponentModel.DataAnnotations;

namespace FallenFaction.Server.DTOs.Ratings
{
    public class RatingDto
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public int TitleId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RatingStatsDto
    {
        public int TitleId { get; set; }
        public double Average { get; set; }
        public int Total { get; set; }
        public List<RatingDistributionDto> Distribution { get; set; } = new List<RatingDistributionDto>();
    }

    public class RatingDistributionDto
    {
        public int Value { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class UserRatingDto
    {
        public int? RatingId { get; set; }
        public int? Value { get; set; }
        public bool HasRated { get; set; }
        public DateTime? RatedAt { get; set; }
    }

    public class RatingsSummaryDto
    {
        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public UserRatingDto? UserRating { get; set; }
        public List<RatingDistributionDto> Distribution { get; set; } = new List<RatingDistributionDto>();
    }
}