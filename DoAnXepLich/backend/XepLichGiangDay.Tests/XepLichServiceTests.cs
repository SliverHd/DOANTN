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

    [Fact]
    public async Task XuatExcelMaTranTkbAsync_XuatDayDuTongHopThongKeVaTungGiangVienCoPhongVaSeminar()
    {
        using var db = TaoInMemoryDb(nameof(XuatExcelMaTranTkbAsync_XuatDayDuTongHopThongKeVaTungGiangVienCoPhongVaSeminar));
        db.BoMon.Add(new BoMon { MaBoMon = 5, TenBoMon = "Bộ môn Toán", TenKhoa = "Khoa Khoa học cơ bản" });
        db.HocKy.Add(new HocKy { MaHocKy = 8, MaNamHoc = 1, TenHocKy = "Học kì 1" });
        db.HocPhan.AddRange(
            new HocPhan { MaHocPhan = 1, TenHocPhan = "Giải tích 1", SoTinChi = 3, MaBoMon = 5 },
            new HocPhan { MaHocPhan = 2, TenHocPhan = "Đại số tuyến tính", SoTinChi = 3, MaBoMon = 5 }
        );
        db.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 10, HoTen = "Mai Hồng", Email = "hongm@huce.edu.vn", TrangThai = "DangLamViec", MaBoMon = 5 },
            new GiangVien { MaGiangVien = 11, HoTen = "Trần Văn Khiên", Email = "khientv@huce.edu.vn", TrangThai = "DangLamViec", MaBoMon = 5 }
        );
        db.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 100, MaHocKy = 8, MaHocPhan = 1, MaLopHocPhanTruong = "GT1_71KTDT", TenLop = "71KTDT", SoLuongSinhVien = 50 },
            new LopHocPhan { MaLopHocPhan = 101, MaHocKy = 8, MaHocPhan = 2, MaLopHocPhanTruong = "DSTT_66KTHN", TenLop = "66KTHN", SoLuongSinhVien = 45 }
        );
        db.ChiTietThoiKhoaBieu.AddRange(
            // Mai Hồng: Thứ 2 Ca 1 (T6-15 => 10 tuần = 10 buổi, 30 tiết)
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 100, Thu = 2, TietBatDau = 1, TietKetThuc = 3, PhongHoc = "301.H1", TuTuan = 6, DenTuan = 15 },
            // Trần Văn Khiên: Thứ 4 Ca 2 (T1-5 => 5 tuần = 5 buổi, 15 tiết)
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 101, Thu = 4, TietBatDau = 4, TietKetThuc = 6, PhongHoc = "614.H1", TuTuan = 1, DenTuan = 5 }
        );
        db.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 100, MaGiangVien = 10, NguonPhanCong = "GA", TrangThai = "DuKien" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 101, MaGiangVien = 11, NguonPhanCong = "GA", TrangThai = "DaKhoa" }
        );
        // Lịch Seminar Thứ 3 Ca 2 cho Trần Văn Khiên
        db.LichBanGiangVien.Add(new LichBanGiangVien
        {
            MaLichBan = 1,
            MaHocKy = 8,
            MaGiangVien = 11,
            Thu = 3,
            TietBatDau = 4,
            TietKetThuc = 6,
            LyDo = "Seminar Bộ môn Toán"
        });
        await db.SaveChangesAsync();

        var service = new XepLichService(db, new BoKiemTraRangBuoc());
        var bytes = await service.XuatExcelMaTranTkbAsync(8, 5);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        // Đủ 4 sheet: TongHop_BoMon, ThongKe_GiangVien, Mai Hồng, Trần Văn Khiên
        Assert.Equal(4, wb.Worksheets.Count);
        Assert.NotNull(wb.Worksheet("TongHop_BoMon"));
        Assert.NotNull(wb.Worksheet("ThongKe_GiangVien"));
        Assert.NotNull(wb.Worksheet("Mai Hồng"));
        Assert.NotNull(wb.Worksheet("Trần Văn Khiên"));

        // Kiểm tra sheet Tổng hợp có phòng học và [SEMINAR]
        var wsAll = wb.Worksheet("TongHop_BoMon");
        Assert.Contains("Mai Hồng - GT1 (71KTDT, P.301.H1, T6-15)", wsAll.Cell(5, 3).GetString()); // Ca 1, Thứ 2
        Assert.Contains("[SEMINAR] Trần Văn Khiên", wsAll.Cell(6, 4).GetString());                // Ca 2, Thứ 3
        Assert.Contains("GT1: Giải tích 1", wsAll.Cell(11, 1).GetString());
        Assert.Contains("ĐSTT: Đại số tuyến tính", wsAll.Cell(11, 1).GetString());

        // Kiểm tra sheet Thống kê tính đúng số buổi / số tiết cả kỳ và không còn cột link thừa
        var wsTk = wb.Worksheet("ThongKe_GiangVien");
        Assert.Equal("Tổng số buổi cả kỳ", wsTk.Cell(4, 4).GetString());
        Assert.Equal("Tổng số tiết cả kỳ", wsTk.Cell(4, 5).GetString());
        Assert.Equal(6, wsTk.LastColumnUsed()!.ColumnNumber());
        // Dòng tổng cộng (row 7): 2 lớp, 15 buổi, 45 tiết
        Assert.Equal("TỔNG CỘNG TOÀN BỘ MÔN", wsTk.Cell(7, 2).GetString());
        Assert.Equal(2, wsTk.Cell(7, 3).GetValue<int>());
        Assert.Equal(15, wsTk.Cell(7, 4).GetValue<int>());
        Assert.Equal(45, wsTk.Cell(7, 5).GetValue<int>());

        // Kiểm tra sheet cá nhân giảng viên có phòng học, có Seminar và không có nút quay lại ở H1/I1
        var wsKhien = wb.Worksheet("Trần Văn Khiên");
        Assert.Equal("[SEMINAR] Sinh hoạt Bộ môn", wsKhien.Cell(6, 4).GetString());               // Ca 2, Thứ 3
        Assert.Equal("ĐSTT (66KTHN, P.614.H1, T1-5)", wsKhien.Cell(6, 5).GetString());            // Ca 2, Thứ 4
        Assert.DoesNotContain("Mục lục", wsKhien.Cell(1, 8).GetString());
        Assert.DoesNotContain("Tổng hợp", wsKhien.Cell(1, 9).GetString());
    }

    [Fact]
    public async Task ThoiKhoaBieuService_XoaToanBoLopHocPhanAsync_XoaCaLopDaKhoaVaGiuBoMonKhac()
    {
        using var db = TaoInMemoryDb(nameof(ThoiKhoaBieuService_XoaToanBoLopHocPhanAsync_XoaCaLopDaKhoaVaGiuBoMonKhac));
        db.HocPhan.AddRange(
            new HocPhan { MaHocPhan = 1, TenHocPhan = "Giải tích 1", SoTinChi = 3, MaBoMon = 5 },
            new HocPhan { MaHocPhan = 2, TenHocPhan = "Lập trình C#", SoTinChi = 3, MaBoMon = 6 }
        );
        db.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 10, MaHocKy = 8, MaHocPhan = 1, MaLopHocPhanTruong = "TOAN_01", TenLop = "71IT1" },
            new LopHocPhan { MaLopHocPhan = 11, MaHocKy = 8, MaHocPhan = 1, MaLopHocPhanTruong = "TOAN_02", TenLop = "71IT2" },
            new LopHocPhan { MaLopHocPhan = 12, MaHocKy = 8, MaHocPhan = 2, MaLopHocPhanTruong = "CNTT_01", TenLop = "71CS1" }
        );
        db.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 10, Thu = 2, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 11, Thu = 3, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 3, MaLopHocPhan = 12, Thu = 4, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 }
        );
        db.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 10, MaGiangVien = 100, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 11, MaGiangVien = 101, TrangThai = "DuKien", NguonPhanCong = "GA" },
            new PhanCongGiangDay { MaHocKy = 8, MaLopHocPhan = 12, MaGiangVien = 102, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" }
        );
        db.KetQuaXepLich.Add(new KetQuaXepLich { MaKetQua = 1, MaHocKy = 8, MaLopHocPhan = 10, MaGiangVien = 100 });
        await db.SaveChangesAsync();

        var service = new ThoiKhoaBieuService(db);
        var (soLopDaXoa, soPhanCongDaXoa) = await service.XoaToanBoLopHocPhanAsync(maHocKy: 8, maBoMon: 5);

        Assert.Equal(2, soLopDaXoa);
        Assert.Equal(2, soPhanCongDaXoa);

        // Bộ môn 5 đã sạch (gồm cả lớp DaKhoa và KetQuaXepLich), Bộ môn 6 vẫn nguyên vẹn
        var lopConLai = await db.LopHocPhan.ToListAsync();
        Assert.Single(lopConLai);
        Assert.Equal(12, lopConLai[0].MaLopHocPhan);
        Assert.Single(await db.ChiTietThoiKhoaBieu.ToListAsync());
        Assert.Single(await db.PhanCongGiangDay.ToListAsync());
        Assert.Empty(await db.KetQuaXepLich.ToListAsync());
    }
}
