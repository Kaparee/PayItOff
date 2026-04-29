using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayItOff.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorToExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses");

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: null);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreatorId",
                table: "Expenses",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_CreatorId",
                table: "Expenses",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses",
                column: "PayerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_CreatorId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CreatorId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Expenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_PayerId",
                table: "Expenses",
                column: "PayerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
