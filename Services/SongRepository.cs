using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class SongRepository
    {
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        public List<Song> GetAllSongs(int userId = -1)
        {
            var songs = new List<Song>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                           a.Name AS Artist, al.Title AS Album, al.CoverPath,
                           g.Name AS Genre,
                           CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                    FROM Song s
                    JOIN Artist a ON s.ArtistId = a.Id
                    JOIN Album al ON s.AlbumId = al.Id
                    LEFT JOIN Genre g ON s.GenreId = g.Id
                    LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                Album = reader["Album"].ToString(),
                                Duration = reader["Duration"].ToString(),
                                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                GenreId = reader["GenreId"] != DBNull.Value ? Convert.ToInt32(reader["GenreId"]) : 0,
                                Genre = reader["Genre"]?.ToString() ?? "Unknown",
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            return songs;
        }

        public List<Song> GetSongsByAlbum(int albumId, int userId = -1)
        {
            var songs = new List<Song>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                           a.Name AS Artist, al.Title AS Album, al.CoverPath,
                           g.Name AS Genre,
                           CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                    FROM Song s
                    JOIN Artist a ON s.ArtistId = a.Id
                    JOIN Album al ON s.AlbumId = al.Id
                    LEFT JOIN Genre g ON s.GenreId = g.Id
                    LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId
                    WHERE s.AlbumId = @AlbumId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AlbumId", albumId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                Album = reader["Album"].ToString(),
                                Duration = reader["Duration"].ToString(),
                                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                GenreId = reader["GenreId"] != DBNull.Value ? Convert.ToInt32(reader["GenreId"]) : 0,
                                Genre = reader["Genre"]?.ToString() ?? "Unknown",
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            return songs;
        }

        public List<Song> GetSongsByGenre(int genreId, int userId)
        {
            var songs = new List<Song>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                           a.Name AS Artist, al.Title AS Album, al.CoverPath,
                           g.Name AS Genre,
                           CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                    FROM Song s
                    JOIN Artist a ON s.ArtistId = a.Id
                    JOIN Album al ON s.AlbumId = al.Id
                    LEFT JOIN Genre g ON s.GenreId = g.Id
                    LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId
                    WHERE s.GenreId = @GenreId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@GenreId", genreId);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                Album = reader["Album"].ToString(),
                                Duration = reader["Duration"].ToString(),
                                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                GenreId = Convert.ToInt32(reader["GenreId"]),
                                Genre = reader["Genre"]?.ToString() ?? "Unknown",
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            return songs;
        }

        public int AddSong(string title, string filePath, int albumId, int artistId, int genreId = 1)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string duration = "00:00";

            using var cmd = new SqlCommand(
                @"INSERT INTO Song (Title, FilePath, AlbumId, ArtistId, GenreId, Duration) 
          OUTPUT INSERTED.Id VALUES (@title, @filePath, @albumId, @artistId, @genreId, @duration)", conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@filePath", filePath);
            cmd.Parameters.AddWithValue("@albumId", albumId);
            cmd.Parameters.AddWithValue("@artistId", artistId);
            cmd.Parameters.AddWithValue("@genreId", genreId);
            cmd.Parameters.AddWithValue("@duration", duration);

            return (int)cmd.ExecuteScalar();
        }

        // Обновление песни
        public bool UpdateSong(int songId, string title)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE Song SET Title = @title WHERE Id = @songId", conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@songId", songId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // Удаление песни
        public bool DeleteSong(int songId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Сначала удаляем из избранного
            using var deleteFavCmd = new SqlCommand("DELETE FROM FavoriteSongs WHERE SongId = @songId", conn);
            deleteFavCmd.Parameters.AddWithValue("@songId", songId);
            deleteFavCmd.ExecuteNonQuery();

            // Удаляем песню
            using var deleteCmd = new SqlCommand("DELETE FROM Song WHERE Id = @songId", conn);
            deleteCmd.Parameters.AddWithValue("@songId", songId);

            return deleteCmd.ExecuteNonQuery() > 0;
        }

        // Получение песни по ID с учетом статуса избранного
        public Song GetSongById(int songId, int userId = -1)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                       a.Name AS Artist, al.Title AS Album, al.Id AS AlbumId,
                       g.Name AS Genre,
                       CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                FROM Song s
                JOIN Artist a ON s.ArtistId = a.Id
                JOIN Album al ON s.AlbumId = al.Id
                LEFT JOIN Genre g ON s.GenreId = g.Id
                LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId
                WHERE s.Id = @songId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@songId", songId);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Song
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    Album = reader["Album"].ToString(),
                    AlbumId = Convert.ToInt32(reader["AlbumId"]),
                    Duration = reader["Duration"].ToString(),
                    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                    GenreId = reader["GenreId"] != DBNull.Value ? Convert.ToInt32(reader["GenreId"]) : 0,
                    Genre = reader["Genre"]?.ToString() ?? "Unknown",
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                };
            }

            return null;
        }

        public List<Song> SearchSongs(string searchText, int userId = -1)
        {
            var songs = new List<Song>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                       a.Name AS Artist, al.Title AS Album, al.CoverPath,
                       g.Name AS Genre,
                       CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                FROM Song s
                JOIN Artist a ON s.ArtistId = a.Id
                JOIN Album al ON s.AlbumId = al.Id
                LEFT JOIN Genre g ON s.GenreId = g.Id
                LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId
                WHERE s.Title LIKE @search OR a.Name LIKE @search OR al.Title LIKE @search";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@search", $"%{searchText}%");
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                songs.Add(new Song
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    Album = reader["Album"].ToString(),
                    Duration = reader["Duration"].ToString(),
                    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                    CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                    GenreId = reader["GenreId"] != DBNull.Value ? Convert.ToInt32(reader["GenreId"]) : 0,
                    Genre = reader["Genre"]?.ToString() ?? "Unknown",
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                });
            }

            return songs;
        }

        public int GetSongsCount()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Song", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Song> GetSongsByGenreIds(List<int> genreIds, int userId = -1)
        {
            var songs = new List<Song>();

            if (genreIds == null || genreIds.Count == 0)
                return GetAllSongs(userId);

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var paramNames = genreIds.Select((_, index) => $"@genreId{index}").ToArray();
                var inClause = string.Join(",", paramNames);

                string query = $@"
                    SELECT s.Id, s.Title, s.Duration, s.FilePath, s.GenreId,
                           a.Name AS Artist, al.Title AS Album, al.CoverPath,
                           g.Name AS Genre,
                           CASE WHEN fs.SongId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
                    FROM Song s
                    JOIN Artist a ON s.ArtistId = a.Id
                    JOIN Album al ON s.AlbumId = al.Id
                    LEFT JOIN Genre g ON s.GenreId = g.Id
                    LEFT JOIN FavoriteSongs fs ON s.Id = fs.SongId AND fs.user_id = @userId
                    WHERE s.GenreId IN ({inClause})";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    for (int i = 0; i < genreIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@genreId{i}", genreIds[i]);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            songs.Add(new Song
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                Album = reader["Album"].ToString(),
                                Duration = reader["Duration"].ToString(),
                                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["FilePath"].ToString()),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                GenreId = reader["GenreId"] != DBNull.Value ? Convert.ToInt32(reader["GenreId"]) : 0,
                                Genre = reader["Genre"]?.ToString() ?? "Unknown",
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            return songs;
        }

    }
}