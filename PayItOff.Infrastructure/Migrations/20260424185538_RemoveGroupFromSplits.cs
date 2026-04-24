using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayItOff.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGroupFromSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""ExpenseSplits""
                SET ""ExpenseItemId"" = (
                    SELECT ""Id""
                    FROM ""ExpenseItems""
                    WHERE ""ExpenseGroupId"" = ""ExpenseSplits"".""ExpenseGroupId""
                    LIMIT 1
                )
                WHERE ""ExpenseItemId"" IS NULL AND ""ExpenseGroupId"" IS NOT NULL;
            ");
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseSplits_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseSplits");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseSplits_ExpenseItems_ExpenseItemId",
                table: "ExpenseSplits");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseSplits_ExpenseGroupId",
                table: "ExpenseSplits");

            migrationBuilder.DropColumn(
                name: "ExpenseGroupId",
                table: "ExpenseSplits");

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseItemId",
                table: "ExpenseSplits",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseItems",
                column: "ExpenseGroupId",
                principalTable: "ExpenseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseSplits_ExpenseItems_ExpenseItemId",
                table: "ExpenseSplits",
                column: "ExpenseItemId",
                principalTable: "ExpenseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseSplits_ExpenseItems_ExpenseItemId",
                table: "ExpenseSplits");

            migrationBuilder.AlterColumn<int>(
                name: "ExpenseItemId",
                table: "ExpenseSplits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ExpenseGroupId",
                table: "ExpenseSplits",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseSplits_ExpenseGroupId",
                table: "ExpenseSplits",
                column: "ExpenseGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseItems",
                column: "ExpenseGroupId",
                principalTable: "ExpenseGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseSplits_ExpenseGroups_ExpenseGroupId",
                table: "ExpenseSplits",
                column: "ExpenseGroupId",
                principalTable: "ExpenseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseSplits_ExpenseItems_ExpenseItemId",
                table: "ExpenseSplits",
                column: "ExpenseItemId",
                principalTable: "ExpenseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
