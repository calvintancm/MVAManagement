using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVAManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDisbursemengtCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
         name: "DisbursementCategory",
         columns: table => new
         {
             Id = table.Column<int>(nullable: false)
                 .Annotation("SqlServer:Identity", "1, 1"),
             CategoryName = table.Column<string>(maxLength: 100, nullable: false),
             Description = table.Column<string>(maxLength: 300, nullable: true),
             HexColor = table.Column<string>(maxLength: 10, nullable: false, defaultValue: "#94A3B8"),
             DisplayOrder = table.Column<int>(nullable: false, defaultValue: 0),
             IsActive = table.Column<bool>(nullable: false, defaultValue: true),
             CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
             UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
         },
         constraints: table =>
         {
             table.PrimaryKey("PK_DisbursementCategory", x => x.Id);
         });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DisbursementCategory");
        }
    }
}
