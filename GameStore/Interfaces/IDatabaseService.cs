using System.Data;

namespace GameStore.Interfaces
{
    public interface IDatabaseService
    {
        Task<List<string>> GetTableNamesAsync();
        Task<DataTable> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50);
        Task<int> GetTableRowCountAsync(string tableName);
        Task<List<string>> GetTableColumnsAsync(string tableName);
        Task<Dictionary<string, string>> GetDatabaseInfoAsync();
        Task<bool> ExecuteQueryAsync(string query);
        Task<DataTable> ExecuteSelectQueryAsync(string query);
    }
}