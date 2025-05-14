namespace FallenFaction.Server.Data.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICollection<Title> Titles { get; set; } = new List<Title>();
        public ICollection<PendingTitle> PendingTitles { get; set; } = new List<PendingTitle>();
    }
}
