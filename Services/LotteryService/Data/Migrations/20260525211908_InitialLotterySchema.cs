using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotteryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLotterySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lottery");

            migrationBuilder.CreateTable(
                name: "Lottery",
                schema: "Lottery",
                columns: table => new
                {
                    LotteryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GiftId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    WonAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lottery", x => x.LotteryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lottery_CreatedAt",
                schema: "Lottery",
                table: "Lottery",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Lottery_GiftId",
                schema: "Lottery",
                table: "Lottery",
                column: "GiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Lottery_Status",
                schema: "Lottery",
                table: "Lottery",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Lottery_UserId",
                schema: "Lottery",
                table: "Lottery",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lottery",
                schema: "Lottery");
        }
    }
}
