using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingSoftware.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBankBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "Companies",
                type: "TEXT",
                nullable: false,
                defaultValue: "Main Branch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "Companies");
        }
    }
}
