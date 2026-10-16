using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ValidacionesIdentidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuario_Dni_Numerico",
                table: "Usuario",
                sql: "trim(\"Dni\") <> '' AND \"Dni\" NOT GLOB '*[^0-9]*'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Adoptantes_Dni_Numerico",
                table: "Adoptantes",
                sql: "trim(\"Dni\") <> '' AND \"Dni\" NOT GLOB '*[^0-9]*'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Adoptantes_EstadoCivil_Valido",
                table: "Adoptantes",
                sql: "\"EstadoCivil\" IN ('Soltero/a','Casado/a','Divorciado/a','Viudo/a','Conviviente/Unión de hecho','Separado/a')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Adoptantes_Telefono_Numerico",
                table: "Adoptantes",
                sql: "trim(\"Telefono\") <> '' AND \"Telefono\" NOT GLOB '*[^0-9]*'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuario_Dni_Numerico",
                table: "Usuario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Adoptantes_Dni_Numerico",
                table: "Adoptantes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Adoptantes_EstadoCivil_Valido",
                table: "Adoptantes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Adoptantes_Telefono_Numerico",
                table: "Adoptantes");
        }
    }
}
