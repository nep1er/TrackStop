using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.Managers
{
    public class PlayerManager
    {
        private readonly PlayerService _playerService;
        private readonly NavigationService _navigationService;
        private readonly ArtistRepository _artistRepo;
        private readonly AlbumRepository _albumRepo;

        private List<Song> _currentPlaylist;
        private int _currentIndex = 0;
        private bool _isDragging = false;

        private DispatcherTimer _progressTimer = new DispatcherTimer();

        public event Action<Song> SongChanged;

        public PlayerManager(
            PlayerService playerService,
            NavigationService navigationService,
            ArtistRepository artistRepo,
            AlbumRepository albumRepo)
        {
            _playerService = playerService;
            _navigationService = navigationService;
            _artistRepo = artistRepo;
            _albumRepo = albumRepo;

            _playerService.OnSongChanged += OnSongChanged;

            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += UpdateProgress;
        }

        public void StartTimer() => _progressTimer.Start();
        public void StopTimer() => _progressTimer.Stop();

        public void SetPlaylist(List<Song> songs)
        {
            _currentPlaylist = songs ?? new List<Song>();
        }

        public void PlaySong(int index)
        {
            if (index < 0 || index >= _currentPlaylist.Count)
                return;

            _currentIndex = index;
            var song = _currentPlaylist[index];

            if (song != null && System.IO.File.Exists(song.FilePath))
            {
                _playerService.Play(song);
            }
        }

        public void PlaySong(Song song)
        {
            var index = _currentPlaylist?.FindIndex(s => s.Id == song.Id) ?? -1;
            if (index >= 0)
            {
                PlaySong(index);
            }
        }

        public void PlayPause()
        {
            _playerService.TogglePlayPause();
        }

        public void Next()
        {
            if (_currentPlaylist == null || _currentPlaylist.Count == 0)
                return;

            _currentIndex = (_currentIndex + 1) % _currentPlaylist.Count;
            PlaySong(_currentIndex);
        }

        public void Previous()
        {
            if (_currentPlaylist == null || _currentPlaylist.Count == 0)
                return;

            _currentIndex = _currentIndex == 0 ? _currentPlaylist.Count - 1 : _currentIndex - 1;
            PlaySong(_currentIndex);
        }

        public void SeekToPosition(double ratio)
        {
            _playerService.SeekToPosition(ratio);
        }

        public void SetDragging(bool dragging) => _isDragging = dragging;
        public bool IsDragging => _isDragging;

        private void UpdateProgress(object sender, EventArgs e)
        {
        }

        private void OnSongChanged(Song song)
        {
            SongChanged?.Invoke(song);

            // Обновляем информацию о текущем альбоме и артисте
            var currentAlbum = _albumRepo.GetAllAlbums(App.CurrentUserId)
                .FirstOrDefault(a => a.Title == song.Album);
            var currentArtist = _artistRepo.GetArtistByName(song.Artist);

            _navigationService.CurrentViewedAlbum = currentAlbum;
            _navigationService.CurrentArtist = currentArtist;
        }

        public (double current, double total) GetCurrentProgress()
        {
            var currentTime = _playerService.GetCurrentTime().TotalSeconds;
            var totalTime = _playerService.GetTotalDuration().TotalSeconds;
            return (currentTime, totalTime);
        }

        public string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero)
                return "00:00";
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        public TimeSpan GetCurrentTime() => _playerService.GetCurrentTime();
        public TimeSpan GetTotalDuration() => _playerService.GetTotalDuration();

        public System.Windows.Media.Imaging.BitmapImage GetCover(Song song)
        {
            return _playerService.GetCover(song);
        }

        public Song GetCurrentSong()
        {
            if (_currentIndex >= 0 && _currentIndex < _currentPlaylist?.Count)
                return _currentPlaylist[_currentIndex];
            return null;
        }

        public int GetCurrentIndex() => _currentIndex;
        public int GetPlaylistCount() => _currentPlaylist?.Count ?? 0;
    }
}