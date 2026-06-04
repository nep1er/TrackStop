using System;
using System.Collections.Generic;
using System.Linq;
using TrackStop.Models;
using TrackStop.Models.Search;
using TrackStop.Services;

namespace TrackStop.Managers
{
    public class SearchManager
    {
        private readonly SearchService _searchService;
        private readonly AlbumRepository _albumRepo;
        private readonly ArtistRepository _artistRepo;
        private readonly NavigationService _navigationService;

        public SearchManager(
            SearchService searchService,
            AlbumRepository albumRepo,
            ArtistRepository artistRepo,
            NavigationService navigationService)
        {
            _searchService = searchService;
            _albumRepo = albumRepo;
            _artistRepo = artistRepo;
            _navigationService = navigationService;
        }

        public List<object> GetSearchResults(string query, FilterManager filterManager = null)
        {
            if (string.IsNullOrEmpty(query))
                return new List<object>();

            var results = _searchService.Search(query);
            return FormatSearchResults(results, filterManager);
        }

        private List<object> FormatSearchResults(SearchResult results, FilterManager filterManager)
        {
            var combinedItems = new List<object>();

            bool hasGenres = results.Genres.Count > 0;
            bool hasArtists = results.Artists.Count > 0;
            bool hasAlbums = results.Albums.Count > 0;
            bool hasSongs = results.Songs.Count > 0;

            // 1. Жанры
            if (hasGenres)
            {
                combinedItems.Add(new SectionHeaderItem { Title = "Жанры" });
                foreach (var genre in results.Genres)
                {
                    combinedItems.Add(new GenreSearchItem { Genre = genre, DisplayName = genre.Name });
                }
            }

            // 2. Артисты
            if (hasArtists)
            {
                if (hasGenres) combinedItems.Add(new SeparatorItem());
                combinedItems.Add(new SectionHeaderItem { Title = "Исполнители" });

                var favArtistIds = _artistRepo.GetFavoriteIds(App.CurrentUserId);
                foreach (var artistName in results.Artists)
                {
                    var artist = _artistRepo.GetArtistByName(artistName);
                    if (artist != null)
                    {
                        artist.IsFavorite = favArtistIds.Contains(artist.Id);
                        FixArtistPhotoPath(artist);
                        combinedItems.Add(artist);
                    }
                }
            }

            // 3. Альбомы
            if (hasAlbums)
            {
                if (hasGenres || hasArtists) combinedItems.Add(new SeparatorItem());
                combinedItems.Add(new SectionHeaderItem { Title = "Альбомы" });

                var favAlbumIds = _albumRepo.GetFavoriteIds(App.CurrentUserId);
                foreach (var searchAlbum in results.Albums)
                {
                    var existingAlbum = _navigationService.AllAlbums.FirstOrDefault(a => a.Id == searchAlbum.Id);

                    if (existingAlbum != null)
                    {
                        existingAlbum.IsFavorite = favAlbumIds.Contains(searchAlbum.Id);
                        combinedItems.Add(existingAlbum);
                    }
                    else
                    {
                        searchAlbum.IsFavorite = favAlbumIds.Contains(searchAlbum.Id);
                        combinedItems.Add(searchAlbum);
                        _navigationService.AllAlbums.Add(searchAlbum);
                    }
                }
            }

            // 4. Песни
            if (hasSongs)
            {
                if (hasGenres || hasArtists || hasAlbums) combinedItems.Add(new SeparatorItem());
                combinedItems.Add(new SectionHeaderItem { Title = "Песни" });

                var favSongIds = FavoriteSongRepository.GetFavoriteIds(App.CurrentUserId);
                foreach (var searchSong in results.Songs)
                {
                    var existingSong = _navigationService.AllSongs.FirstOrDefault(s => s.Id == searchSong.Id);

                    if (existingSong != null)
                    {
                        existingSong.IsFavorite = favSongIds.Contains(searchSong.Id);
                        combinedItems.Add(existingSong);
                    }
                    else
                    {
                        searchSong.IsFavorite = favSongIds.Contains(searchSong.Id);
                        combinedItems.Add(searchSong);
                        _navigationService.AllSongs.Add(searchSong);
                    }
                }
            }

            // Если ничего не найдено
            if (combinedItems.Count == 0)
            {
                combinedItems.Add(new NoResultsItem { Message = "Ничего не найдено" });
            }

            // Применяем фильтры
            if (filterManager != null && filterManager.IsFilterApplied && filterManager.SelectedGenreIds.Count > 0)
            {
                return filterManager.ApplyFilterToSearchResults(combinedItems);
            }

            return combinedItems;
        }

        private void FixArtistPhotoPath(Artist artist)
        {
            if (!string.IsNullOrEmpty(artist.PhotoPath) && !System.IO.Path.IsPathRooted(artist.PhotoPath))
            {
                artist.PhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, artist.PhotoPath);
            }
        }

        public List<Song> GetSearchSongs(string query)
        {
            var results = _searchService.Search(query);
            return results.Songs;
        }

        public int FindSongIndex(List<Song> songs, Song targetSong)
        {
            return songs.FindIndex(s => s.Id == targetSong.Id);
        }
    }
}