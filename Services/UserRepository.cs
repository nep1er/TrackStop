using System;
using System.Data.SqlClient;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class UserRepository
    {
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        // Создание нового пользователя
        public bool CreateUser(string login, string password, string role = "user")
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Проверяем, существует ли уже такой логин
            using var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE login = @login", conn);
            checkCmd.Parameters.AddWithValue("@login", login);
            var exists = (int)checkCmd.ExecuteScalar() > 0;

            if (exists)
                return false;

            // Создаем нового пользователя
            using var insertCmd = new SqlCommand(
                "INSERT INTO Users (login, password, Role) VALUES (@login, @password, @role)", conn);
            insertCmd.Parameters.AddWithValue("@login", login);
            insertCmd.Parameters.AddWithValue("@password", password);
            insertCmd.Parameters.AddWithValue("@role", role);

            return insertCmd.ExecuteNonQuery() > 0;
        }

        // Аутентификация пользователя
        public User Authenticate(string login, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT user_id, login, password, Role FROM Users WHERE login = @login AND password = @password", conn);
            cmd.Parameters.AddWithValue("@login", login);
            cmd.Parameters.AddWithValue("@password", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    Login = reader["login"].ToString(),
                    Password = reader["password"].ToString(),
                    Role = reader["Role"].ToString()
                };
            }

            return null;
        }

        // Получаем артиста, связанного с пользователем
        public Artist GetArtistByUserId(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT Id, Name, Bio, PhotoPath, Subscribers FROM Artist WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

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
                    UserId = userId, // Устанавливаем UserId
                    IsFavorite = false // Это сам исполнитель
                };
            }

            return null;
        }

        // Проверка существования логина
        public bool UserExists(string login)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE login = @login", conn);
            cmd.Parameters.AddWithValue("@login", login);

            return (int)cmd.ExecuteScalar() > 0;
        }

        // Создание исполнителя из пользователя
        public bool CreateArtistFromUser(int userId, string artistName, string bio, string photoPath)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Обновляем роль пользователя
                using var updateCmd = new SqlCommand(
                    "UPDATE Users SET Role = 'artist' WHERE user_id = @userId", conn, transaction);
                updateCmd.Parameters.AddWithValue("@userId", userId);
                updateCmd.ExecuteNonQuery();

                // Создаем артиста
                using var insertCmd = new SqlCommand(
                    "INSERT INTO Artist (Name, Bio, PhotoPath, Subscribers, UserId) VALUES (@name, @bio, @photoPath, 0, @userId)", conn, transaction);
                insertCmd.Parameters.AddWithValue("@name", artistName);
                insertCmd.Parameters.AddWithValue("@bio", bio ?? "");
                insertCmd.Parameters.AddWithValue("@photoPath", photoPath ?? "");
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT user_id, login, Role FROM Users ORDER BY user_id", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    Login = reader["login"].ToString(),
                    Role = reader["Role"].ToString()
                });
            }

            return users;
        }

        public List<User> SearchUsers(string searchText)
        {
            var users = new List<User>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT user_id, login, Role FROM Users WHERE login LIKE @search OR Role LIKE @search ORDER BY user_id", conn);
            cmd.Parameters.AddWithValue("@search", $"%{searchText}%");

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    Login = reader["login"].ToString(),
                    Role = reader["Role"].ToString()
                });
            }

            return users;
        }

        public bool DeleteUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Если пользователь является исполнителем, удаляем связанного артиста
                using var checkArtistCmd = new SqlCommand("SELECT Id FROM Artist WHERE UserId = @userId", conn, transaction);
                checkArtistCmd.Parameters.AddWithValue("@userId", userId);
                var artistId = checkArtistCmd.ExecuteScalar();

                if (artistId != null)
                {
                    // Удаляем из FavoriteArtists
                    using var deleteFavArtistsCmd = new SqlCommand("DELETE FROM FavoriteArtists WHERE ArtistId = @artistId", conn, transaction);
                    deleteFavArtistsCmd.Parameters.AddWithValue("@artistId", artistId);
                    deleteFavArtistsCmd.ExecuteNonQuery();

                    // Удаляем артиста
                    using var deleteArtistCmd = new SqlCommand("DELETE FROM Artist WHERE Id = @artistId", conn, transaction);
                    deleteArtistCmd.Parameters.AddWithValue("@artistId", artistId);
                    deleteArtistCmd.ExecuteNonQuery();
                }

                // Удаляем из избранных песен и альбомов
                using var deleteFavSongsCmd = new SqlCommand("DELETE FROM FavoriteSongs WHERE user_id = @userId", conn, transaction);
                deleteFavSongsCmd.Parameters.AddWithValue("@userId", userId);
                deleteFavSongsCmd.ExecuteNonQuery();

                using var deleteFavAlbumsCmd = new SqlCommand("DELETE FROM FavoriteAlbums WHERE user_id = @userId", conn, transaction);
                deleteFavAlbumsCmd.Parameters.AddWithValue("@userId", userId);
                deleteFavAlbumsCmd.ExecuteNonQuery();

                using var deleteFavArtistsUserCmd = new SqlCommand("DELETE FROM FavoriteArtists WHERE user_id = @userId", conn, transaction);
                deleteFavArtistsUserCmd.Parameters.AddWithValue("@userId", userId);
                deleteFavArtistsUserCmd.ExecuteNonQuery();

                // Удаляем пользователя
                using var deleteUserCmd = new SqlCommand("DELETE FROM Users WHERE user_id = @userId", conn, transaction);
                deleteUserCmd.Parameters.AddWithValue("@userId", userId);
                int result = deleteUserCmd.ExecuteNonQuery();

                transaction.Commit();
                return result > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public int GetUsersCount()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}