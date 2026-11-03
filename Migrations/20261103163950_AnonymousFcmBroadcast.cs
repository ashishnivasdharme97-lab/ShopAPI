using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopAPI.Migrations
{
    /// <inheritdoc />
    public partial class AnonymousFcmBroadcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceTokenId",
                table: "NotificationOutboxMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "NotificationOutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxMessages_NotificationId_Topic",
                table: "NotificationOutboxMessages",
                columns: new[] { "NotificationId", "Topic" },
                unique: true,
                filter: "\"Topic\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationOutboxMessages_NotificationId_Topic",
                table: "NotificationOutboxMessages");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "NotificationOutboxMessages");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceTokenId",
                table: "NotificationOutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
