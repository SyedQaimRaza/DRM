namespace DRM.Models
{
    public class FileModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "audio", "video", "pdf"
        public string Date { get; set; }
        public bool Locked { get; set; }
    }
}
