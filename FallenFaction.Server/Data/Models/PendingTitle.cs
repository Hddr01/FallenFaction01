using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FallenFaction.Server.Data.Models
{
    public class PendingTitle
    {
        public int Id { get; set; }
        public string CoverImagePath { get; set; } = string.Empty;
        public string BackgroundImagePath { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public string EnglishTitle { get; set; } = string.Empty;
        public string AlternativeNames { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StatusTitle { get; set; } = string.Empty;
        public string StatusTranslation { get; set; } = string.Empty;
        public MangaType Type { get; set; }
        public int AgeRestriction { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Format> Formats { get; set; } = new List<Format>();
        public ICollection<Author> Authors { get; set; } = new List<Author>();
        public ICollection<Artist> Artists { get; set; } = new List<Artist>();
        public ICollection<Publisher> Publishers { get; set; } = new List<Publisher>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();

        [NotMapped]
        public List<string> ExternalLinks { get; set; } = new List<string>();

        public string ExternalLinksSerialized
        {
            get => string.Join(";", ExternalLinks);
            set => ExternalLinks = value?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        }
    }
}
