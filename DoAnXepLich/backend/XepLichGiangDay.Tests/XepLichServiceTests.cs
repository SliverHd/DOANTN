using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Controllers;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services;
using XepLichGiangDay.Api.Services.RangBuoc;
using XepLichGiangDay.Api.Services.ThuatToan;

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

    [Fact]
    public async Task LayThongKeTaiAsync_DemDuNhieuBuoiHoc_KhuLopGhep_VaTinhTaiTuanCaoDiem()
    {
        using var db = TaoInMemoryDb(nameof(LayThongKeTaiAsync_DemDuNhieuBuoiHoc_KhuLopGhep_VaTinhTaiTuanCaoDiem));
        db.HocKy.Add(new HocKy { MaHocKy = 8, MaNamHoc = 1, TenHocKy = "HK8" });
        db.HocPhan.AddRange(
            new HocPhan { MaHocPhan = 17, TenHocPhan = "Giải tích 1", SoTinChi = 3 },
            new HocPhan { MaHocPhan = 19, TenHocPhan = "Giải tích ứng dụng", SoTinChi = 3 }
        );
        db.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 19, HoTen = "Trần Thị Liễu", Email = "lieu@huce.edu.vn", TrangThai = "DangLamViec" },
            new GiangVien { MaGiangVien = 22, HoTen = "Vũ Thị Thủy", Email = "thuy@huce.edu.vn", TrangThai = "DangLamViec" }
        );

        // GV 19 dạy 1 lớp có 2 buổi (3t + 3t = 6 tiết/tuần -> đạt tối thiểu 6 tiết, BinhThuong)
        db.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 8, MaHocPhan = 19, MaLopHocPhanTruong = "398803_71CS1" });
        db.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 2, TietBatDau = 4, TietKetThuc = 6, PhongHoc = "H3.67", TuTuan = 6, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 1, Thu = 4, TietBatDau = 1, TietKetThuc = 3, PhongHoc = "H3.61", TuTuan = 6, DenTuan = 14 }
        );
        db.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 1, MaGiangVien = 19, TrangThai = "DuKien" });

        // GV 22 dạy 2 lớp ghép cùng giờ/phòng (6t thực tế) + các lớp khác lên tới 26 tiết ở tuần 6 (QuaTai > 25),
        // và thêm 1 lớp ở tuần 18-25 (không cộng dồn sang tuần 6)
        db.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 8, MaHocPhan = 17, MaLopHocPhanTruong = "GHEP_1" },
            new LopHocPhan { MaLopHocPhan = 3, MaHocKy = 8, MaHocPhan = 17, MaLopHocPhanTruong = "GHEP_2" },
            new LopHocPhan { MaLopHocPhan = 4, MaHocKy = 8, MaHocPhan = 19, MaLopHocPhanTruong = "LOP_A" },
            new LopHocPhan { MaLopHocPhan = 5, MaHocKy = 8, MaHocPhan = 19, MaLopHocPhanTruong = "LOP_B" },
            new LopHocPhan { MaLopHocPhan = 6, MaHocKy = 8, MaHocPhan = 19, MaLopHocPhanTruong = "LOP_KY_SAU" }
        );
        db.ChiTietThoiKhoaBieu.AddRange(
            // GHEP_1 và GHEP_2 cùng môn 17, cùng phòng 501.H1, tuần 6-15 -> chỉ tính 6 tiết
            new ChiTietThoiKhoaBieu { MaChiTiet = 3, MaLopHocPhan = 2, Thu = 2, TietBatDau = 1, TietKetThuc = 6, PhongHoc = "501.H1", TuTuan = 6, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 4, MaLopHocPhan = 3, Thu = 2, TietBatDau = 1, TietKetThuc = 6, PhongHoc = "501.H1", TuTuan = 6, DenTuan = 15 },
            // LOP_A: Thứ 3 (10 tiết) tuần 6-15
            new ChiTietThoiKhoaBieu { MaChiTiet = 5, MaLopHocPhan = 4, Thu = 3, TietBatDau = 1, TietKetThuc = 5, PhongHoc = "601.H1", TuTuan = 6, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 6, MaLopHocPhan = 4, Thu = 3, TietBatDau = 7, TietKetThuc = 11, PhongHoc = "601.H1", TuTuan = 6, DenTuan = 15 },
            // LOP_B: Thứ 5 (10 tiết) tuần 6-15 => Tổng tuần 6 = 6 + 10 + 10 = 26 tiết (> 25)
            new ChiTietThoiKhoaBieu { MaChiTiet = 7, MaLopHocPhan = 5, Thu = 5, TietBatDau = 1, TietKetThuc = 5, PhongHoc = "602.H1", TuTuan = 6, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 8, MaLopHocPhan = 5, Thu = 5, TietBatDau = 7, TietKetThuc = 11, PhongHoc = "602.H1", TuTuan = 6, DenTuan = 15 },
            // LOP_KY_SAU: Tuần 18-25 (6 tiết) - không cộng vào tuần 6
            new ChiTietThoiKhoaBieu { MaChiTiet = 9, MaLopHocPhan = 6, Thu = 4, TietBatDau = 1, TietKetThuc = 6, PhongHoc = "603.H1", TuTuan = 18, DenTuan = 25 }
        );
        db.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 2, MaGiangVien = 22, TrangThai = "DuKien" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 3, MaGiangVien = 22, TrangThai = "DuKien" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 4, MaGiangVien = 22, TrangThai = "DuKien" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 5, MaGiangVien = 22, TrangThai = "DuKien" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 6, MaGiangVien = 22, TrangThai = "DuKien" }
        );
        await db.SaveChangesAsync();

        var service = new XepLichService(db, new BoKiemTraRangBuoc());
        var ds = await service.LayThongKeTaiAsync(8);

        var lieu = ds.First(x => x.MaGiangVien == 19);
        Assert.Equal(6, lieu.TongSoTietTuan);
        Assert.Equal("BinhThuong", lieu.TrangThaiTai);

        var thuy = ds.First(x => x.MaGiangVien == 22);
        Assert.Equal(26, thuy.TongSoTietTuan);
        Assert.Equal("QuaTai", thuy.TrangThaiTai);
    }

    private sealed class FakeSchedulingAlgorithm(
        string tenThuatToan,
        Func<DuLieuXepLichDto, Task<KetQuaXepLichDto>> handler) : ISchedulingAlgorithm
    {
        public string TenThuatToan => tenThuatToan;
        public Task<KetQuaXepLichDto> ChayAsync(DuLieuXepLichDto duLieu, CancellationToken cancellationToken = default)
            => handler(duLieu);
    }

    [Fact]
    public async Task XepLichController_SoSanhThuatToan_VanTraVeKetQuaGa_KhiCpSatLoiNativeOrTools()
    {
        using var db = TaoInMemoryDb(nameof(XepLichController_SoSanhThuatToan_VanTraVeKetQuaGa_KhiCpSatLoiNativeOrTools));
        db.HocKy.Add(new HocKy { MaHocKy = 8, MaNamHoc = 1, TenHocKy = "Học kỳ 8" });
        await db.SaveChangesAsync();

        var algorithms = new ISchedulingAlgorithm[]
        {
            new FakeSchedulingAlgorithm("CP_SAT", _ => throw new TypeInitializationException(
                "Google.OrTools.Sat.CpModel",
                new DllNotFoundException("Unable to load DLL 'google-ortools-native'"))),
            new FakeSchedulingAlgorithm("GA", _ => Task.FromResult(new KetQuaXepLichDto
            {
                ThuatToan = "GA",
                TrangThai = "ThanhCong",
                TongDiemPhat = 12.5,
                SoViPhamCung = 0
            }))
        };
        var lockService = new HocKyLockService();
        var controller = new XepLichController(
            algorithms,
            new XepLichService(db, new BoKiemTraRangBuoc()),
            lockService);

        var actionResult = await controller.SoSanhThuatToan(new YeuCauXepLichDto { MaHocKy = 8 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<SoSanhThuatToanDto>(ok.Value);
        Assert.Equal("KhongHoTroMoiTruong", dto.KetQuaCpSat.TrangThai);
        Assert.Contains("CP-SAT", dto.KetQuaCpSat.ThongDiep);
        Assert.Equal("ThanhCong", dto.KetQuaGa.TrangThai);
        Assert.Equal(12.5, dto.KetQuaGa.TongDiemPhat);
        Assert.False(lockService.DangChay(8));
    }

    [Fact]
    public async Task XepLichController_ChayXepLich_TraVeBadRequest_KhiCpSatLoiNativeOrTools()
    {
        using var db = TaoInMemoryDb(nameof(XepLichController_ChayXepLich_TraVeBadRequest_KhiCpSatLoiNativeOrTools));
        var algorithms = new ISchedulingAlgorithm[]
        {
            new FakeSchedulingAlgorithm("CP_SAT", _ => throw new TypeInitializationException(
                "Google.OrTools.Sat.CpModel",
                new BadImageFormatException("32-bit/64-bit mismatch")))
        };
        var lockService = new HocKyLockService();
        var controller = new XepLichController(
            algorithms,
            new XepLichService(db, new BoKiemTraRangBuoc()),
            lockService);

        var actionResult = await controller.ChayXepLich(new YeuCauXepLichDto
        {
            MaHocKy = 8,
            ThuatToan = "CP_SAT"
        }, CancellationToken.None);

        var badReq = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badReq.Value);
        Assert.Contains("CP-SAT", problem.Detail ?? string.Empty);
        Assert.Contains("GA", problem.Detail ?? string.Empty);
        Assert.False(lockService.DangChay(8));
    }
}
