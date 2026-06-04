using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class EditAlbumWindow : Window
    {
        private readonly int _albumId;
        private readonly Album _album;
        private readonly AlbumRepository _albumRepo = new AlbumRepository();
        private readonly SongRepository _songRepo = new SongRepository();
        private string _selectedCoverPath = "";

        public EditAlbumWindow(int albumId)
        {
            InitializeComponent();
            _albumId = albumId;
            _album = _albumRepo.GetAlbumById(albumId);

            LoadAlbumData();
            LoadSongs();
        }

        private void LoadAlbumData()
        {
            if (_album != null)
            {
                AlbumTitleTextBox.Text = _album.Title;

                if (!string.IsNullOrEmpty(_album.CoverPath))
                {
                    try
                    {
                        AlbumCoverImage.Source = new BitmapImage(new Uri(_album.CoverPath));
                        CoverPathText.Text = Path.GetFileName(_album.CoverPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void LoadSongs()
        {
            var songs = _songRepo.GetSongsByAlbum(_albumId);
            SongsListView.ItemsSource = songs;
        }

        private void ChangeCoverButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png",
                Title = "Выберите обложку альбома"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedCoverPath = openFileDialog.FileName;
                CoverPathText.Text = Path.GetFileName(_selectedCoverPath);

                // Показываем превью
                AlbumCoverImage.Source = new BitmapImage(new Uri(_selectedCoverPath));
            }
        }

        private void SaveAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            var title = AlbumTitleTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название альбома", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string finalCoverPath = _album.CoverPath;

                // Если выбрана новая обложка
                if (!string.IsNullOrEmpty(_selectedCoverPath))
                {
                    string coversFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers");
                    if (!Directory.Exists(coversFolder))
                        Directory.CreateDirectory(coversFolder);

                    string fileName = $"album_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(_selectedCoverPath)}";
                    finalCoverPath = Path.Combine(coversFolder, fileName);
                    File.Copy(_selectedCoverPath, finalCoverPath, true);
                }

                // Обновляем альбом
                bool success = _albumRepo.UpdateAlbum(_albumId, title, finalCoverPath);

                if (success)
                {
                    MessageBox.Show("Альбом успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadAlbumData();
                }
                else
                {
                    MessageBox.Show("Не удалось обновить альбом", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении альбома: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить этот альбом? Все песни в нем также будут удалены.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _albumRepo.DeleteAlbum(_albumId);
                    MessageBox.Show("Альбом удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            var addSongWindow = new AddSongWindow(_albumId, _album.ArtistId);
            addSongWindow.Owner = this;
            if (addSongWindow.ShowDialog() == true)
            {
                LoadSongs(); // Обновляем список песен
            }
        }

        private void EditSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int songId)
            {
                var editSongWindow = new EditSongWindow(songId);
                editSongWindow.Owner = this;
                if (editSongWindow.ShowDialog() == true)
                {
                    LoadSongs();
                }
            }
        }

        private void DeleteSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int songId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту песню?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _songRepo.DeleteSong(songId);
                        LoadSongs();
                        MessageBox.Show("Песня удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}