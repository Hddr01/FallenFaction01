using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace FallenFaction.Server.Data.Models
{
    public class Bookmark
    {
        public int Id { get; set; }
        public int TitleId { get; set; }
        public int FolderId { get; set; }
        public string UserId { get; set; }
        public DateTime AddedDate { get; set; }
        public int LastReadChapter { get; set; }

        // Navigation properties
        [ForeignKey("TitleId")]
        public Title Title { get; set; }
        public BookmarkFolder Folder { get; set; }
        public AppUser User { get; set; }
        public DateTime LastReadDate { get; internal set; }
    }
}
