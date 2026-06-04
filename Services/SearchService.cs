using System;
using System.Collections.Generic;
using System.Linq;
using TrackStop.Models;
using TrackStop.Services;

namespace TrackStop.Services
{
    public class SearchService
    {
        private readonly SongRepository _songRepo;
        private readonly AlbumRepository _albumRepo;
        private readonly GenreRepository _genreRepo;
        private readonly ArtistRepository _artistRepo;

        public SearchService(SongRepository songRepo, AlbumRepository albumRepo,
                            GenreRepository genreRepo, ArtistRepository artistRepo)
        {
            _songRepo = songRepo;
            _albumRepo = albumRepo;
            _genreRepo = genreRepo;
            _artistRepo = artistRepo;
        }

        public SearchResult Search(string query)
        {
            var result = new SearchResult();

            if (string.IsNullOrWhiteSpace(query))
                return result;

            var normalizedQuery = query.ToLowerInvariant().Trim();

            var allSongs = _songRepo.GetAllSongs();
            var allAlbums = _albumRepo.GetAllAlbums();
            var allGenres = _genreRepo.GetAllGenres();

            // поиск по жанрам
            var genres = allGenres
                .Where(g => g.Name.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           g.Name.ToLowerInvariant().Contains(normalizedQuery))
                .OrderBy(g => g.Name.ToLowerInvariant().StartsWith(normalizedQuery) ? 0 : 1)
                .ThenBy(g => g.Name.Length)
                .Take(5)
                .ToList();

            result.Genres = genres;

            // поиск по артистам
            try
            {
                var artists = _artistRepo.GetAllArtists()
                    .Where(a => a.Name.ToLowerInvariant().StartsWith(normalizedQuery) ||
                               a.Name.ToLowerInvariant().Contains(normalizedQuery))
                    .OrderBy(a => a.Name.ToLowerInvariant().StartsWith(normalizedQuery) ? 0 : 1)
                    .ThenBy(a => a.Name.Length)
                    .Take(10)
                    .ToList();

                result.Artists = artists.Select(a => a.Name).ToList();
            }
            catch
            {
                // Fallback
                result.Artists = allSongs
                    .Where(s => s.Artist.ToLowerInvariant().StartsWith(normalizedQuery) ||
                               s.Artist.ToLowerInvariant().Contains(normalizedQuery))
                    .Select(s => s.Artist)
                    .Distinct()
                    .OrderBy(a => a.ToLowerInvariant().StartsWith(normalizedQuery) ? 0 : 1)
                    .ThenBy(a => a.Length)
                    .Take(10)
                    .ToList();
            }

            // поиск по альбомам
            var albums = allAlbums
                .Where(a => a.Title.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           a.Artist.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           a.Title.ToLowerInvariant().Contains(normalizedQuery) ||
                           a.Artist.ToLowerInvariant().Contains(normalizedQuery))
                .OrderBy(a => a.Title.ToLowerInvariant().StartsWith(normalizedQuery) ? 0 :
                             a.Artist.ToLowerInvariant().StartsWith(normalizedQuery) ? 1 : 2)
                .ThenBy(a => a.Title.Length)
                .Take(15)
                .ToList();

            result.Albums = albums;

            //поиск по песням
            var songs = allSongs
                .Where(s => s.Title.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           s.Artist.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           s.Title.ToLowerInvariant().Contains(normalizedQuery) ||
                           s.Artist.ToLowerInvariant().Contains(normalizedQuery) ||
                           s.Genre.ToLowerInvariant().StartsWith(normalizedQuery) ||
                           s.Genre.ToLowerInvariant().Contains(normalizedQuery))
                .OrderBy(s => s.Title.ToLowerInvariant().StartsWith(normalizedQuery) ? 0 :
                             s.Artist.ToLowerInvariant().StartsWith(normalizedQuery) ? 1 :
                             s.Genre.ToLowerInvariant().StartsWith(normalizedQuery) ? 2 : 3)
                .ThenBy(s => s.Title.Length)
                .Take(20)
                .ToList();

            result.Songs = songs;

            return result;
        }
    }
}

public class SearchResult
{
    public List<Genre> Genres { get; set; } = new List<Genre>();
    public List<string> Artists { get; set; } = new List<string>();
    public List<Album> Albums { get; set; } = new List<Album>();
    public List<Song> Songs { get; set; } = new List<Song>();
}