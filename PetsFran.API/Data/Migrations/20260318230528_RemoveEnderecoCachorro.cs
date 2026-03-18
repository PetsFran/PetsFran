using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetsFran.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEnderecoCachorro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnderecoCachorro",
                table: "Cachorros");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnderecoCachorro",
                table: "Cachorros",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
