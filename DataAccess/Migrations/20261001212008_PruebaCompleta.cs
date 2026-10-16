using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DataAccess.Migrations
{
    /// <inheritdoc/>
    public partial class PruebaCompleta : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(name: "Adoptantes", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), NombreCompleto = table.Column<string>(type: "TEXT", nullable: false), Email = table.Column<string>(type: "TEXT", nullable: false), Dni = table.Column<string>(type: "TEXT", nullable: false), Telefono = table.Column<string>(type: "TEXT", nullable: false), Direccion = table.Column<string>(type: "TEXT", nullable: false), Edad = table.Column<int>(type: "INTEGER", nullable: false), EstadoCivil = table.Column<string>(type: "TEXT", nullable: false), DatosLaborales = table.Column<string>(type: "TEXT", nullable: false), FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false), Baja = table.Column<bool>(type: "INTEGER", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Adoptantes", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "EventoAuditoria", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), Accion = table.Column<string>(type: "TEXT", nullable: false), EntidadId = table.Column<Guid>(type: "TEXT", nullable: false), Motivo = table.Column<string>(type: "TEXT", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_EventoAuditoria", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "Mascotas", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), Nombre = table.Column<string>(type: "TEXT", nullable: false), Especie = table.Column<string>(type: "TEXT", nullable: false), Raza = table.Column<string>(type: "TEXT", nullable: true), Edad = table.Column<int>(type: "INTEGER", nullable: false), Sexo = table.Column<string>(type: "TEXT", nullable: false), FotoUrl = table.Column<string>(type: "TEXT", nullable: true), Chip = table.Column<string>(type: "TEXT", nullable: true), Estado = table.Column<string>(type: "TEXT", nullable: false), FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Mascotas", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "Notificacion", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true), Rol = table.Column<string>(type: "TEXT", nullable: true), Mensaje = table.Column<string>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), EntidadId = table.Column<Guid>(type: "TEXT", nullable: false), Canal = table.Column<string>(type: "TEXT", nullable: false), Destino = table.Column<string>(type: "TEXT", nullable: true), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Notificacion", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "SancionBlacklist", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), AdoptanteId = table.Column<Guid>(type: "TEXT", nullable: false), Motivo = table.Column<string>(type: "TEXT", nullable: false), Desde = table.Column<DateTime>(type: "TEXT", nullable: false), Hasta = table.Column<DateTime>(type: "TEXT", nullable: false), Revocada = table.Column<bool>(type: "INTEGER", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_SancionBlacklist", x => x.Id);
                table.ForeignKey(name: "FK_SancionBlacklist_Adoptantes_AdoptanteId", column: x => x.AdoptanteId, principalTable: "Adoptantes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "Usuario", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), Nombre = table.Column<string>(type: "TEXT", nullable: false), Email = table.Column<string>(type: "TEXT", nullable: false), Dni = table.Column<string>(type: "TEXT", nullable: false), PasswordHash = table.Column<string>(type: "TEXT", nullable: false), Rol = table.Column<string>(type: "TEXT", nullable: false), Estado = table.Column<string>(type: "TEXT", nullable: false), Matricula = table.Column<string>(type: "TEXT", nullable: true), AdoptanteId = table.Column<Guid>(type: "TEXT", nullable: true), FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Usuario", x => x.Id);
                table.ForeignKey(name: "FK_Usuario_Adoptantes_AdoptanteId", column: x => x.AdoptanteId, principalTable: "Adoptantes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "HistorialClinico", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), MascotaId = table.Column<Guid>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), VeterinarioId = table.Column<Guid>(type: "TEXT", nullable: false), Peso = table.Column<decimal>(type: "TEXT", nullable: false), EstadoGeneral = table.Column<string>(type: "TEXT", nullable: false), Vacunas = table.Column<string>(type: "TEXT", nullable: false), Antecedentes = table.Column<string>(type: "TEXT", nullable: false), Esterilizada = table.Column<bool>(type: "INTEGER", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_HistorialClinico", x => x.Id);
                table.ForeignKey(name: "FK_HistorialClinico_Mascotas_MascotaId", column: x => x.MascotaId, principalTable: "Mascotas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "Solicitudes", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), MascotaId = table.Column<Guid>(type: "TEXT", nullable: false), AdoptanteId = table.Column<Guid>(type: "TEXT", nullable: false), FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false), UltimaActividad = table.Column<DateTime>(type: "TEXT", nullable: false), Estado = table.Column<string>(type: "TEXT", nullable: false), Motivo = table.Column<string>(type: "TEXT", nullable: true), MotivoAdopcion = table.Column<string>(type: "TEXT", nullable: false), CodigoConfirmacion = table.Column<string>(type: "TEXT", nullable: false), EvaluadorId = table.Column<Guid>(type: "TEXT", nullable: true), EntidadAutorizante = table.Column<string>(type: "TEXT", nullable: true), FechaEvaluacion = table.Column<DateTime>(type: "TEXT", nullable: true), InicioPrueba = table.Column<DateTime>(type: "TEXT", nullable: true), VoluntarioId = table.Column<Guid>(type: "TEXT", nullable: true), Alerta = table.Column<bool>(type: "INTEGER", nullable: false), ValidacionVeterinaria = table.Column<bool>(type: "INTEGER", nullable: false), VeterinarioValidadorId = table.Column<Guid>(type: "TEXT", nullable: true), FechaFinalizacion = table.Column<DateTime>(type: "TEXT", nullable: true), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Solicitudes", x => x.Id);
                table.ForeignKey(name: "FK_Solicitudes_Adoptantes_AdoptanteId", column: x => x.AdoptanteId, principalTable: "Adoptantes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Solicitudes_Mascotas_MascotaId", column: x => x.MascotaId, principalTable: "Mascotas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "EventoMedico", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), HistorialClinicoId = table.Column<Guid>(type: "TEXT", nullable: false), VeterinarioId = table.Column<Guid>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), Tipo = table.Column<string>(type: "TEXT", nullable: false), Descripcion = table.Column<string>(type: "TEXT", nullable: false), Tratamiento = table.Column<string>(type: "TEXT", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_EventoMedico", x => x.Id);
                table.ForeignKey(name: "FK_EventoMedico_HistorialClinico_HistorialClinicoId", column: x => x.HistorialClinicoId, principalTable: "HistorialClinico", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "Entrevista", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), SolicitudId = table.Column<Guid>(type: "TEXT", nullable: false), VoluntarioId = table.Column<Guid>(type: "TEXT", nullable: false), FechaProgramada = table.Column<DateTime>(type: "TEXT", nullable: false), FechaRealizada = table.Column<DateTime>(type: "TEXT", nullable: true), Resultado = table.Column<string>(type: "TEXT", nullable: true), Observaciones = table.Column<string>(type: "TEXT", nullable: true), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Entrevista", x => x.Id);
                table.ForeignKey(name: "FK_Entrevista_Solicitudes_SolicitudId", column: x => x.SolicitudId, principalTable: "Solicitudes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "Incidente", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), SolicitudId = table.Column<Guid>(type: "TEXT", nullable: false), VoluntarioId = table.Column<Guid>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), Gravedad = table.Column<string>(type: "TEXT", nullable: false), Detalle = table.Column<string>(type: "TEXT", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Incidente", x => x.Id);
                table.ForeignKey(name: "FK_Incidente_Solicitudes_SolicitudId", column: x => x.SolicitudId, principalTable: "Solicitudes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateTable(name: "Seguimiento", columns: table => new { Id = table.Column<Guid>(type: "TEXT", nullable: false), SolicitudId = table.Column<Guid>(type: "TEXT", nullable: false), VoluntarioId = table.Column<Guid>(type: "TEXT", nullable: false), Fecha = table.Column<DateTime>(type: "TEXT", nullable: false), Animo = table.Column<string>(type: "TEXT", nullable: false), Alimentacion = table.Column<string>(type: "TEXT", nullable: false), Tareas = table.Column<string>(type: "TEXT", nullable: false), Medicacion = table.Column<string>(type: "TEXT", nullable: false), Limpieza = table.Column<string>(type: "TEXT", nullable: false), Alerta = table.Column<bool>(type: "INTEGER", nullable: false), Version = table.Column<long>(type: "INTEGER", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Seguimiento", x => x.Id);
                table.ForeignKey(name: "FK_Seguimiento_Solicitudes_SolicitudId", column: x => x.SolicitudId, principalTable: "Solicitudes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });
            migrationBuilder.CreateIndex(name: "IX_Adoptantes_Dni", table: "Adoptantes", column: "Dni", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Adoptantes_Email", table: "Adoptantes", column: "Email", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Entrevista_SolicitudId", table: "Entrevista", column: "SolicitudId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_EventoMedico_HistorialClinicoId", table: "EventoMedico", column: "HistorialClinicoId");
            migrationBuilder.CreateIndex(name: "IX_HistorialClinico_MascotaId", table: "HistorialClinico", column: "MascotaId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Incidente_SolicitudId", table: "Incidente", column: "SolicitudId");
            migrationBuilder.CreateIndex(name: "IX_Mascotas_Chip", table: "Mascotas", column: "Chip", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SancionBlacklist_AdoptanteId", table: "SancionBlacklist", column: "AdoptanteId");
            migrationBuilder.CreateIndex(name: "IX_Seguimiento_SolicitudId_Fecha", table: "Seguimiento", columns: new[] { "SolicitudId", "Fecha" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Solicitudes_AdoptanteId", table: "Solicitudes", column: "AdoptanteId", unique: true, filter: "Estado IN ('Pendiente','EnObservacion','Aprobada','EnEntrevista','EnPeriodoDePrueba')");
            migrationBuilder.CreateIndex(name: "IX_Solicitudes_MascotaId", table: "Solicitudes", column: "MascotaId", unique: true, filter: "Estado IN ('Pendiente','EnObservacion','Aprobada','EnEntrevista','EnPeriodoDePrueba')");
            migrationBuilder.CreateIndex(name: "IX_Usuario_AdoptanteId", table: "Usuario", column: "AdoptanteId");
            migrationBuilder.CreateIndex(name: "IX_Usuario_Dni", table: "Usuario", column: "Dni", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Usuario_Email", table: "Usuario", column: "Email", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Usuario_Matricula", table: "Usuario", column: "Matricula", unique: true);
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Entrevista");
            migrationBuilder.DropTable(name: "EventoAuditoria");
            migrationBuilder.DropTable(name: "EventoMedico");
            migrationBuilder.DropTable(name: "Incidente");
            migrationBuilder.DropTable(name: "Notificacion");
            migrationBuilder.DropTable(name: "SancionBlacklist");
            migrationBuilder.DropTable(name: "Seguimiento");
            migrationBuilder.DropTable(name: "Usuario");
            migrationBuilder.DropTable(name: "HistorialClinico");
            migrationBuilder.DropTable(name: "Solicitudes");
            migrationBuilder.DropTable(name: "Adoptantes");
            migrationBuilder.DropTable(name: "Mascotas");
        }
    }
}
