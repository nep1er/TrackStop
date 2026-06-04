using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class AddSongWindow : Window
    {
        private readonly int _albumId;
        private readonly int _artistId;
        private readonly SongRepository _songRepo = new SongRepository();
        private readonly AlbumRepository _albumRepo = new AlbumRepository();
        private string _selectedAudioPath = "";

        public AddSongWindow(int albumId, int artistId)
        {
            InitializeComponent();
            _albumId = albumId;
            _artistId = artistId;

        }

        private void SelectAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3; *.wav; *.flac)|*.mp3; *.wav; *.flac",
                Title = "Выберите аудиофайл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAudioPath = openFileDialog.FileName;
                AudioFilePathText.Text = Path.GetFileName(_selectedAudioPath);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var title = SongTitleTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название песни", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(_selectedAudioPath))
            {
                MessageBox.Show("Выберите аудиофайл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Копир аудиофайл в папку приложения
                string songsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Songs");
                if (!Directory.Exists(songsFolder))
                    Directory.CreateDirectory(songsFolder);

                string audioFileName = $"song_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(_selectedAudioPath)}";
                string finalAudioPath = Path.Combine(songsFolder, audioFileName);
                File.Copy(_selectedAudioPath, finalAudioPath, true);

                // Добавляем песню в базу данных
                int songId = _songRepo.AddSong(title, finalAudioPath, _albumId, _artistId);

                if (songId > 0)
                {
                    MessageBox.Show("Песня успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось добавить песню", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении песни: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}