using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PolicyService.Infrastructure.Persistence.Migrationscls
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "policy_type_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaximumClaimAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AllowedVehicleTypes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_type_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    PolicyTypeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Vehicle_RegistrationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Vehicle_Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Vehicle_VehicleType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_policies_policy_type_definitions_PolicyTypeDefinitionId",
                        column: x => x.PolicyTypeDefinitionId,
                        principalTable: "policy_type_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicyTypeDefinitionCoverages",
                columns: table => new
                {
                    PolicyTypeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CoverageLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyTypeDefinitionCoverages", x => new { x.PolicyTypeDefinitionId, x.Id });
                    table.ForeignKey(
                        name: "FK_PolicyTypeDefinitionCoverages_policy_type_definitions_Polic~",
                        column: x => x.PolicyTypeDefinitionId,
                        principalTable: "policy_type_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_policies_PolicyTypeDefinitionId",
                table: "policies",
                column: "PolicyTypeDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policies");

            migrationBuilder.DropTable(
                name: "PolicyTypeDefinitionCoverages");

            migrationBuilder.DropTable(
                name: "policy_type_definitions");
        }
    }
}
