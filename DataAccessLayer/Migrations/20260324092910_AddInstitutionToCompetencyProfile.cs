using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionToCompetencyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "CompetencyProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyProfiles_InstitutionId",
                table: "CompetencyProfiles",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetencyProfiles_Institutions_InstitutionId",
                table: "CompetencyProfiles",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetencyProfiles_Institutions_InstitutionId",
                table: "CompetencyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompetencyProfiles_InstitutionId",
                table: "CompetencyProfiles");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "CompetencyProfiles");
        }
    }
}
