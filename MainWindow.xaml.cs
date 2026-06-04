using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using TrackStop.Managers;
using TrackStop.Models;
using TrackStop.Models.Search;
using TrackStop.Services;
using TrackStop.ViewModels;

namespace TrackStop
{
    public partial class MainWindow : Window
    {
        // Сервисы
        private readonly NavigationService _navigationService;
        private readonly FilterManager _filterManager;
        private readonly SearchManager _searchManager;
        private readonly PlayerManager _playerManager;
        private readonly GenreRepository _genreRepo;
        private readonly UserRepository _userRepo;
        public string CurrentUserName => App.CurrentUserLogin;
        // Таймеры
        private readonly DispatcherTimer _searchTimer = new DispatcherTimer();
        private readonly DispatcherTimer _progressTimer = new DispatcherTimer();
        public bool IsUserArtist => App.IsArtist;
        // Состояние
        private enum ContentType { Albums, Songs, Search, Artist, FavoriteSongs, FavoriteAlbums, FavoriteArtists }
        private ContentType _currentContentType = ContentType.Albums;
        private bool _isDragging = false;

        // Привязки данных
        public ObservableCollection<Album> AllAlbums => _navigationService.AllAlbums;
        public ObservableCollection<Song> AllSongs => _navigationService.AllSongs;
        public ObservableCollection<GenreFilterItem> GenreFilters => _filterManager.GenreFilters;

        private Artist _currentUserArtist;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Инициализация репозиториев
            var songRepo = new SongRepository();
            var albumRepo = new AlbumRepository();
            var artistRepo = new ArtistRepository();
            _genreRepo = new GenreRepository();
            var searchService = new SearchService(songRepo, albumRepo, _genreRepo, artistRepo);
            var playerService = new PlayerService(Player);
            _userRepo = new UserRepository();

            // Инициализация сервисов
            _navigationService = new NavigationService(albumRepo, songRepo, artistRepo, this);
            _filterManager = new FilterManager(songRepo);
            _searchManager = new SearchManager(searchService, albumRepo, artistRepo, _navigationService);
            _playerManager = new PlayerManager(playerService, _navigationService, artistRepo, albumRepo);

            Initialize();
        }

        private void Initialize()
        {
            Title = $"TrackStop - {App.CurrentUserLogin}";

            if (App.IsArtist)
            {
                var artistRepo = new ArtistRepository();
                _currentUserArtist = artistRepo.GetArtistByIdWithUser(App.CurrentUserId);
            }

            FolderService.EnsureRequiredFolders();

            ExitBtn.Visibility = Visibility.Collapsed;
            if (App.IsArtist) UserRoleText.Text = "Artist";
            else if (App.IsAdmin) UserRoleText.Text = "Admin";
            else UserRoleText.Text = "User";
            

            // Инициализация таймеров
            _searchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _searchTimer.Tick += OnSearchTimerTick;

            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += UpdateProgress;
            _progressTimer.Start();

            SetupSearchBox();

            _playerManager.SongChanged += UpdateCurrentSongUI;

            // Загрузка данных
            LoadInitialData();

            _playerManager.StartTimer();
            Console.WriteLine($"Пользователь вошел: {App.CurrentUserLogin} (ID: {App.CurrentUserId})");
        }

        private void OpenArtistPageByUserId(int userId)
        {
            var artistRepo = new ArtistRepository();
            var artist = artistRepo.GetArtistByIdWithUser(userId);
            if (artist != null) ShowArtistPage(artist);
        }

        private void UserProfile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (App.IsArtist)
            {
                OpenArtistPageByUserId(App.CurrentUserId);
            }

            else if (App.IsAdmin)
            {
                var adminPanel = new AdminControlPanel();
                adminPanel.Owner = this;
                adminPanel.ShowDialog();
            }
            /*
            else
            {
                // Показываем настройки для всех пользователей
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            */
        }

        private void ShowArtistPage(Artist artist)
        {
            _navigationService.CurrentArtist = artist;
            _currentContentType = ContentType.Artist;

            var artistRepo = new ArtistRepository();

            bool isFavorite = artistRepo.IsFavorite(artist.Id, App.CurrentUserId);
            artist.IsFavorite = isFavorite;

            var artistAlbums = _navigationService.GetArtistAlbums(artist);

            // Обновляем UI
            ArtistName.Text = artist.Name;

            // Форматируем подписчиков правильно
            string formattedSubscribers = $"{artist.FormattedSubscribers} subscribers";
            ArtistSubscribers.Text = formattedSubscribers;

            ArtistBio.Text = artist.Bio ?? "Биография отсутствует";

            bool isCurrentUserArtist = (artist.UserId == App.CurrentUserId) ||
                                      (_currentUserArtist != null && _currentUserArtist.Id == artist.Id);

            if (isCurrentUserArtist)
            {
                ArtistFavoriteBtn.Content = "Edit";
                ArtistFavoriteBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ArtistFavoriteBtn.Content = isFavorite ? "❤ Following" : "❤ Follow";
                ArtistFavoriteBtn.Visibility = Visibility.Visible;
                // Не нужно вызывать UpdateArtistFavoriteButtonText, так как текст уже установлен выше
            }

            // Загрузка фото артиста
            if (!string.IsNullOrEmpty(artist.PhotoPath))
            {
                try
                {
                    ArtistPhoto.Source = LoadImageSafely(artist.PhotoPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки фото артиста: {ex.Message}");
                    ArtistPhoto.Source = GetDefaultImage();
                }
            }
            else
            {
                ArtistPhoto.Source = GetDefaultImage();
            }

            ArtistAlbumsGrid.ItemsSource = artistAlbums;
            HideAllPanels();
            ArtistPanel.Visibility = Visibility.Visible;
        }

        private void ArtistProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsArtist && _currentUserArtist != null)
            {
                ShowArtistPage(_currentUserArtist);
            }
        }

        private void ArtistFavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ArtistFavoriteBtn.Content == "Edit")
            {
                var artistRepo = new ArtistRepository();
                var userArtist = artistRepo.GetArtistByIdWithUser(App.CurrentUserId);

                if (userArtist != null)
                {
                    var artistSettingsWindow = new ArtistSettingsWindow(userArtist);
                    artistSettingsWindow.Owner = this;
                    artistSettingsWindow.ShowDialog();
                }
            }
            else if (_navigationService.CurrentArtist != null)
            {
                bool isFavorite = !_navigationService.CurrentArtist.IsFavorite;

                // Обновляем через ArtistRepository
                var artistRepo = new ArtistRepository();
                artistRepo.SetFavorite(_navigationService.CurrentArtist.Id, isFavorite, App.CurrentUserId);

                // Обновляем свойство артиста
                _navigationService.CurrentArtist.IsFavorite = isFavorite;

                // Обновляем текст кнопки
                ArtistFavoriteBtn.Content = isFavorite ? "❤ Following" : "❤ Follow";

                // Обновляем отображение подписчиков
                var updatedArtist = artistRepo.GetArtistById(_navigationService.CurrentArtist.Id);
                if (updatedArtist != null)
                {
                    _navigationService.CurrentArtist.Subscribers = updatedArtist.Subscribers;
                    ArtistSubscribers.Text = $"{_navigationService.CurrentArtist.FormattedSubscribers} subscribers";
                }
            }
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            
        }
        #region Профиль и настройки


        #endregion

        private void LoadInitialData()
        {
            _navigationService.LoadAlbums();
            _navigationService.LoadAllSongs();

            var allGenres = _genreRepo.GetAllGenres();
            _filterManager.InitializeGenreFilters(allGenres);
            GenreFilterList.ItemsSource = _filterManager.GenreFilters;

            ShowAllAlbums();
        }



        #region Навигация

        private void ShowAllAlbums()
        {
            HideAllPanels();
            _currentContentType = ContentType.Albums;
            _navigationService.ShowAllAlbums();

            var filteredAlbums = _filterManager.ApplyFilterToAlbums(_navigationService.AllAlbums.ToList());
            AlbumsGrid.ItemsSource = filteredAlbums;
            AlbumsGrid.Visibility = Visibility.Visible;
        }

        private void ShowFavoriteAlbums()
        {
            HideAllPanels();
            _currentContentType = ContentType.FavoriteAlbums;

            var favoriteAlbums = _navigationService.GetFavoriteAlbums();
            var filteredAlbums = _filterManager.ApplyFilterToAlbums(favoriteAlbums);

            AlbumsGrid.ItemsSource = filteredAlbums;
            AlbumsGrid.Visibility = Visibility.Visible;
        }

        private void ShowFavoriteSongs()
        {
            HideAllPanels();
            _currentContentType = ContentType.FavoriteSongs;
            AlbumTitleHeader.Visibility = Visibility.Visible;
            AlbumTitleHeader.Text = "Liked Songs";

            var favoriteSongs = _navigationService.GetFavoriteSongs();
            var filteredSongs = _filterManager.ApplyFilterToSongs(favoriteSongs);

            SongsList.ItemsSource = filteredSongs;
            _playerManager.SetPlaylist(filteredSongs);
            SongsList.Visibility = Visibility.Visible;
        }

        private void ShowFavoriteArtists()
        {
            HideAllPanels();
            _currentContentType = ContentType.FavoriteArtists;

            var favoriteArtists = _navigationService.GetFavoriteArtists();
            AlbumsGrid.ItemsSource = favoriteArtists;
            AlbumsGrid.Visibility = Visibility.Visible;
        }

        private void ShowSongsByAlbum(int albumId)
        {
            HideAllPanels();
            _currentContentType = ContentType.Songs;

            _navigationService.LoadSongsByAlbum(albumId);
            _navigationService.UpdateAlbumHeader(_navigationService.CurrentViewedAlbum);

            var filteredSongs = _filterManager.ApplyFilterToSongs(_navigationService.CurrentSongs);
            SongsList.ItemsSource = filteredSongs;
            _playerManager.SetPlaylist(filteredSongs);
            SongsList.Visibility = Visibility.Visible;
        }

        

        private void ShowSongsByGenre(Genre genre)
        {
            HideAllPanels();

            var songsByGenre = _navigationService.GetSongsByGenre(genre);
            var filteredSongs = _filterManager.ApplyFilterToSongs(songsByGenre);

            SongsList.ItemsSource = filteredSongs;
            _playerManager.SetPlaylist(filteredSongs);
            SongsList.Visibility = Visibility.Visible;
        }

        private void ShowMainPage()
        {
            HideAllPanels();
            _currentContentType = ContentType.Albums;

            var filteredAlbums = _filterManager.ApplyFilterToAlbums(_navigationService.AllAlbums.ToList());
            AlbumsGrid.ItemsSource = filteredAlbums;
            AlbumsGrid.Visibility = Visibility.Visible;
        }

        private void HideAllPanels()
        {
            AlbumsGrid.Visibility = Visibility.Collapsed;
            SongsList.Visibility = Visibility.Collapsed;
            ArtistPanel.Visibility = Visibility.Collapsed;
            AlbumTitleHeader.Visibility = Visibility.Collapsed;
            AlbumTitleHeader.Text = "";

            AlbumsGrid.ItemsSource = null;
            SongsList.ItemsSource = null;
        }

        #endregion

        #region Поиск

        private void SetupSearchBox()
        {
            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void OnSearchTimerTick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            PerformSearch();
        }

        private void PerformSearch()
        {
            var query = SearchBox.Text.Trim();

            if (string.IsNullOrEmpty(query))
            {
                ShowMainPage();
                return;
            }

            var searchResults = _searchManager.GetSearchResults(query, _filterManager);
            DisplaySearchResults(searchResults);
        }

        private void DisplaySearchResults(List<object> searchResults)
        {
            HideAllPanels();
            _currentContentType = ContentType.Search;

            AlbumsGrid.ItemsSource = searchResults;
            AlbumsGrid.Visibility = Visibility.Visible;
        }

        #endregion

        #region Обработчики событий UI

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAllAlbums();
        }

        private void PlaylistsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowFavoriteAlbums();
        }

        private void FavoriteSongsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowFavoriteSongs();
        }

        private void FavoriteArtistsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowFavoriteArtists();
        }

        private void Album_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Album album)
            {
                ShowSongsByAlbum(album.Id);
            }
        }

        private void BackToAlbums_Click(object sender, RoutedEventArgs e)
        {
            ShowAllAlbums();
        }

        private void CurrentAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationService.CurrentViewedAlbum != null)
            {
                ShowSongsByAlbum(_navigationService.CurrentViewedAlbum.Id);
            }
        }

        private void CurrentArtistButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navigationService.CurrentArtist != null)
            {
                ShowArtistPage(_navigationService.CurrentArtist);
            }
        }

        private void ArtistSearchItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Artist artist)
            {
                ShowArtistPage(artist);
            }
        }

        private void SongSearchItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Song song)
            {
                var searchSongs = _searchManager.GetSearchSongs(SearchBox.Text.Trim());
                var songIndex = _searchManager.FindSongIndex(searchSongs, song);

                if (songIndex >= 0)
                {
                    _playerManager.SetPlaylist(searchSongs);
                    _playerManager.PlaySong(songIndex);
                }
            }
        }

        private void GenreSearchItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GenreSearchItem genreItem)
            {
                ShowSongsByGenre(genreItem.Genre);
            }
        }

        private void SongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SongsList.SelectedIndex >= 0)
            {
                _playerManager.PlaySong(SongsList.SelectedIndex);
                SongsList.SelectedItem = null;
            }
        }

        #endregion

        #region Избранное

        private void FavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.DataContext is Album album)
            {
                bool isFavorite = btn.IsChecked == true;
                _navigationService.UpdateAlbumFavoriteStatus(album, isFavorite, App.CurrentUserId);

            }
        }

        private void SongFavoriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && btn.DataContext is Song song)
            {
                bool isFavorite = btn.IsChecked == true;
                FavoriteSongRepository.SetFavorite(song.Id, isFavorite, App.CurrentUserId);
                song.IsFavorite = isFavorite;

            }
        }


        private void RefreshFavoriteAlbumsIfNeeded()
        {
            if (_currentContentType == ContentType.FavoriteAlbums)
            {
                ShowFavoriteAlbums();
            }
        }

        private void RefreshFavoriteSongsIfNeeded()
        {
            if (_currentContentType == ContentType.FavoriteSongs)
            {
                ShowFavoriteSongs();
            }
        }

        private void UpdateArtistFavoriteButtonText(bool isFavorite)
        {
            ArtistFavoriteBtn.Content = isFavorite ? " В подписках " : " Подписаться ";
        }

        #endregion

        #region Плеер

        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            _playerManager.PlayPause();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _playerManager.Next();
        }

        private void UpdateCurrentSongUI(Song song)
        {
            CurrentSongTitle.Text = song.Title;
            CurrentAlbumText.Text = song.Album ?? "Альбом";
            CurrentArtistText.Text = song.Artist;
            CurrentSongCover.Source = _playerManager.GetCover(song);


            // Сбрасываем отображение времени
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateTimeDisplay();
            }), DispatcherPriority.Background);
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (!_isDragging)
            {
                var (current, total) = _playerManager.GetCurrentProgress();
                PlayerProgress.Maximum = total;
                PlayerProgress.Value = current;
                UpdateTimeDisplay();
            }
        }

        private void UpdateTimeDisplay()
        {
            var currentTime = _playerManager.FormatTime(_playerManager.GetCurrentTime());
            var totalTime = _playerManager.FormatTime(_playerManager.GetTotalDuration());

            CurrentTimeText.Text = currentTime;
            TotalTimeText.Text = totalTime;
        }

        private void PlayerProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _playerManager.SetDragging(true);
        }

        private void PlayerProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PlayerProgress.ActualWidth > 0)
            {
                double ratio = e.GetPosition(PlayerProgress).X / PlayerProgress.ActualWidth;
                _playerManager.SeekToPosition(ratio);
                UpdateTimeDisplay();
            }
            _isDragging = false;
            _playerManager.SetDragging(false);
        }

        #endregion

        #region Фильтры

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        private void GenreCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is GenreFilterItem genreItem)
            {
                _filterManager.ToggleGenreFilter(genreItem.Id, true);
                UpdateFilterIndicator();
                ApplyCurrentFilter();
            }
        }

        private void GenreCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is GenreFilterItem genreItem)
            {
                _filterManager.ToggleGenreFilter(genreItem.Id, false);
                UpdateFilterIndicator();
                ApplyCurrentFilter();
            }
        }

        private void SelectAllGenres_Click(object sender, RoutedEventArgs e)
        {
            _filterManager.SelectAllGenres();
            UpdateFilterIndicator();
            ApplyCurrentFilter();
        }

        private void ClearAllGenres_Click(object sender, RoutedEventArgs e)
        {
            _filterManager.ClearAllGenres();
            UpdateFilterIndicator();
            ApplyCurrentFilter();
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = false;
            UpdateFilterIndicator();
            ApplyCurrentFilter();
        }

        private void UpdateFilterIndicator()
        {
            FilterButton.Content = _filterManager.GetFilterIndicatorText();
            FilterButton.ToolTip = _filterManager.IsFilterApplied
                ? $"Filter applied: {_filterManager.SelectedGenreIds.Count} genre(s) selected"
                : "Filter by genre";
        }

        private void ApplyCurrentFilter()
        {
            switch (_currentContentType)
            {
                case ContentType.Albums:
                    var filteredAlbums = _filterManager.ApplyFilterToAlbums(_navigationService.AllAlbums.ToList());
                    AlbumsGrid.ItemsSource = filteredAlbums;
                    break;

                case ContentType.Songs:
                    var filteredSongs = _filterManager.ApplyFilterToSongs(_navigationService.CurrentSongs);
                    SongsList.ItemsSource = filteredSongs;
                    _playerManager.SetPlaylist(filteredSongs);
                    break;

                case ContentType.FavoriteAlbums:
                    var favAlbums = _navigationService.GetFavoriteAlbums();
                    var filteredFavAlbums = _filterManager.ApplyFilterToAlbums(favAlbums);
                    AlbumsGrid.ItemsSource = filteredFavAlbums;
                    break;

                case ContentType.FavoriteSongs:
                    var favSongs = _navigationService.GetFavoriteSongs();
                    var filteredFavSongs = _filterManager.ApplyFilterToSongs(favSongs);
                    SongsList.ItemsSource = filteredFavSongs;
                    _playerManager.SetPlaylist(filteredFavSongs);
                    break;

                case ContentType.Search:
                    // Для поиска фильтр уже применяется в DisplaySearchResults
                    break;

                case ContentType.Artist:
                case ContentType.FavoriteArtists:
                    // Для артистов фильтрация не применяется
                    break;
            }
        }

        #endregion

        #region Настройки и профиль


        private void BackFromSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        #endregion

        #region Вспомогательные методы

        private System.Windows.Media.Imaging.BitmapImage LoadImageSafely(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return GetDefaultImage();

                string fullPath;
                if (System.IO.Path.IsPathRooted(imagePath))
                {
                    fullPath = imagePath;
                }
                else
                {
                    fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }

                if (System.IO.File.Exists(fullPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                else
                {
                    return GetDefaultImage();
                }
            }
            catch
            {
                return GetDefaultImage();
            }
        }

        private System.Windows.Media.Imaging.BitmapImage GetDefaultImage()
        {
            var width = 200;
            var height = 200;

            var visual = new System.Windows.Media.DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(System.Windows.Media.Brushes.Black, null,
                    new System.Windows.Rect(0, 0, width, height));
                var text = new System.Windows.Media.FormattedText(
                    "No Image",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Arial"),
                    14,
                    System.Windows.Media.Brushes.White,
                    System.Windows.Media.VisualTreeHelper.GetDpi(visual).PixelsPerDip);
                context.DrawText(text, new System.Windows.Point(width / 2 - text.Width / 2, height / 2 - text.Height / 2));
            }

            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96,
                System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            var bitmapEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            bitmapEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

            using (var stream = new System.IO.MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        #endregion
    }
}