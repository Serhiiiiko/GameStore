using GameStore.Data;
using GameStore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace GameStore.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DatabaseService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<string>> GetTableNamesAsync()
        {
            var tables = new List<string>();

            try
            {
                var query = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name";

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table names");
            }

            return tables;
        }

        public async Task<DataTable> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50)
        {
            var dataTable = new DataTable();

            try
            {
                // Проверяем, что имя таблицы безопасно
                if (!await IsValidTableNameAsync(tableName))
                {
                    throw new ArgumentException("Invalid table name");
                }

                var offset = (page - 1) * pageSize;
                var query = $@"SELECT * FROM ""{tableName}"" LIMIT {pageSize} OFFSET {offset}";

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var adapter = new NpgsqlDataAdapter(query, connection);
                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting data from table {tableName}");
            }

            return dataTable;
        }

        public async Task<int> GetTableRowCountAsync(string tableName)
        {
            try
            {
                if (!await IsValidTableNameAsync(tableName))
                {
                    throw new ArgumentException("Invalid table name");
                }

                var query = $@"SELECT COUNT(*) FROM ""{tableName}""";

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting row count for table {tableName}");
                return 0;
            }
        }

        public async Task<List<string>> GetTableColumnsAsync(string tableName)
        {
            var columns = new List<string>();

            try
            {
                if (!await IsValidTableNameAsync(tableName))
                {
                    throw new ArgumentException("Invalid table name");
                }

                var query = @"
                    SELECT column_name, data_type 
                    FROM information_schema.columns 
                    WHERE table_name = @tableName 
                    AND table_schema = 'public'
                    ORDER BY ordinal_position";

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@tableName", tableName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(0);
                    var dataType = reader.GetString(1);
                    columns.Add($"{columnName} ({dataType})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting columns for table {tableName}");
            }

            return columns;
        }

        public async Task<Dictionary<string, string>> GetDatabaseInfoAsync()
        {
            var info = new Dictionary<string, string>();

            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                // Версия PostgreSQL
                using (var command = new NpgsqlCommand("SELECT version()", connection))
                {
                    var version = await command.ExecuteScalarAsync();
                    info["PostgreSQL Version"] = version?.ToString() ?? "Unknown";
                }

                // Размер базы данных
                using (var command = new NpgsqlCommand(@"
                    SELECT pg_size_pretty(pg_database_size(current_database()))", connection))
                {
                    var size = await command.ExecuteScalarAsync();
                    info["Database Size"] = size?.ToString() ?? "Unknown";
                }

                // Количество таблиц
                using (var command = new NpgsqlCommand(@"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_type = 'BASE TABLE'", connection))
                {
                    var count = await command.ExecuteScalarAsync();
                    info["Total Tables"] = count?.ToString() ?? "0";
                }

                // Текущая база данных
                info["Database Name"] = connection.Database;

                // Сервер
                info["Server"] = connection.DataSource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database info");
                info["Error"] = ex.Message;
            }

            return info;
        }

        public async Task<bool> ExecuteQueryAsync(string query)
        {
            try
            {
                // Простая проверка на опасные операции
                var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER" };
                var upperQuery = query.ToUpper();

                if (dangerousKeywords.Any(keyword => upperQuery.Contains(keyword)))
                {
                    _logger.LogWarning($"Dangerous query blocked: {query}");
                    return false;
                }

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query: {query}");
                return false;
            }
        }

        public async Task<DataTable> ExecuteSelectQueryAsync(string query)
        {
            var dataTable = new DataTable();

            try
            {
                // Проверяем, что это SELECT запрос
                if (!query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Only SELECT queries are allowed");
                }

                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var adapter = new NpgsqlDataAdapter(query, connection);
                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing select query: {query}");
            }

            return dataTable;
        }

        private async Task<bool> IsValidTableNameAsync(string tableName)
        {
            var validTables = await GetTableNamesAsync();
            return validTables.Contains(tableName);
        }
    }
}