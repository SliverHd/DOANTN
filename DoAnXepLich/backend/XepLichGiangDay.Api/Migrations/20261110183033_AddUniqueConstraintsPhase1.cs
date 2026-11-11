using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XepLichGiangDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Don dep cac ban ghi trung lap (neu co) truoc khi tao Unique Index
            migrationBuilder.Sql(@"
                ;WITH DupRangBuoc AS (
                    SELECT ma_rang_buoc,
                           ROW_NUMBER() OVER (PARTITION BY ma_hoc_ky, ten_rang_buoc ORDER BY ma_rang_buoc DESC) AS rn
                    FROM rang_buoc_hoc_ky
                )
                DELETE FROM DupRangBuoc WHERE rn > 1;

                ;WITH DupNangLuc AS (
                    SELECT ma_nang_luc,
                           ROW_NUMBER() OVER (PARTITION BY ma_giang_vien, ma_hoc_phan ORDER BY ma_nang_luc DESC) AS rn
                    FROM nang_luc_giang_day
                )
                DELETE FROM DupNangLuc WHERE rn > 1;

                ;WITH DupDinhMuc AS (
                    SELECT ma_dinh_muc,
                           ROW_NUMBER() OVER (PARTITION BY ma_giang_vien, ma_hoc_ky ORDER BY ma_dinh_muc DESC) AS rn
                    FROM dinh_muc_giang_vien
                )
                DELETE FROM DupDinhMuc WHERE rn > 1;
            ");

            migrationBuilder.DropIndex(
                name: "IX_rang_buoc_hoc_ky_ma_hoc_ky",
                table: "rang_buoc_hoc_ky");

            migrationBuilder.DropIndex(
                name: "IX_nang_luc_giang_day_ma_giang_vien",
                table: "nang_luc_giang_day");

            migrationBuilder.DropIndex(
                name: "IX_lop_hoc_phan_ma_hoc_ky",
                table: "lop_hoc_phan");

            migrationBuilder.DropIndex(
                name: "IX_dinh_muc_giang_vien_ma_giang_vien",
                table: "dinh_muc_giang_vien");

            migrationBuilder.AlterColumn<string>(
                name: "ten_rang_buoc",
                table: "rang_buoc_hoc_ky",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ma_lop_hoc_phan_truong",
                table: "lop_hoc_phan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_rang_buoc_hoc_ky_ma_hoc_ky_ten_rang_buoc",
                table: "rang_buoc_hoc_ky",
                columns: new[] { "ma_hoc_ky", "ten_rang_buoc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nang_luc_giang_day_ma_giang_vien_ma_hoc_phan",
                table: "nang_luc_giang_day",
                columns: new[] { "ma_giang_vien", "ma_hoc_phan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lop_hoc_phan_ma_hoc_ky_ma_lop_hoc_phan_truong",
                table: "lop_hoc_phan",
                columns: new[] { "ma_hoc_ky", "ma_lop_hoc_phan_truong" },
                unique: true,
                filter: "[ma_lop_hoc_phan_truong] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_dinh_muc_giang_vien_ma_giang_vien_ma_hoc_ky",
                table: "dinh_muc_giang_vien",
                columns: new[] { "ma_giang_vien", "ma_hoc_ky" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rang_buoc_hoc_ky_ma_hoc_ky_ten_rang_buoc",
                table: "rang_buoc_hoc_ky");

            migrationBuilder.DropIndex(
                name: "IX_nang_luc_giang_day_ma_giang_vien_ma_hoc_phan",
                table: "nang_luc_giang_day");

            migrationBuilder.DropIndex(
                name: "IX_lop_hoc_phan_ma_hoc_ky_ma_lop_hoc_phan_truong",
                table: "lop_hoc_phan");

            migrationBuilder.DropIndex(
                name: "IX_dinh_muc_giang_vien_ma_giang_vien_ma_hoc_ky",
                table: "dinh_muc_giang_vien");

            migrationBuilder.AlterColumn<string>(
                name: "ten_rang_buoc",
                table: "rang_buoc_hoc_ky",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ma_lop_hoc_phan_truong",
                table: "lop_hoc_phan",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_rang_buoc_hoc_ky_ma_hoc_ky",
                table: "rang_buoc_hoc_ky",
                column: "ma_hoc_ky");

            migrationBuilder.CreateIndex(
                name: "IX_nang_luc_giang_day_ma_giang_vien",
                table: "nang_luc_giang_day",
                column: "ma_giang_vien");

            migrationBuilder.CreateIndex(
                name: "IX_lop_hoc_phan_ma_hoc_ky",
                table: "lop_hoc_phan",
                column: "ma_hoc_ky");

            migrationBuilder.CreateIndex(
                name: "IX_dinh_muc_giang_vien_ma_giang_vien",
                table: "dinh_muc_giang_vien",
                column: "ma_giang_vien");
        }
    }
}
