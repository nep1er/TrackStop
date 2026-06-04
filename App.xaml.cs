using System.Windows;
using TrackStop.Utils;

namespace TrackStop
{
    public partial class App : Application
    {
        public static int CurrentUserId { get; set; } = -1;
        public static string CurrentUserLogin { get; set; } = "";
        public static string CurrentUserRole { get; set; } = "user";
        public static bool IsAdmin => CurrentUserRole == "admin";
        public static bool IsArtist => CurrentUserRole == "artist";
        public static bool IsRegularUser => CurrentUserRole == "user";
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Загружаем и применяем сохраненную тему
            ThemeManager.LoadSavedTheme();
        }
    }
}