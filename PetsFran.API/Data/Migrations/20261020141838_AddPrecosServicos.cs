using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetsFran.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecosServicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrecosServicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PetShopId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Porte = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Raca = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecosServicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecosServicos_PetShops_PetShopId",
                        column: x => x.PetShopId,
                        principalTable: "PetShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrecosServicos_PetShopId_Tipo_Porte_Raca_Nome",
                table: "PrecosServicos",
                columns: new[] { "PetShopId", "Tipo", "Porte", "Raca", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrecosServicos");
        }
    }
}
