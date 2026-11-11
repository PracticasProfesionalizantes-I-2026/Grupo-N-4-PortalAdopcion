using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyControlVoluntario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcesosActivos",
                table: "Usuario",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Usuario",
                type: "BLOB",
                rowVersion: true,
                nullable: true);

            // Backfill RN-16: sin esto, los voluntarios con procesos ya asignados antes de esta migración
            // arrancarían con ProcesosActivos = 0 y el límite de 2 no se respetaría hasta que esos procesos terminen.
            migrationBuilder.Sql(
                @"UPDATE ""Usuario"" SET ""ProcesosActivos"" = (SELECT COUNT(*) FROM ""Solicitudes"" s WHERE s.""VoluntarioId"" = ""Usuario"".""Id"" AND s.""Estado"" IN ('EnEntrevista', 'EnPeriodoDePrueba')) WHERE ""Rol"" = 'Voluntario';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcesosActivos",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Usuario");
        }
    }
}
