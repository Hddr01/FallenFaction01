using FallenFaction.Server.Data.Models;

namespace FallenFaction.Server.DTOs.Chapter
{
    public class ChapterDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VolumeNumber { get; set; }
        public int ChapterNumber { get; set; }
        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public NameIdDTO? Team { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ReleaseDate { get; set; }
        public List<ChapterImageDTO> ImagePaths { get; set; } = new List<ChapterImageDTO>();

        // Navigation properties for chapter browsing
        public int? NextChapterId { get; set; }
        public string? NextChapterName { get; set; }
        public int? NextChapterVolume { get; set; }
        public int? NextChapterTeamId { get; set; }
        public int? PreviousChapterId { get; set; }
        public string? PreviousChapterName { get; set; }
        public int? PreviousChapterVolume { get; set; }
        public int? PreviousChapterTeamId { get; set; }
        public int? PreviousChapterPageCount { get; set; }
    }

    public class ChapterImageDTO
    {
        public int Id { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int? ChapterId { get; set; }
    }

    public class NameIdDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarImagePath { get; set; }
        public string? BackgroundImagePath { get; set; }
    }

    public static class ChapterMapper
    {
        public static ChapterDTO ToDTO(Data.Models.Chapter chapter)
        {
            if (chapter == null) return null;

            return new ChapterDTO
            {
                Id = chapter.Id,
                Name = chapter.Name ?? string.Empty,
                VolumeNumber = chapter.VolumeNumber,
                ChapterNumber = chapter.ChapterNumber,
                TitleId = chapter.TitleId,
                TitleName = chapter.Title?.OriginalTitle ?? chapter.Title?.EnglishTitle ?? string.Empty,
                TeamId = chapter.TeamId,
                Team = chapter.Team != null ? new NameIdDTO
                {
                    Id = chapter.Team.Id,
                    Name = chapter.Team.Name ?? string.Empty,
                    AvatarImagePath = chapter.Team.AvatarImagePath,
                    BackgroundImagePath = chapter.Team.BackgroundImagePath
                } : null,
                CreatedDate = chapter.CreatedDate,
                ReleaseDate = chapter.ReleaseDate,
                ImagePaths = chapter.ImagePaths?.Select(ip => new ChapterImageDTO
                {
                    Id = ip.Id,
                    ImagePath = ip.ImagePath ?? string.Empty,
                    OrderIndex = ip.OrderIndex,
                    ChapterId = ip.ChapterId ?? 0
                }).OrderBy(ip => ip.OrderIndex).ToList() ?? new List<ChapterImageDTO>()
            };
        }
    }
}