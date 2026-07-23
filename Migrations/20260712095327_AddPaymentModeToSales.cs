using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModeToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "Sales");
        }
    }
}
