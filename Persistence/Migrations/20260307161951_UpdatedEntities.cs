using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FHIREndpoints_Institutions_InstitutionId",
                table: "FHIREndpoints");

            migrationBuilder.DropColumn(
                name: "SupportedResources",
                table: "FHIREndpoints");

            migrationBuilder.RenameColumn(
                name: "InstitutionId",
                table: "FHIREndpoints",
                newName: "InstitutionID");

            migrationBuilder.RenameIndex(
                name: "IX_FHIREndpoints_InstitutionId",
                table: "FHIREndpoints",
                newName: "IX_FHIREndpoints_InstitutionID");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "DataRequests",
                newName: "InstitutePatientId");

            migrationBuilder.AddColumn<string>(
                name: "InstitutePatientId",
                table: "Patients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "InstitutionID",
                table: "FHIREndpoints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestingInstitutionId",
                table: "DataRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "FhirResourceStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceName = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    InsituteBaseUrlId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstituteBaseUrlId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FhirResourceStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FhirResourceStatus_FHIREndpoints_InstituteBaseUrlId",
                        column: x => x.InstituteBaseUrlId,
                        principalTable: "FHIREndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FhirResourceStatus_InstituteBaseUrlId",
                table: "FhirResourceStatus",
                column: "InstituteBaseUrlId");

            migrationBuilder.AddForeignKey(
                name: "FK_FHIREndpoints_Institutions_InstitutionID",
                table: "FHIREndpoints",
                column: "InstitutionID",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FHIREndpoints_Institutions_InstitutionID",
                table: "FHIREndpoints");

            migrationBuilder.DropTable(
                name: "FhirResourceStatus");

            migrationBuilder.DropColumn(
                name: "InstitutePatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "RequestingInstitutionId",
                table: "DataRequests");

            migrationBuilder.RenameColumn(
                name: "InstitutionID",
                table: "FHIREndpoints",
                newName: "InstitutionId");

            migrationBuilder.RenameIndex(
                name: "IX_FHIREndpoints_InstitutionID",
                table: "FHIREndpoints",
                newName: "IX_FHIREndpoints_InstitutionId");

            migrationBuilder.RenameColumn(
                name: "InstitutePatientId",
                table: "DataRequests",
                newName: "PatientId");

            migrationBuilder.AlterColumn<Guid>(
                name: "InstitutionId",
                table: "FHIREndpoints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<List<string>>(
                name: "SupportedResources",
                table: "FHIREndpoints",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_FHIREndpoints_Institutions_InstitutionId",
                table: "FHIREndpoints",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }
    }
}
