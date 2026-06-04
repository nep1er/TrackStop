using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using TrackStop.Models;

namespace TrackStop.Services
{
    public class GenreRepository
    {
        private readonly string _connectionString =
            @"Server=localhost\SQLEXPRESS;Database=TruckStop;Trusted_Connection=True;";

        public List<Genre> GetAllGenres()
        {
            var genres = new List<Genre>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT Id, Name FROM Genre ORDER BY Name";

            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                genres.Add(new Genre
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString()
                });
            }

            return genres;
        }

        public Genre GetGenreById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT Id, Name FROM Genre WHERE Id = @Id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Genre
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString()
                };
            }

            return null;
        }

        public Genre GetGenreByName(string name)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT Id, Name FROM Genre WHERE Name = @Name";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Name", name);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Genre
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString()
                };
            }

            return null;
        }
    }
}