using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class EditSongWindow : Window
    {
        private readonly int _songId;
        private readonly Song _song;
        private readonly SongRepository _songRepo = new SongRepository();
        private string _selectedAudioPath = "";

        public EditSongWindow(int songId)
        {
            InitializeComponent();
            _songId = songId;
            _song = _songRepo.GetSongById(songId);

            LoadSongData();
        }

        private void LoadSongData()
        {
            if (_song != null)
            {
                SongTitleTextBox.Text = _song.Title;
                CurrentAudioFileText.Text = Path.GetFileName(_song.FilePath);
            }
        }

        private void ReplaceAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3; *.wav; *.flac)|*.mp3; *.wav; *.flac",
                Title = "Выберите новый аудиофайл"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAudioPath = openFileDialog.FileName;
                CurrentAudioFileText.Text = $"Будет заменен на: {Path.GetFileName(_selectedAudioPath)}";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var title = SongTitleTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название песни", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Обновляем название
                bool success = _songRepo.UpdateSong(_songId, title);

                // Если выбран новый файл, заменяем его
                if (!string.IsNullOrEmpty(_selectedAudioPath))
                {
                    string songsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Songs");
                    if (!Directory.Exists(songsFolder))
                        Directory.CreateDirectory(songsFolder);

                    string fileName = $"song_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(_selectedAudioPath)}";
                    string finalPath = Path.Combine(songsFolder, fileName);
                    File.Copy(_selectedAudioPath, finalPath, true);

                    // Обновляем путь в базе данных
                    using var conn = new System.Data.SqlClient.SqlConnection(
                        @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;");
                    conn.Open();

                    using var cmd = new System.Data.SqlClient.SqlCommand(
                        "UPDATE Song SET FilePath = @filePath WHERE Id = @songId", conn);
                    cmd.Parameters.AddWithValue("@filePath", finalPath);
                    cmd.Parameters.AddWithValue("@songId", _songId);
                    cmd.ExecuteNonQuery();
                }

                if (success)
                {
                    MessageBox.Show("Песня успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось обновить песню", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении песни: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}