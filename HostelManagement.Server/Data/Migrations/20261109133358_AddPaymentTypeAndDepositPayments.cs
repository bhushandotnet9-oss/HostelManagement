using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostelManagement.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeAndDepositPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DuePeriods_DuePeriodId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DuePeriodId",
                table: "Payments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ResidentId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Payments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill before the index and the FKs below, not after. Every
            // pre-existing payment is rent, so Type's 0 default already lands on
            // PaymentType.Rent, but ResidentId cannot be defaulted - it has to be
            // read back through the owning due period. This has to run while
            // ResidentId is still unconstrained: AddForeignKey makes SQLite
            // rebuild the table and copy the rows, and a copy of Guid.Empty
            // ResidentIds under a live FK to Residents would fail.
            migrationBuilder.Sql("""
                UPDATE Payments
                SET ResidentId = (
                    SELECT ResidentId FROM DuePeriods WHERE DuePeriods.Id = Payments.DuePeriodId
                )
                WHERE DuePeriodId IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ResidentId",
                table: "Payments",
                column: "ResidentId");

            // Note this FK comes back without ON DELETE CASCADE, which it had
            // before: a nullable FK defaults to no action instead. Nothing
            // regresses - the only place that deletes due periods is
            // ResidentsController.Delete, which refuses to run at all when the
            // resident has any payment, so no payment's due period is ever
            // deleted underneath it.
            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DuePeriods_DuePeriodId",
                table: "Payments",
                column: "DuePeriodId",
                principalTable: "DuePeriods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Residents_ResidentId",
                table: "Payments",
                column: "ResidentId",
                principalTable: "Residents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        // One-way door once a deposit payment exists: restoring DuePeriodId to
        // non-nullable cannot succeed while rows hold a null there, and silently
        // deleting deposit payments to make it fit would destroy records of money
        // taken. Roll back the code and leave the extra columns in place instead -
        // the old model simply ignores them. See design.md - Migration Plan.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DuePeriods_DuePeriodId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Residents_ResidentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ResidentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ResidentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DuePeriodId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DuePeriods_DuePeriodId",
                table: "Payments",
                column: "DuePeriodId",
                principalTable: "DuePeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
