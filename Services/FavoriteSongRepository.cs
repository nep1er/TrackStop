using System.Collections.Generic;
using System.Data.SqlClient;

namespace TrackStop.Services
{
    public static class FavoriteSongRepository
    {
        private static readonly string _connStr =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        public static void SetFavorite(int songId, bool isFavorite, int userId)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            if (isFavorite)
            {
                using var cmd = new SqlCommand(
                    "IF NOT EXISTS(SELECT 1 FROM FavoriteSongs WHERE SongId=@songId AND user_id=@userId) " +
                    "INSERT INTO FavoriteSongs(SongId, user_id) VALUES(@songId, @userId)", conn);
                cmd.Parameters.AddWithValue("@songId", songId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new SqlCommand("DELETE FROM FavoriteSongs WHERE SongId=@songId AND user_id=@userId", conn);
                cmd.Parameters.AddWithValue("@songId", songId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<int> GetFavoriteIds(int userId)
        {
            var list = new List<int>();
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            using var cmd = new SqlCommand("SELECT SongId FROM FavoriteSongs WHERE user_id = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(reader.GetInt32(0));
            return list;
        }

        // для получения избранных песен с полной информацией
        public static List<Song> GetFavoriteSongs(int userId, SongRepository songRepo)
        {
            var favoriteIds = GetFavoriteIds(userId);
            var allSongs = songRepo.GetAllSongs();
            var favoriteSongs = new List<Song>();

            foreach (var song in allSongs)
            {
                if (favoriteIds.Contains(song.Id))
                {
                    song.IsFavorite = true;
                    favoriteSongs.Add(song);
                }
            }

            return favoriteSongs;
        }
    }
}