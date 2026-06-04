using System;
using System.Collections.Generic;
using System.Windows;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.ViewModels
{
    public partial class AdminControlPanel : Window
    {
        private readonly UserRepository _userRepo = new UserRepository();
        private readonly ArtistRepository _artistRepo = new ArtistRepository();
        private readonly AlbumRepository _albumRepo = new AlbumRepository();
        private readonly SongRepository _songRepo = new SongRepository();

        public AdminControlPanel()
        {
            InitializeComponent();
            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadUsers();
            LoadAlbums();
            LoadSongs();
            LoadStatistics();
        }

        private void LoadUsers()
        {
            try
            {
                var users = _userRepo.GetAllUsers();
                UsersListView.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAlbums()
        {
            try
            {
                var albums = _albumRepo.GetAllAlbums();
                AlbumsListView.ItemsSource = albums;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки альбомов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSongs()
        {
            try
            {
                var songs = _songRepo.GetAllSongs();
                SongsListView.ItemsSource = songs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки песен: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                int usersCount = _userRepo.GetUsersCount();
                int artistsCount = _artistRepo.GetArtistsCount();
                int albumsCount = _albumRepo.GetAlbumsCount();
                int songsCount = _songRepo.GetSongsCount();

                UsersCountText.Text = usersCount.ToString();
                ArtistsCountText.Text = artistsCount.ToString();
                AlbumsCountText.Text = albumsCount.ToString();
                SongsCountText.Text = songsCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchUsersButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = UserSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                LoadUsers();
                return;
            }

            try
            {
                var users = _userRepo.SearchUsers(searchText);
                UsersListView.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchAlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = AlbumSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                LoadAlbums();
                return;
            }

            try
            {
                var albums = _albumRepo.SearchAlbums(searchText);
                AlbumsListView.ItemsSource = albums;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска альбомов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchSongsButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SongSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                LoadSongs();
                return;
            }

            try
            {
                var songs = _songRepo.SearchSongs(searchText);
                SongsListView.ItemsSource = songs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска песен: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int userId)
            {
                // Нельзя удалить самого себя (админа)
                if (userId == App.CurrentUserId)
                {
                    MessageBox.Show("Вы не можете удалить свой собственный аккаунт!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя с ID: {userId}? Все его данные также будут удалены.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = _userRepo.DeleteUser(userId);
                        if (success)
                        {
                            MessageBox.Show("Пользователь успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsers();
                            LoadStatistics();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void DeleteAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int albumId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот альбом? Все песни в нем также будут удалены.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _albumRepo.DeleteAlbum(albumId);
                        MessageBox.Show("Альбом успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAlbums();
                        LoadSongs(); // Обновляем список песен
                        LoadStatistics();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении альбома: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                        bool success = _songRepo.DeleteSong(songId);
                        if (success)
                        {
                            MessageBox.Show("Песня успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadSongs();
                            LoadStatistics();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить песню", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении песни: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
            MessageBox.Show("Данные обновлены", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}