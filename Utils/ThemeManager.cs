using System;
using System.Windows;

namespace TrackStop.Utils
{
    public static class ThemeManager
    {
        public enum ThemeType
        {
            Light,
            Dark
        }

        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        public static void ApplyTheme(ThemeType theme)
        {
            CurrentTheme = theme;

            var app = Application.Current;

            // Просто меняем тему через изменение свойства BundledTheme
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict is MaterialDesignThemes.Wpf.BundledTheme bundledTheme)
                {
                    // В старой версии может быть свойство Theme вместо BaseTheme
                    try
                    {
                        // Попробуем через отражение или доступ к свойству
                        var themeProperty = bundledTheme.GetType().GetProperty("BaseTheme");
                        if (themeProperty != null)
                        {
                            var themeValue = Enum.Parse(themeProperty.PropertyType,
                                theme == ThemeType.Light ? "Light" : "Dark");
                            themeProperty.SetValue(bundledTheme, themeValue);
                        }
                    }
                    catch
                    {
                        // Альтернативный способ
                        try
                        {
                            var themeProperty = bundledTheme.GetType().GetProperty("Theme");
                            if (themeProperty != null)
                            {
                                var themeValue = Enum.Parse(themeProperty.PropertyType,
                                    theme == ThemeType.Light ? "Light" : "Dark");
                                themeProperty.SetValue(bundledTheme, themeValue);
                            }
                        }
                        catch { }
                    }
                    break;
                }
            }

            // Сохраняем настройку
            AppSettings.CurrentSettings.Theme = theme.ToString();
            AppSettings.SaveSettings();
        }

        public static void LoadSavedTheme()
        {
            AppSettings.LoadSettings();

            if (AppSettings.CurrentSettings.Theme == "Dark")
            {
                ApplyTheme(ThemeType.Dark);
            }
            else
            {
                ApplyTheme(ThemeType.Light);
            }
        }
    }
}