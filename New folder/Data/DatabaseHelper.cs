using MySql.Data.MySqlClient;
using System.Data;

namespace HardwareStoreAPI.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private static DatabaseHelper? _instance;
        private static readonly object _lock = new object();

        private DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static void Initialize(string connectionString)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DatabaseHelper(connectionString);
                    }
                }
            }
        }

        public static DatabaseHelper Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("DatabaseHelper not initialized. Call Initialize() first in Program.cs");
                return _instance;
            }
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, MySqlParameter[]? parameters = null)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            using var adapter = new MySqlDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);
            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, MySqlParameter[]? parameters = null)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<object?> ExecuteScalarAsync(string query, MySqlParameter[]? parameters = null)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            return await command.ExecuteScalarAsync();
        }
    }
}