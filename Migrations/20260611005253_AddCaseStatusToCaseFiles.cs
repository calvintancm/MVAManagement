using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVAManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseStatusToCaseFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DisbursementCategories",
                table: "DisbursementCategories");

            migrationBuilder.RenameTable(
                name: "DisbursementCategories",
                newName: "DisbursementCategory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisbursementCategory",
                table: "DisbursementCategory",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DisbursementCategory",
                table: "DisbursementCategory");

            migrationBuilder.RenameTable(
                name: "DisbursementCategory",
                newName: "DisbursementCategories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisbursementCategories",
                table: "DisbursementCategories",
                column: "Id");
        }
    }
}
