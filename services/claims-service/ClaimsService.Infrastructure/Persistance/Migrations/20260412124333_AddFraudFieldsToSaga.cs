using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimsService.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddFraudFieldsToSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ClaimProcessingSagaState");

            migrationBuilder.RenameColumn(
                name: "DocumentsDeadline",
                table: "ClaimProcessingSagaState",
                newName: "FraudEvaluatedAt");

            migrationBuilder.AddColumn<string>(
                name: "FraudReason",
                table: "ClaimProcessingSagaState",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FraudRiskScore",
                table: "ClaimProcessingSagaState",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFraudulent",
                table: "ClaimProcessingSagaState",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FraudReason",
                table: "ClaimProcessingSagaState");

            migrationBuilder.DropColumn(
                name: "FraudRiskScore",
                table: "ClaimProcessingSagaState");

            migrationBuilder.DropColumn(
                name: "IsFraudulent",
                table: "ClaimProcessingSagaState");

            migrationBuilder.RenameColumn(
                name: "FraudEvaluatedAt",
                table: "ClaimProcessingSagaState",
                newName: "DocumentsDeadline");

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ClaimProcessingSagaState",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
