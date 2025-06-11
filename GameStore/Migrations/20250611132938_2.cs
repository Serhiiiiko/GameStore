using Microsoft.EntityFrameworkCore.Migrations;

namespace GameStore.Migrations
{
    public partial class _2: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Проверяем, существует ли колонка
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name='Games' AND column_name='Platform'
                    ) THEN
                        ALTER TABLE ""Games"" ADD COLUMN ""Platform"" text NOT NULL DEFAULT 'Steam';
                    END IF;
                END $$;
            ");

            // Обновляем существующие записи
            migrationBuilder.Sql(@"
                UPDATE ""Games"" SET ""Platform"" = 'Steam' WHERE ""Platform"" IS NULL OR ""Platform"" = '';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Необязательно - удаление колонки при откате
            // migrationBuilder.DropColumn(name: "Platform", table: "Games");
        }
    }
}