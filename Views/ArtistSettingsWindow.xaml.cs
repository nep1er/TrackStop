using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class ArtistSettingsWindow : Window
    {
        private readonly Artist _artist;
        private readonly ArtistRepository _artistRepo = new ArtistRepository();
        private readonly AlbumRepository _albumRepo = new AlbumRepository();
        private string _selectedAvatarPath = "";

        public ArtistSettingsWindow(Artist artist)
        {
            InitializeComponent();
            _artist = artist;

            // Заполняем поля данными артиста
            ArtistNameTextBox.Text = _artist.Name;
            ArtistBioTextBox.Text = _artist.Bio;

            if (!string.IsNullOrEmpty(_artist.PhotoPath))
            {
                AvatarPathText.Text = Path.GetFileName(_artist.PhotoPath);
                try
                {
                    AvatarPreview.Source = new BitmapImage(new Uri(_artist.PhotoPath));
                }
                catch
                {
                }
            }

            LoadArtistAlbums();
        }

        private void LoadArtistAlbums()
        {
            var albums = _albumRepo.GetAlbumsByArtist(_artist.Id);
            ArtistAlbumsListView.ItemsSource = albums;
        }

        private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png",
                Title = "Выберите аватар исполнителя"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAvatarPath = openFileDialog.FileName;
                AvatarPathText.Text = Path.GetFileName(_selectedAvatarPath);

                // Показываем превью
                AvatarPreview.Source = new BitmapImage(new Uri(_selectedAvatarPath));
            }
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ArtistNameTextBox.Text.Trim();
            var bio = ArtistBioTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя исполнителя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Обновляем путь к аватару если выбран новый
                string finalAvatarPath = _artist.PhotoPath;
                if (!string.IsNullOrEmpty(_selectedAvatarPath))
                {
                    string artistsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Artists");
                    if (!Directory.Exists(artistsFolder))
                        Directory.CreateDirectory(artistsFolder);

                    string fileName = $"artist_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(_selectedAvatarPath)}";
                    finalAvatarPath = Path.Combine(artistsFolder, fileName);
                    File.Copy(_selectedAvatarPath, finalAvatarPath, true);
                }

                // Обновляем данные артиста
                _artist.Name = name;
                _artist.Bio = bio;
                _artist.PhotoPath = finalAvatarPath;

                // Используем метод из ArtistRepository
                bool success = _artistRepo.UpdateArtist(_artist);

                if (success)
                {
                    MessageBox.Show("Профиль успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось обновить профиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            var createAlbumWindow = new CreateAlbumWindow(_artist.Id);
            createAlbumWindow.Owner = this;
            if (createAlbumWindow.ShowDialog() == true)
            {
                LoadArtistAlbums(); // Обновляем список
            }
        }

        private void EditAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int albumId)
            {
                var editAlbumWindow = new EditAlbumWindow(albumId);
                editAlbumWindow.Owner = this;
                if (editAlbumWindow.ShowDialog() == true)
                {
                    LoadArtistAlbums();
                }
            }
        }

        private void DeleteAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int albumId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот альбом? Это действие нельзя отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _albumRepo.DeleteAlbum(albumId);
                        LoadArtistAlbums();
                        MessageBox.Show("Альбом удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из аккаунта исполнителя?",
                "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Выход из приложения
                App.CurrentUserId = -1;
                App.CurrentUserLogin = "";
                App.CurrentUserRole = "user";

                // Закрываем все окна
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != this)
                        window.Close();
                }

                // Показываем окно входа
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}