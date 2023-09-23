using System;
using System.IO;

namespace win_tasks {
    internal static class Globals {
        public const string APP_NAME = "win-tasks";
        private static readonly string AppPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_NAME);

        public static string GetAppPath() {
            Directory.CreateDirectory(AppPath);
            return AppPath;
        }
    }
}
