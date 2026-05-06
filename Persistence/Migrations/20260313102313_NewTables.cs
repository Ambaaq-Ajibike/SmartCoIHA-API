using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FhirResourceStatus_FHIREndpoints_InstituteBaseUrlId",
                table: "FhirResourceStatus");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FhirResourceStatus",
                table: "FhirResourceStatus");

            migrationBuilder.RenameTable(
                name: "FhirResourceStatus",
                newName: "FhirResourceStatuses");

            migrationBuilder.RenameIndex(
                name: "IX_FhirResourceStatus_InstituteBaseUrlId",
                table: "FhirResourceStatuses",
                newName: "IX_FhirResourceStatuses_InstituteBaseUrlId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FhirResourceStatuses",
                table: "FhirResourceStatuses",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: true),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionManagers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstitutionManagers_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstitutionManagers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionManagers_InstitutionId",
                table: "InstitutionManagers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionManagers_UserId",
                table: "InstitutionManagers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FhirResourceStatuses_FHIREndpoints_InstituteBaseUrlId",
                table: "FhirResourceStatuses",
                column: "InstituteBaseUrlId",
                principalTable: "FHIREndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FhirResourceStatuses_FHIREndpoints_InstituteBaseUrlId",
                table: "FhirResourceStatuses");

            migrationBuilder.DropTable(
                name: "InstitutionManagers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FhirResourceStatuses",
                table: "FhirResourceStatuses");

            migrationBuilder.RenameTable(
                name: "FhirResourceStatuses",
                newName: "FhirResourceStatus");

            migrationBuilder.RenameIndex(
                name: "IX_FhirResourceStatuses_InstituteBaseUrlId",
                table: "FhirResourceStatus",
                newName: "IX_FhirResourceStatus_InstituteBaseUrlId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FhirResourceStatus",
                table: "FhirResourceStatus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FhirResourceStatus_FHIREndpoints_InstituteBaseUrlId",
                table: "FhirResourceStatus",
                column: "InstituteBaseUrlId",
                principalTable: "FHIREndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
