using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XepLichGiangDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class BoSungTrangThaiNguyenVong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ghi_chu_duyet",
                table: "nguyen_vong_giang_vien",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_duyet",
                table: "nguyen_vong_giang_vien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ngay_tao",
                table: "nguyen_vong_giang_vien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trang_thai",
                table: "nguyen_vong_giang_vien",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ghi_chu_duyet",
                table: "nguyen_vong_giang_vien");

            migrationBuilder.DropColumn(
                name: "ngay_duyet",
                table: "nguyen_vong_giang_vien");

            migrationBuilder.DropColumn(
                name: "ngay_tao",
                table: "nguyen_vong_giang_vien");

            migrationBuilder.DropColumn(
                name: "trang_thai",
                table: "nguyen_vong_giang_vien");
        }
    }
}
