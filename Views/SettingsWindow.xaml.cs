using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrackStop.Utils;

namespace TrackStop.ViewModels
{
    public partial class SettingsWindow : Window
    {
        private Color _currentAccentColor;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            AppSettings.LoadSettings();

            // Загружаем текущую тему
            if (ThemeManager.CurrentTheme == ThemeManager.ThemeType.Dark)
            {
                DarkThemeRadio.IsChecked = true;
            }
            else
            {
                LightThemeRadio.IsChecked = true;
            }

            // Загружаем текущего пользователя
            CurrentUserText.Text = App.CurrentUserLogin;

            // Загружаем сохраненный акцентный цвет
            try
            {
                var savedColor = AppSettings.CurrentSettings.AccentColor;
                if (!string.IsNullOrEmpty(savedColor))
                {
                    var color = (Color)ColorConverter.ConvertFromString(savedColor);
                    _currentAccentColor = color;

                    // Устанавливаем выбранный цвет в комбобокс
                    foreach (ComboBoxItem item in AccentColorComboBox.Items)
                    {
                        if (item.Tag?.ToString() == savedColor)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
                else
                {
                    _currentAccentColor = (Color)ColorConverter.ConvertFromString("#FFAB0000");
                }
            }
            catch
            {
                _currentAccentColor = (Color)ColorConverter.ConvertFromString("#FFAB0000");
            }
        }

        private void LightThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Применяем тему сразу для предпросмотра
            ThemeManager.ApplyTheme(ThemeManager.ThemeType.Light);
        }

        private void DarkThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Применяем тему сразу для предпросмотра
            ThemeManager.ApplyTheme(ThemeManager.ThemeType.Dark);
        }

        private void AccentColorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AccentColorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(selectedItem.Tag.ToString());
                    _currentAccentColor = color;

                    // Применяем цвет акцента сразу для предпросмотра
                    ApplyAccentColor(color);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка применения цвета: {ex.Message}");
                }
            }
        }

        private void ApplyAccentColor(Color color)
        {
            // Создаем новый ResourceDictionary с акцентным цветом
            var accentDictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Lime.xaml", UriKind.Absolute)
            };

            // Обновляем цвет акцента
            accentDictionary["PrimaryHueMidBrush"] = new SolidColorBrush(color);
            accentDictionary["PrimaryHueMidForegroundBrush"] = Brushes.White;
            accentDictionary["SecondaryAccentBrush"] = new SolidColorBrush(color);
            accentDictionary["SecondaryAccentForegroundBrush"] = Brushes.White;

            // Заменяем старый словарь акцентов
            var app = Application.Current;
            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null && dict.Source.OriginalString.Contains("MaterialDesignColor.Lime"))
                {
                    app.Resources.MergedDictionaries.RemoveAt(i);
                    break;
                }
            }

            app.Resources.MergedDictionaries.Add(accentDictionary);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем настройки
            AppSettings.CurrentSettings.Theme = ThemeManager.CurrentTheme.ToString();
            AppSettings.CurrentSettings.AccentColor = _currentAccentColor.ToString();
            AppSettings.SaveSettings();

            MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Восстанавливаем исходные настройки
            ThemeManager.LoadSavedTheme();
            DialogResult = false;
            Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход из аккаунта",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Закрываем все окна
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != this)
                        window.Close();
                }

                // Открываем окно входа
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Если окно закрыто через крестик
            if (DialogResult == null)
            {
                ThemeManager.LoadSavedTheme(); // Восстанавливаем старые настройки
            }
            base.OnClosing(e);
        }
    }
}