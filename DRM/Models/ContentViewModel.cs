using System;
using System.Collections.Generic;
using DRM.Models;

namespace DRM.ViewModels 
{
    public class ContentViewModel
    {
        public List<AudioFile> AudioFiles { get; set; } = new();
        public List<VideoFile> VideoFiles { get; set; } = new();
        public List<PdfFile> PdfFiles { get; set; } = new();
        public object LatestFile { get; set; }
    }
}
