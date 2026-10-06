using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adoptantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Dni = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adoptantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mascotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Especie = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Raza = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Edad = table.Column<int>(type: "INTEGER", nullable: false),
                    Sexo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mascotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MascotaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AdoptanteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Adoptantes_AdoptanteId",
                        column: x => x.AdoptanteId,
                        principalTable: "Adoptantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adoptantes_Dni",
                table: "Adoptantes",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adoptantes_Email",
                table: "Adoptantes",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mascotas_Estado",
                table: "Mascotas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_AdoptanteId_Estado",
                table: "Solicitudes",
                columns: new[] { "AdoptanteId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_MascotaId_Estado",
                table: "Solicitudes",
                columns: new[] { "MascotaId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "Adoptantes");

            migrationBuilder.DropTable(
                name: "Mascotas");
        }
    }
}
