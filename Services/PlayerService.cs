using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class PlayerService
    {
        private readonly MediaElement _player;
        private bool _isPlaying;

        public event Action<Song> OnSongChanged;

        public PlayerService(MediaElement player)
        {
            _player = player;
        }

        public bool IsPlaying => _isPlaying;

        public void Play(Song song)
        {
            if (song == null || !System.IO.File.Exists(song.FilePath)) return;

            _player.Source = new Uri(song.FilePath, UriKind.Absolute);
            _player.Play();
            _isPlaying = true;

            OnSongChanged?.Invoke(song);
        }

        public void Pause()
        {
            _player.Pause();
            _isPlaying = false;
        }

        public void Resume()
        {
            _player.Play();
            _isPlaying = true;
        }

        public void TogglePlayPause()
        {
            if (_isPlaying) Pause();
            else Resume();
        }

        public BitmapImage GetCover(Song song)
        {
            return new BitmapImage(new Uri(song.CoverPath, UriKind.Absolute));
        }

        public void SeekToPosition(double ratio)
        {
            if (_player.NaturalDuration.HasTimeSpan)
            {
                var newPosition = TimeSpan.FromSeconds(ratio * _player.NaturalDuration.TimeSpan.TotalSeconds);
                _player.Position = newPosition;
            }
        }

        public TimeSpan GetCurrentTime()
        {
            return _player.Position;
        }

        public TimeSpan GetTotalDuration()
        {
            return _player.NaturalDuration.HasTimeSpan
                ? _player.NaturalDuration.TimeSpan
                : TimeSpan.Zero;
        }
    }
}