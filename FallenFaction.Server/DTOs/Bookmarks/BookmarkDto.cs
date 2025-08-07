// DTOs/Bookmarks/BookmarkDto.cs
namespace FallenFaction.Server.DTOs.Bookmarks
{
    public class BookmarkDto
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public int FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string TitleName { get; set; } = string.Empty;
        public string CoverImage { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public int LastReadChapter { get; set; }
    }

    public class BookmarkFolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; }
        public int Count { get; set; }
    }

    public class BookmarkFoldersResponseDto
    {
        public List<BookmarkFolderDto> Folders { get; set; } = new List<BookmarkFolderDto>();
        public BookmarkDto? CurrentBookmark { get; set; }
    }

    public class BookmarkStatsDto
    {
        public int TitleId { get; set; }
        public int TotalBookmarks { get; set; }
        public List<BookmarkFolderDistributionDto> FolderDistribution { get; set; } = new List<BookmarkFolderDistributionDto>();
    }

    public class BookmarkFolderDistributionDto
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}