using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PayItOff.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpensePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpenseId = table.Column<int>(type: "integer", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpensePhotos_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePhotos_ExpenseId",
                table: "ExpensePhotos",
                column: "ExpenseId");


            migrationBuilder.Sql("INSERT INTO \"ExpensePhotos\" (\"ExpenseId\", \"PhotoUrl\", \"CreatedAt\") SELECT \"Id\", \"ReceiptImageUrl\", \"CreatedAt\" FROM \"Expenses\" WHERE \"ReceiptImageUrl\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ReceiptImageUrl",
                table: "Expenses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptImageUrl",
                table: "Expenses",
                type: "text",
                nullable: true);


            migrationBuilder.Sql("UPDATE \"Expenses\" e SET \"ReceiptImageUrl\" = (SELECT \"PhotoUrl\" FROM \"ExpensePhotos\" p WHERE p.\"ExpenseId\" = e.\"Id\" LIMIT 1);");

            migrationBuilder.DropTable(
                name: "ExpensePhotos");
        }
    }
}
