using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeWorkItemFileIDToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change File_ID column type from int to nvarchar(500) in WorkItem table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkItem]') AND name = 'File_ID')
                BEGIN
                    -- Convert existing int values to string (comma-separated if needed)
                    -- First, convert existing single int values to string
                    UPDATE [WorkItem]
                    SET [File_ID] = CAST([File_ID] AS nvarchar(500))
                    WHERE [File_ID] IS NOT NULL;
                    
                    -- Then alter the column type
                    ALTER TABLE [WorkItem] ALTER COLUMN [File_ID] nvarchar(500) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert File_ID column type from nvarchar back to int
            // Note: This will fail if there are non-numeric values
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkItem]') AND name = 'File_ID')
                BEGIN
                    -- Try to convert string values back to int (only if they are single numeric values)
                    -- This will fail if there are comma-separated values
                    UPDATE [WorkItem]
                    SET [File_ID] = CASE 
                        WHEN ISNUMERIC([File_ID]) = 1 THEN CAST([File_ID] AS int)
                        ELSE NULL
                    END
                    WHERE [File_ID] IS NOT NULL;
                    
                    -- Then alter the column type back to int
                    ALTER TABLE [WorkItem] ALTER COLUMN [File_ID] int NULL;
                END
            ");
        }
    }
}

