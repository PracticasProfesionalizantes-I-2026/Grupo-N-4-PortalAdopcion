using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TraspasoClinico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistorialClinico_MascotaId",
                table: "HistorialClinico");

            migrationBuilder.AlterColumn<Guid>(
                name: "MascotaId",
                table: "HistorialClinico",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "AdoptanteId",
                table: "HistorialClinico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrigenHistorialId",
                table: "HistorialClinico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrigenMascotaId",
                table: "HistorialClinico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_AdoptanteId",
                table: "HistorialClinico",
                column: "AdoptanteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_MascotaId",
                table: "HistorialClinico",
                column: "MascotaId",
                unique: true,
                filter: "MascotaId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_OrigenMascotaId",
                table: "HistorialClinico",
                column: "OrigenMascotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistorialClinico_AdoptanteId",
                table: "HistorialClinico");

            migrationBuilder.DropIndex(
                name: "IX_HistorialClinico_MascotaId",
                table: "HistorialClinico");

            migrationBuilder.DropIndex(
                name: "IX_HistorialClinico_OrigenMascotaId",
                table: "HistorialClinico");

            migrationBuilder.DropColumn(
                name: "AdoptanteId",
                table: "HistorialClinico");

            migrationBuilder.DropColumn(
                name: "OrigenHistorialId",
                table: "HistorialClinico");

            migrationBuilder.DropColumn(
                name: "OrigenMascotaId",
                table: "HistorialClinico");

            migrationBuilder.AlterColumn<Guid>(
                name: "MascotaId",
                table: "HistorialClinico",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialClinico_MascotaId",
                table: "HistorialClinico",
                column: "MascotaId",
                unique: true);
        }
    }
}
