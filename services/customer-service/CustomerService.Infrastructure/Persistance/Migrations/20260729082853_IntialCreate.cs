using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    ContactInformation_Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactInformation_Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrimaryAddress_Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryAddress_Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrimaryAddress_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryAddress_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryAddress_PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrimaryAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreferredCommunication = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_LastName",
                table: "customers",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Status",
                table: "customers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
