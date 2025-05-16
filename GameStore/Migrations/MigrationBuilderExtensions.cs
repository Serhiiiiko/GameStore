// Создайте файл Migrations/MigrationBuilderExtensions.cs
using Microsoft.EntityFrameworkCore.Migrations;

public static class MigrationBuilderExtensions
{
    public static bool IsColumnExists(this MigrationBuilder migrationBuilder, string tableName, string columnName)
    {
        // Так как нет прямого API для проверки существования колонки,
        // мы выполним проверку через SQL
        var checkColumnSql = $@"
            SELECT COUNT(1) 
            FROM information_schema.columns 
            WHERE table_name = '{tableName.ToLower()}' 
            AND column_name = '{columnName.ToLower()}'";

        var result = migrationBuilder.Sql(checkColumnSql);
        return result != null;
    }
}