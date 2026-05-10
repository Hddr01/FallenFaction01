namespace FallenFaction.Server.Services
{
    // Single source of truth for the ChangeType strings written into TitleChangeLog
    // and read by TitleChangeApplicator. Existing writers still use string literals;
    // they can migrate to these constants opportunistically.
    public static class TitleChangeTypes
    {
        public const string OriginalTitle = "Original Title";
        public const string EnglishTitle = "English Title";
        public const string Description = "Description";
        public const string AlternativeNames = "Alternative Names";
        public const string ReleaseDate = "Release Date";
        public const string Status = "Status";
        public const string TranslationStatus = "Translation Status";
        public const string Type = "Type";
        public const string AgeRestriction = "Age Restriction";
        public const string CoverImage = "Cover Image";
        public const string BackgroundImage = "Background Image";
        public const string Authors = "Authors";
        public const string Artists = "Artists";
        public const string Publishers = "Publishers";
        public const string Teams = "Teams";
        public const string Categories = "Categories";
        public const string Tags = "Tags";
        public const string Formats = "Formats";
        public const string ExternalLinks = "External Links";
    }
}
