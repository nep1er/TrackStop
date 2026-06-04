using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class NavigationService
    {
        private readonly AlbumRepository _albumRepo;
        private readonly SongRepository _songRepo;
        private readonly ArtistRepository _artistRepo;
        private readonly MainWindow _mainWindow;

        private Album _currentViewedAlbum;
        private Artist _currentArtist;

        public ObservableCollection<Album> AllAlbums { get; private set; } = new ObservableCollection<Album>();
        public ObservableCollection<Song> AllSongs { get; private set; } = new ObservableCollection<Song>();
        public List<Song> CurrentSongs { get; private set; } = new List<Song>();

        public Album CurrentViewedAlbum
        {
            get => _currentViewedAlbum;
            set => _currentViewedAlbum = value;
        }

        public Artist CurrentArtist
        {
            get => _currentArtist;
            set => _currentArtist = value;
        }

        public NavigationService(
            AlbumRepository albumRepo,
            SongRepository songRepo,
            ArtistRepository artistRepo,
            MainWindow mainWindow)
        {
            _albumRepo = albumRepo;
            _songRepo = songRepo;
            _artistRepo = artistRepo;
            _mainWindow = mainWindow;
        }

        public void ShowAllAlbums()
        {
            var albumsFromDb = _albumRepo.GetAllAlbums(App.CurrentUserId);
            var favAlbumIds = _albumRepo.GetFavoriteIds(App.CurrentUserId);

            foreach (var album in albumsFromDb)
                album.IsFavorite = favAlbumIds.Contains(album.Id);

            AllAlbums.Clear();
            foreach (var album in albumsFromDb)
                AllAlbums.Add(album);
        }

        public void LoadAllSongs()
        {
            var songsFromDb = _songRepo.GetAllSongs(App.CurrentUserId);
            AllSongs.Clear();
            foreach (var song in songsFromDb)
                AllSongs.Add(song);
        }

        public void LoadSongsByAlbum(int albumId)
        {
            var album = _albumRepo.GetAlbumById(albumId, App.CurrentUserId);
            if (album != null)
            {
                _currentViewedAlbum = album;
                CurrentSongs = _songRepo.GetSongsByAlbum(albumId, App.CurrentUserId);
            }
        }

        public List<Album> GetFavoriteAlbums()
        {
            var favAlbumIds = _albumRepo.GetFavoriteIds(App.CurrentUserId);
            return AllAlbums.Where(album => favAlbumIds.Contains(album.Id)).ToList();
        }

        public List<Song> GetFavoriteSongs()
        {
            var favIds = FavoriteSongRepository.GetFavoriteIds(App.CurrentUserId);
            CurrentSongs = AllSongs.Where(song => favIds.Contains(song.Id)).ToList();

            foreach (var song in CurrentSongs)
                song.IsFavorite = true;

            return CurrentSongs;
        }

        public List<Artist> GetFavoriteArtists()
        {
            return _artistRepo.GetFavoriteArtists(App.CurrentUserId);
        }

        public List<Album> GetArtistAlbums(Artist artist)
        {
            bool isFavorite = _artistRepo.IsFavorite(artist.Id, App.CurrentUserId);
            artist.IsFavorite = isFavorite;

            return _albumRepo.GetAlbumsByArtist(artist.Id, App.CurrentUserId);
        }

        public List<Song> GetSongsByGenre(Genre genre)
        {
            CurrentSongs = _songRepo.GetSongsByGenre(genre.Id, App.CurrentUserId);
            var favIds = FavoriteSongRepository.GetFavoriteIds(App.CurrentUserId);

            foreach (var song in CurrentSongs)
                song.IsFavorite = favIds.Contains(song.Id);

            return CurrentSongs;
        }

        public void UpdateArtistFavoriteStatus(Artist artist, bool isFavorite)
        {
            _artistRepo.SetFavorite(artist.Id, isFavorite, App.CurrentUserId);
            artist.IsFavorite = isFavorite;

            var updatedArtist = _artistRepo.GetArtistById(artist.Id);
            if (updatedArtist != null)
            {
                artist.Subscribers = updatedArtist.Subscribers;
            }
        }

        public void UpdateAlbumFavoriteStatus(Album album, bool isFavorite, int userId)
        {
            _albumRepo.SetFavorite(album.Id, isFavorite, userId);
            album.IsFavorite = isFavorite;
        }

        public void LoadAlbums()
        {
            var albumsFromDb = _albumRepo.GetAllAlbums(App.CurrentUserId);
            AllAlbums.Clear();
            foreach (var album in albumsFromDb)
                AllAlbums.Add(album);
        }

        public void UpdateAlbumHeader(Album album)
        {
            if (album != null)
            {
                _mainWindow.AlbumTitleHeader.Text = album.Title;
                _mainWindow.AlbumTitleHeader.Visibility = Visibility.Visible;
            }
            else
            {
                _mainWindow.AlbumTitleHeader.Text = "";
                _mainWindow.AlbumTitleHeader.Visibility = Visibility.Collapsed;
            }
        }
    }
}