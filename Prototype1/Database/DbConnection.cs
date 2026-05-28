using System;
using MySql.Data.MySqlClient;

namespace Prototype1.Database
{
    public static class DbConnection
    {
        // ── Change these to match your MySQL setup ──
        private const string Server   = "localhost";
        private const string Port     = "3306";
        private const string Database = "idsms2";
        private const string User     = "root";
        private const string Password = "";  // no password

        public static string ConnectionString =>
            $"Server={Server};Port={Port};Database={Database};" +
            $"Uid={User};Pwd={Password};CharSet=utf8mb4;";

        /// <summary>Returns an open MySqlConnection. Caller must Dispose.</summary>
        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
