using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.Managers
{
    public class FilterManager
    {
        private readonly ObservableCollection<GenreFilterItem> _genreFilters = new ObservableCollection<GenreFilterItem>();
        private readonly List<int> _selectedGenreIds = new List<int>();
        private bool _isFilterApplied = false;

        private Dictionary<int, List<Song>> _albumSongsCache = new Dictionary<int, List<Song>>();
        private readonly SongRepository _songRepo;

        public FilterManager(SongRepository songRepo)
        {
            _songRepo = songRepo;
        }

        public ObservableCollection<GenreFilterItem> GenreFilters => _genreFilters;
        public List<int> SelectedGenreIds => _selectedGenreIds;
        public bool IsFilterApplied => _isFilterApplied;

        public void InitializeGenreFilters(List<Genre> allGenres)
        {
            _genreFilters.Clear();
            foreach (var genre in allGenres)
            {
                _genreFilters.Add(new GenreFilterItem
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    IsSelected = false
                });
            }
        }

        public List<Song> GetCachedSongsByAlbum(int albumId)
        {
            if (!_albumSongsCache.ContainsKey(albumId))
            {
                _albumSongsCache[albumId] = _songRepo.GetSongsByAlbum(albumId, App.CurrentUserId);
            }
            return _albumSongsCache[albumId];
        }

        public void ClearCache()
        {
            _albumSongsCache.Clear();
        }

        public void ToggleGenreFilter(int genreId, bool isSelected)
        {
            if (isSelected && !_selectedGenreIds.Contains(genreId))
            {
                _selectedGenreIds.Add(genreId);
            }
            else if (!isSelected)
            {
                _selectedGenreIds.Remove(genreId);
            }

            _isFilterApplied = _selectedGenreIds.Count > 0;
        }

        public void SelectAllGenres()
        {
            _selectedGenreIds.Clear();
            foreach (var genre in _genreFilters)
            {
                genre.IsSelected = true;
                _selectedGenreIds.Add(genre.Id);
            }
            _isFilterApplied = true;
        }

        public void ClearAllGenres()
        {
            _selectedGenreIds.Clear();
            foreach (var genre in _genreFilters)
                genre.IsSelected = false;
            _isFilterApplied = false;
        }

        // Методы фильтрации
        public List<Album> ApplyFilterToAlbums(List<Album> albums)
        {
            if (!_isFilterApplied || _selectedGenreIds.Count == 0)
                return albums;

            return albums.Where(album =>
            {
                var albumSongs = GetCachedSongsByAlbum(album.Id);
                return albumSongs.Any(song => _selectedGenreIds.Contains(song.GenreId));
            }).ToList();
        }

        public List<Song> ApplyFilterToSongs(List<Song> songs)
        {
            if (!_isFilterApplied || _selectedGenreIds.Count == 0)
                return songs;

            return songs.Where(song => _selectedGenreIds.Contains(song.GenreId)).ToList();
        }

        public List<object> ApplyFilterToSearchResults(List<object> searchResults)
        {
            if (!_isFilterApplied || _selectedGenreIds.Count == 0)
                return searchResults;

            var filteredItems = new List<object>();

            foreach (var item in searchResults)
            {
                if (item is Album album)
                {
                    var albumSongs = GetCachedSongsByAlbum(album.Id);
                    if (albumSongs.Any(song => _selectedGenreIds.Contains(song.GenreId)))
                    {
                        filteredItems.Add(album);
                    }
                }
                else if (item is Song song)
                {
                    if (_selectedGenreIds.Contains(song.GenreId))
                    {
                        filteredItems.Add(song);
                    }
                }
                else
                {
                    filteredItems.Add(item);
                }
            }

            return filteredItems;
        }

        public string GetFilterIndicatorText()
        {
            return _isFilterApplied && _selectedGenreIds.Count > 0
                ? $"🎚️({_selectedGenreIds.Count})"
                : "🎚️";
        }
    }
}