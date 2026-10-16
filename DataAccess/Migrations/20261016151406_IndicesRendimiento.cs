using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IndicesRendimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SancionBlacklist_AdoptanteId",
                table: "SancionBlacklist");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_Estado",
                table: "Solicitudes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_Estado_UltimaActividad",
                table: "Solicitudes",
                columns: new[] { "Estado", "UltimaActividad" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_VoluntarioId",
                table: "Solicitudes",
                column: "VoluntarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SancionBlacklist_AdoptanteId_Revocada_Desde_Hasta",
                table: "SancionBlacklist",
                columns: new[] { "AdoptanteId", "Revocada", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_Canal_Rol",
                table: "Notificacion",
                columns: new[] { "Canal", "Rol" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacion_Canal_UsuarioId",
                table: "Notificacion",
                columns: new[] { "Canal", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mascotas_Estado",
                table: "Mascotas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Mascotas_Nombre",
                table: "Mascotas",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Incidente_VoluntarioId_Fecha",
                table: "Incidente",
                columns: new[] { "VoluntarioId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_EventoAuditoria_Fecha",
                table: "EventoAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_EventoAuditoria_UsuarioId_Fecha",
                table: "EventoAuditoria",
                columns: new[] { "UsuarioId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_Estado",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_Estado_UltimaActividad",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_VoluntarioId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_SancionBlacklist_AdoptanteId_Revocada_Desde_Hasta",
                table: "SancionBlacklist");

            migrationBuilder.DropIndex(
                name: "IX_Notificacion_Canal_Rol",
                table: "Notificacion");

            migrationBuilder.DropIndex(
                name: "IX_Notificacion_Canal_UsuarioId",
                table: "Notificacion");

            migrationBuilder.DropIndex(
                name: "IX_Mascotas_Estado",
                table: "Mascotas");

            migrationBuilder.DropIndex(
                name: "IX_Mascotas_Nombre",
                table: "Mascotas");

            migrationBuilder.DropIndex(
                name: "IX_Incidente_VoluntarioId_Fecha",
                table: "Incidente");

            migrationBuilder.DropIndex(
                name: "IX_EventoAuditoria_Fecha",
                table: "EventoAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_EventoAuditoria_UsuarioId_Fecha",
                table: "EventoAuditoria");

            migrationBuilder.CreateIndex(
                name: "IX_SancionBlacklist_AdoptanteId",
                table: "SancionBlacklist",
                column: "AdoptanteId");
        }
    }
}
