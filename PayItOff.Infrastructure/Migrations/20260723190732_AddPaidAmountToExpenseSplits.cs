using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayItOff.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidAmountToExpenseSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "ExpenseSplits",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "ExpenseSplits");
        }
    }
}
