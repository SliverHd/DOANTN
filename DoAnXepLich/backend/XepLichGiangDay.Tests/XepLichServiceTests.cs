using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Tests;

public class XepLichServiceTests
{
    private static AppDbContext TaoInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task XuatExcelBangPhanCongAsync_TachDayDuTungBuoiHocCuaMoiLop()
    {
        using var db = TaoInMemoryDb(nameof(XuatExcelBangPhanCongAsync_TachDayDuTungBuoiHocCuaMoiLop));
        db.HocKy.Add(new HocKy { MaHocKy = 8, MaNamHoc = 1, TenHocKy = "Học kỳ 8" });
        db.HocPhan.AddRange(
            new HocPhan { MaHocPhan = 1, TenHocPhan = "Giải tích 2", SoTinChi = 3, MaBoMon = 1 },
            new HocPhan { MaHocPhan = 2, TenHocPhan = "Giải tích ứng dụng", SoTinChi = 3, MaBoMon = 1 }
        );
        db.GiangVien.Add(new GiangVien
        {
            MaGiangVien = 10,
            HoTen = "Lê Viết Cường",
            Email = "cuonglv@huce.edu.vn",
            TrangThai = "DangLamViec",
            MaBoMon = 1
        });
        db.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 100, MaHocKy = 8, MaHocPhan = 1, MaLopHocPhanTruong = "396602_69IT3", TenLop = "69IT3", SoLuongSinhVien = 45 },
            new LopHocPhan { MaLopHocPhan = 101, MaHocKy = 8, MaHocPhan = 2, MaLopHocPhanTruong = "398803_71IT1", TenLop = "71IT1", SoLuongSinhVien = 40 }
        );

        // Lớp 100 có 3 buổi ở các thứ / phòng / tuần khác nhau (cố tình insert không theo thứ tự)
        db.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 100, Thu = 8, TietBatDau = 4, TietKetThuc = 6, PhongHoc = "301.H1", TuTuan = 12, DenTuan = 20 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 100, Thu = 2, TietBatDau = 1, TietKetThuc = 3, PhongHoc = "101.H1", TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 3, MaLopHocPhan = 100, Thu = 5, TietBatDau = 7, TietKetThuc = 9, PhongHoc = "205.H2", TuTuan = 1, DenTuan = 11 },
            // Lớp 101 có 1 buổi
            new ChiTietThoiKhoaBieu { MaChiTiet = 4, MaLopHocPhan = 101, Thu = 3, TietBatDau = 10, TietKetThuc = 12, PhongHoc = "505.H1", TuTuan = 6, DenTuan = 18 }
        );
        db.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 100, MaGiangVien = 10, NguonPhanCong = "GA", TrangThai = "DaKhoa" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 101, MaGiangVien = 10, NguonPhanCong = "ThuCong", TrangThai = "DuKien" }
        );
        await db.SaveChangesAsync();

        var service = new XepLichService(db, new BoKiemTraRangBuoc());
        var bytes = await service.XuatExcelBangPhanCongAsync(8);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet("ThoiKhoaBieu");

        // Header ở dòng 3, dữ liệu bắt đầu từ dòng 4: 3 buổi lớp 100 + 1 buổi lớp 101 = 4 dòng (dòng 4..7)
        Assert.Equal(7, ws.LastRowUsed()!.RowNumber());

        // Dòng 4, 5, 6 đều là của lớp 396602_69IT3 nhưng giữ nguyên giảng viên và tách đủ 3 buổi theo thứ tự
        Assert.Equal(1, ws.Cell(4, 1).GetValue<int>());
        Assert.Equal("396602_69IT3", ws.Cell(4, 2).GetString());
        Assert.Equal("Thứ 2", ws.Cell(4, 7).GetString());
        Assert.Equal(1, ws.Cell(4, 8).GetValue<int>());
        Assert.Equal(3, ws.Cell(4, 9).GetValue<int>());
        Assert.Equal(3, ws.Cell(4, 10).GetValue<int>());
        Assert.Equal("101.H1", ws.Cell(4, 11).GetString());
        Assert.Equal("1-15", ws.Cell(4, 12).GetString());
        Assert.Equal("Lê Viết Cường", ws.Cell(4, 13).GetString());
        Assert.Equal("Đã khóa", ws.Cell(4, 14).GetString());
        Assert.Equal("GA", ws.Cell(4, 15).GetString());

        Assert.Equal(2, ws.Cell(5, 1).GetValue<int>());
        Assert.Equal("396602_69IT3", ws.Cell(5, 2).GetString());
        Assert.Equal("Thứ 5", ws.Cell(5, 7).GetString());
        Assert.Equal("205.H2", ws.Cell(5, 11).GetString());
        Assert.Equal("1-11", ws.Cell(5, 12).GetString());
        Assert.Equal("Lê Viết Cường", ws.Cell(5, 13).GetString());

        Assert.Equal(3, ws.Cell(6, 1).GetValue<int>());
        Assert.Equal("396602_69IT3", ws.Cell(6, 2).GetString());
        Assert.Equal("Chủ nhật", ws.Cell(6, 7).GetString());
        Assert.Equal("301.H1", ws.Cell(6, 11).GetString());
        Assert.Equal("12-20", ws.Cell(6, 12).GetString());
        Assert.Equal("Lê Viết Cường", ws.Cell(6, 13).GetString());

        // Dòng 7 là lớp 398803_71IT1
        Assert.Equal(4, ws.Cell(7, 1).GetValue<int>());
        Assert.Equal("398803_71IT1", ws.Cell(7, 2).GetString());
        Assert.Equal("Thứ 3", ws.Cell(7, 7).GetString());
        Assert.Equal("Dự kiến", ws.Cell(7, 14).GetString());
        Assert.Equal("Thủ công", ws.Cell(7, 15).GetString());
    }

    [Fact]
    public async Task LayBangPhanCongAsync_PhatHienTamNghiVaSapXepBuoiHoc()
    {
        using var db = TaoInMemoryDb(nameof(LayBangPhanCongAsync_PhatHienTamNghiVaSapXepBuoiHoc));
        db.HocKy.Add(new HocKy { MaHocKy = 1, MaNamHoc = 1, TenHocKy = "HK1" });
        db.HocPhan.Add(new HocPhan { MaHocPhan = 1, TenHocPhan = "Giải tích 2", SoTinChi = 3 });
        db.GiangVien.Add(new GiangVien { MaGiangVien = 10, HoTen = "Nguyễn Thị Lệ Hải", TrangThai = "DangLamViec" });
        db.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 1, MaLopHocPhanTruong = "396602_71IT1" });
        db.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 6, TietBatDau = 4, TietKetThuc = 6, PhongHoc = "508.H1", TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 1, Thu = 4, TietBatDau = 4, TietKetThuc = 6, PhongHoc = "507.H1", TuTuan = 1, DenTuan = 15 }
        );
        db.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 1, MaGiangVien = 10, NguonPhanCong = "GA", TrangThai = "DaKhoa" });
        // Đơn tạm nghỉ vào Thứ 6 (buổi thứ 2 sau khi sắp xếp) vẫn phải phát hiện được
        db.NguyenVongGiangVien.Add(new NguyenVongGiangVien
        {
            MaHocKy = 1,
            MaGiangVien = 10,
            LoaiNguyenVong = "TamNghi",
            Thu = 6,
            TietBatDau = 4,
            TietKetThuc = 6,
            TrangThai = "DaDuyet",
            NoiDungGoc = "Xin nghỉ Tuần 18 đi công tác"
        });
        await db.SaveChangesAsync();

        var service = new XepLichService(db, new BoKiemTraRangBuoc());
        var bang = await service.LayBangPhanCongAsync(1);

        var lop = Assert.Single(bang.DanhSachLop);
        Assert.Equal(2, lop.DanhSachBuoiHoc.Count);
        Assert.Equal(4, lop.DanhSachBuoiHoc[0].Thu);
        Assert.Equal(6, lop.DanhSachBuoiHoc[1].Thu);
        Assert.True(lop.CoLichTamNghi);
        Assert.Equal("18", lop.TuanTamNghi);
    }
}
