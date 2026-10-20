using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aprovizionare.Api.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceDecisionNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "Invoices");
        }
    }
}
