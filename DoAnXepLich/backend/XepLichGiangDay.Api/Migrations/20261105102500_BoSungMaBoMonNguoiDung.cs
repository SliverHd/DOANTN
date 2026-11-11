using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XepLichGiangDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class BoSungMaBoMonNguoiDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ma_bo_mon",
                table: "nguoi_dung",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_nguoi_dung_ma_bo_mon",
                table: "nguoi_dung",
                column: "ma_bo_mon");

            migrationBuilder.AddForeignKey(
                name: "FK_nguoi_dung_bo_mon_ma_bo_mon",
                table: "nguoi_dung",
                column: "ma_bo_mon",
                principalTable: "bo_mon",
                principalColumn: "ma_bo_mon",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nguoi_dung_bo_mon_ma_bo_mon",
                table: "nguoi_dung");

            migrationBuilder.DropIndex(
                name: "IX_nguoi_dung_ma_bo_mon",
                table: "nguoi_dung");

            migrationBuilder.DropColumn(
                name: "ma_bo_mon",
                table: "nguoi_dung");
        }
    }
}
