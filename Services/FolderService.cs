using System;
using System.IO;

namespace TrackStop.Services
{
    public static class FolderService
    {
        public static void EnsureRequiredFolders()
        {
            string[] folders = { "Songs", "Artists", "Covers" };
            foreach (var folder in folders)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
        }
    }
}
