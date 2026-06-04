using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class ArtistRepository
    {
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        public void SetFavorite(int artistId, bool isFavorite, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                if (isFavorite)
                {
                    // Добавляем в избранное
                    using var insertCmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM FavoriteArtists WHERE ArtistId=@artistId AND user_id=@userId) " +
                        "INSERT INTO FavoriteArtists(ArtistId, user_id) VALUES(@artistId, @userId)", conn, transaction);
                    insertCmd.Parameters.AddWithValue("@artistId", artistId);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.ExecuteNonQuery();

                    // Увеличиваем счетчик подписчиков
                    using var updateCmd = new SqlCommand(
                        "UPDATE Artist SET Subscribers = Subscribers + 1 WHERE Id = @artistId", conn, transaction);
                    updateCmd.Parameters.AddWithValue("@artistId", artistId);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // Удаляем из избранного
                    using var deleteCmd = new SqlCommand(
                        "DELETE FROM FavoriteArtists WHERE ArtistId=@artistId AND user_id=@userId", conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@artistId", artistId);
                    deleteCmd.Parameters.AddWithValue("@userId", userId);
                    deleteCmd.ExecuteNonQuery();

                    // Уменьшаем счетчик подписчиков (но не ниже 0)
                    using var updateCmd = new SqlCommand(
                        "UPDATE Artist SET Subscribers = CASE WHEN Subscribers > 0 THEN Subscribers - 1 ELSE 0 END WHERE Id = @artistId", conn, transaction);
                    updateCmd.Parameters.AddWithValue("@artistId", artistId);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Artist> GetAllArtists()
        {
            var artists = new List<Artist>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT Id, Name, Bio, PhotoPath, Subscribers FROM Artist ORDER BY Name";

            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var photoPath = reader["PhotoPath"]?.ToString();
                string fullPhotoPath = "";

                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (System.IO.Path.IsPathRooted(photoPath))
                    {
                        fullPhotoPath = photoPath;
                    }
                    else
                    {
                        fullPhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                    }
                }

                artists.Add(new Artist
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    PhotoPath = fullPhotoPath,
                    Subscribers = Convert.ToInt32(reader["Subscribers"])
                });
            }

            return artists;
        }

        // Получаем всех избранных исполнителей для конкретного пользователя
        public List<Artist> GetFavoriteArtists(int userId)
        {
            var artists = new List<Artist>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT a.Id, a.Name, a.Bio, a.PhotoPath, a.Subscribers 
                FROM Artist a
                JOIN FavoriteArtists fa ON a.Id = fa.ArtistId
                WHERE fa.user_id = @userId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var photoPath = reader["PhotoPath"]?.ToString();
                string fullPhotoPath = "";

                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (System.IO.Path.IsPathRooted(photoPath))
                    {
                        fullPhotoPath = photoPath;
                    }
                    else
                    {
                        fullPhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                    }
                }

                artists.Add(new Artist
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    PhotoPath = fullPhotoPath,
                    Subscribers = Convert.ToInt32(reader["Subscribers"]),
                    IsFavorite = true
                });
            }

            return artists;
        }

        // Получаем ID всех избранных исполнителей для конкретного пользователя
        public List<int> GetFavoriteIds(int userId)
        {
            var list = new List<int>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT ArtistId FROM FavoriteArtists WHERE user_id = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetInt32(0));
            return list;
        }

        public bool IsFavorite(int artistId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT 1 FROM FavoriteArtists WHERE ArtistId=@artistId AND user_id=@userId", conn);
            cmd.Parameters.AddWithValue("@artistId", artistId);
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = cmd.ExecuteReader();
            return reader.HasRows;
        }


        public Artist GetArtistById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT Id, Name, Bio, PhotoPath, Subscribers 
                FROM Artist 
                WHERE Id = @Id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Artist
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    PhotoPath = reader["PhotoPath"]?.ToString(),
                    Subscribers = Convert.ToInt32(reader["Subscribers"])
                };
            }

            return null;
        }

        public Artist GetArtistByName(string name)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
        SELECT Id, Name, Bio, PhotoPath, Subscribers, UserId 
        FROM Artist 
        WHERE Name = @Name";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var photoPath = reader["PhotoPath"]?.ToString();
                string fullPhotoPath = "";

                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (System.IO.Path.IsPathRooted(photoPath))
                    {
                        fullPhotoPath = photoPath;
                    }
                    else
                    {
                        fullPhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                    }
                }

                return new Artist
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    PhotoPath = fullPhotoPath,
                    Subscribers = Convert.ToInt32(reader["Subscribers"]),
                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null
                };
            }

            return null;
        }

        public bool UpdateArtist(Artist artist)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE Artist SET Name = @name, Bio = @bio, PhotoPath = @photoPath WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@name", artist.Name);
            cmd.Parameters.AddWithValue("@bio", artist.Bio ?? "");
            cmd.Parameters.AddWithValue("@photoPath", artist.PhotoPath ?? "");
            cmd.Parameters.AddWithValue("@id", artist.Id);

            return cmd.ExecuteNonQuery() > 0;
        }

        // для получения с UserId
        public Artist GetArtistByIdWithUser(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
        SELECT Id, Name, Bio, PhotoPath, UserId, Subscribers
        FROM Artist 
        WHERE UserId = @UserId";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var photoPath = reader["PhotoPath"]?.ToString();
                string fullPhotoPath = "";

                if (!string.IsNullOrEmpty(photoPath))
                {
                    if (System.IO.Path.IsPathRooted(photoPath))
                    {
                        fullPhotoPath = photoPath;
                    }
                    else
                    {
                        fullPhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoPath);
                    }
                }

                return new Artist
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    PhotoPath = fullPhotoPath,
                    UserId = Convert.ToInt32(reader["UserId"]),
                    Subscribers = Convert.ToInt32(reader["Subscribers"])
                };
            }

            return null;
        }

        public int GetArtistsCount()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Artist", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}