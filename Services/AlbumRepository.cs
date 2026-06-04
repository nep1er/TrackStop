using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class AlbumRepository
    {
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        // Получаем все альбомы с учетом статуса избранного для пользователя
        public List<Album> GetAllAlbums(int userId = -1)
        {
            var albums = new List<Album>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
SELECT al.Id, al.Title, al.CoverPath, ar.Name AS Artist,
       CASE WHEN fa.AlbumId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
FROM Album al
JOIN Artist ar ON al.ArtistId = ar.Id
LEFT JOIN FavoriteAlbums fa ON al.Id = fa.AlbumId AND fa.user_id = @userId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            albums.Add(new Album
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }

            return albums;
        }

        // Добавляем или удаляем из любимых с учетом user_id
        public void SetFavorite(int albumId, bool isFavorite, int userId = -1)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                if (isFavorite)
                {
                    using var cmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM FavoriteAlbums WHERE AlbumId=@albumId AND user_id=@userId) " +
                        "INSERT INTO FavoriteAlbums(AlbumId, user_id) VALUES(@albumId, @userId)", conn);
                    cmd.Parameters.AddWithValue("@albumId", albumId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new SqlCommand("DELETE FROM FavoriteAlbums WHERE AlbumId=@albumId AND user_id=@userId", conn);
                    cmd.Parameters.AddWithValue("@albumId", albumId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Получаем Id всех любимых альбомов для конкретного пользователя
        public List<int> GetFavoriteIds(int userId = -1)
        {
            var list = new List<int>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using var cmd = new SqlCommand("SELECT AlbumId FROM FavoriteAlbums WHERE user_id = @userId", conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(reader.GetInt32(0));
            }
            return list;
        }

        // Получаем избранные альбомы для конкретного пользователя
        public List<Album> GetFavoriteAlbums(int userId)
        {
            var albums = new List<Album>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
SELECT al.Id, al.Title, al.CoverPath, ar.Name AS Artist
FROM Album al
JOIN Artist ar ON al.ArtistId = ar.Id
JOIN FavoriteAlbums fa ON al.Id = fa.AlbumId
WHERE fa.user_id = @userId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            albums.Add(new Album
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Artist = reader["Artist"].ToString(),
                                CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                                IsFavorite = true // ← УСТАНАВЛИВАЕМ СРАЗУ ПРИ СОЗДАНИИ!
                            });
                        }
                    }
                }
            }

            return albums;
        }

        // Получаем альбомы артиста с учетом статуса избранного
        public List<Album> GetAlbumsByArtist(int artistId, int userId = -1)
        {
            var albums = new List<Album>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
SELECT a.Id, a.Title, a.CoverPath, ar.Name AS Artist,
       (SELECT COUNT(*) FROM Song WHERE AlbumId = a.Id) as SongCount,
       CASE WHEN fa.AlbumId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
FROM Album a
JOIN Artist ar ON a.ArtistId = ar.Id
LEFT JOIN FavoriteAlbums fa ON a.Id = fa.AlbumId AND fa.user_id = @userId
WHERE a.ArtistId = @ArtistId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ArtistId", artistId);
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                albums.Add(new Album
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                    SongCount = Convert.ToInt32(reader["SongCount"]),
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                });
            }

            return albums;
        }

        public void DeleteAlbum(int albumId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Сначала удаляем связанные песни
            using var deleteSongsCmd = new SqlCommand("DELETE FROM Song WHERE AlbumId = @albumId", conn);
            deleteSongsCmd.Parameters.AddWithValue("@albumId", albumId);
            deleteSongsCmd.ExecuteNonQuery();

            // Удаляем из избранного
            using var deleteFavCmd = new SqlCommand("DELETE FROM FavoriteAlbums WHERE AlbumId = @albumId", conn);
            deleteFavCmd.Parameters.AddWithValue("@albumId", albumId);
            deleteFavCmd.ExecuteNonQuery();

            // Удаляем альбом
            using var deleteCmd = new SqlCommand("DELETE FROM Album WHERE Id = @albumId", conn);
            deleteCmd.Parameters.AddWithValue("@albumId", albumId);
            deleteCmd.ExecuteNonQuery();
        }

        public int CreateAlbum(string title, string coverPath, int artistId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO Album (Title, CoverPath, ArtistId) OUTPUT INSERTED.Id VALUES (@title, @coverPath, @artistId)", conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@coverPath", coverPath);
            cmd.Parameters.AddWithValue("@artistId", artistId);

            return (int)cmd.ExecuteScalar();
        }

        // Обновление альбома
        public bool UpdateAlbum(int albumId, string title, string coverPath)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE Album SET Title = @title, CoverPath = @coverPath WHERE Id = @albumId", conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@coverPath", coverPath);
            cmd.Parameters.AddWithValue("@albumId", albumId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // Получение альбома по ID с учетом статуса избранного
        public Album GetAlbumById(int albumId, int userId = -1)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
SELECT a.Id, a.Title, a.CoverPath, ar.Name AS Artist, ar.Id AS ArtistId,
       CASE WHEN fa.AlbumId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
FROM Album a
JOIN Artist ar ON a.ArtistId = ar.Id
LEFT JOIN FavoriteAlbums fa ON a.Id = fa.AlbumId AND fa.user_id = @userId
WHERE a.Id = @albumId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@albumId", albumId);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Album
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                    ArtistId = Convert.ToInt32(reader["ArtistId"]),
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                };
            }

            return null;
        }

        // Поиск альбомов с учетом статуса избранного
        public List<Album> SearchAlbums(string searchText, int userId = -1)
        {
            var albums = new List<Album>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
SELECT al.Id, al.Title, al.CoverPath, ar.Name AS Artist,
       CASE WHEN fa.AlbumId IS NOT NULL THEN 1 ELSE 0 END AS IsFavorite
FROM Album al
JOIN Artist ar ON al.ArtistId = ar.Id
LEFT JOIN FavoriteAlbums fa ON al.Id = fa.AlbumId AND fa.user_id = @userId
WHERE al.Title LIKE @search OR ar.Name LIKE @search";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@search", $"%{searchText}%");
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                albums.Add(new Album
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    CoverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, reader["CoverPath"].ToString()),
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                });
            }

            return albums;
        }
        public int GetAlbumsCount()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Album", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Album GetAlbumByTitle(string title)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
        SELECT al.Id, al.Title, al.CoverPath, ar.Name AS Artist
        FROM Album al
        JOIN Artist ar ON al.ArtistId = ar.Id
        WHERE al.Title = @Title";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Title", title);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var coverPath = reader["CoverPath"]?.ToString();
                string fullCoverPath = "";

                if (!string.IsNullOrEmpty(coverPath))
                {
                    if (System.IO.Path.IsPathRooted(coverPath))
                    {
                        fullCoverPath = coverPath;
                    }
                    else
                    {
                        fullCoverPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, coverPath);
                    }
                }

                return new Album
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Title = reader["Title"].ToString(),
                    Artist = reader["Artist"].ToString(),
                    CoverPath = fullCoverPath
                };
            }

            return null;
        }
    }
}