using System.Globalization;
using Google.OrTools.Sat;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services.RangBuoc;
using XepLichGiangDay.Api.Services.ThuatToan;
using XepLichGiangDay.Api.Services.ThuatToan.CpSat;
using XepLichGiangDay.Api.Services.ThuatToan.Genetic;
using Xunit;

namespace XepLichGiangDay.Tests;

public class CpSatAlgorithmTests
{
    [Fact]
    public void Test_CpSat_CultureInfo_ViVN_KhongCrashKhiThoiGianLaSoLe()
    {
        // Lưu lại Culture ban đầu
        var oldCulture = CultureInfo.CurrentCulture;
        var oldUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Thiết lập culture tiếng Việt (dấu phẩy thập phân: 2,5)
            var viVn = new CultureInfo("vi-VN");
            CultureInfo.CurrentCulture = viVn;
            CultureInfo.CurrentUICulture = viVn;

            double maxTime = 2.5;
            int numWorkers = 1;
            double relGap = 0.01;

            var solver = new CpSolver();

            // Định dạng InvariantCulture chuẩn xác
            solver.StringParameters = string.Create(
                CultureInfo.InvariantCulture,
                $"max_time_in_seconds:{maxTime},num_search_workers:{numWorkers},relative_gap_limit:{relGap},log_search_progress:false");

            var model = new CpModel();
            var x = model.NewBoolVar("x");
            model.Add(x == 1);

            // Phải giải thành công không bị native abort crash C++
            var status = solver.Solve(model);
            Assert.Equal(CpSolverStatus.Optimal, status);
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUiCulture;
        }
    }

    [Fact]
    public void Test_CauHinhGa_GioiHanCanTrenTranhDoS()
    {
        var cauHinh = new CauHinhGa
        {
            KichThuocQuanThe = 50_000_000,
            SoTheHe = 999_999,
            ThoiGianChayToiDaGiay = 86400,
            TyLeLaiGhep = 5.0,
            TyLeDotBien = -1.0,
            KichThuocGiaiDau = 100,
            SoCaTheUuTu = 200
        };

        cauHinh.ChuanHoaVaXacThuc();

        Assert.Equal(500, cauHinh.KichThuocQuanThe);
        Assert.Equal(5000, cauHinh.SoTheHe);
        Assert.Equal(300.0, cauHinh.ThoiGianChayToiDaGiay);
        Assert.Equal(1.0, cauHinh.TyLeLaiGhep);
        Assert.Equal(0.0, cauHinh.TyLeDotBien);
        Assert.True(cauHinh.KichThuocGiaiDau <= 50);
        Assert.True(cauHinh.SoCaTheUuTu <= 50);
    }

    [Fact]
    public async Task Test_CpSat_CancellationToken_HuyTimKiem()
    {
        var dbName = "CpSat_Cancel_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        // Tạo 1 lớp học để không bị trả về sớm KhongCoLop
        context.LopHocPhan.Add(new LopHocPhan
        {
            MaLopHocPhan = 1,
            MaHocKy = 1,
            MaHocPhan = 101,
            MaLopHocPhanTruong = "LHP01",
            TenLop = "Lop 1"
        });
        await context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Hủy ngay trước khi giải

        var duLieu = new DuLieuXepLichDto
        {
            MaHocKy = 1,
            ThoiGianChayToiDa = 2.5
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await cpSatService.ChayAsync(duLieu, cts.Token);
        });
    }

    [Fact]
    public void Test_CpSat_LayDinhMucTuan_ChuanHoaDinhMucHocKySangTuan()
    {
        var boiCanh = new BoiCanhDanhGia();
        boiCanh.GiangVienTheoMa[1] = new GiangVien
        {
            MaGiangVien = 1,
            HoTen = "GV Cơ Hữu",
            ChucDanh = "Giảng viên"
        };
        boiCanh.GiangVienTheoMa[2] = new GiangVien
        {
            MaGiangVien = 2,
            HoTen = "GV Thỉnh Giảng",
            ChucDanh = "Thỉnh giảng"
        };

        // GV 1: Khai báo định mức theo kỳ (> 30) -> 90 tiết tối thiểu, 300 tiết tối đa
        boiCanh.DinhMucTheoGv[1] = new DinhMucGiangVien
        {
            MaGiangVien = 1,
            SoTietToiThieu = 90,
            SoTietToiDa = 300
        };

        var (min1, max1) = BoDanhGiaPhuongAn.LayDinhMucTuan(1, boiCanh);
        Assert.Equal(6, min1);   // 90 / 15 = 6
        Assert.Equal(25, max1);  // Khong ap tran hoc ky, toi da tuan co huu la 25 tiet

        // GV 2: Khai báo định mức theo tuần (<= 30) -> 4 tiết tối thiểu, 14 tiết tối đa
        boiCanh.DinhMucTheoGv[2] = new DinhMucGiangVien
        {
            MaGiangVien = 2,
            SoTietToiThieu = 4,
            SoTietToiDa = 14
        };

        var (min2, max2) = BoDanhGiaPhuongAn.LayDinhMucTuan(2, boiCanh);
        Assert.Equal(4, min2);
        Assert.Equal(14, max2);
    }

    [Fact]
    public void Test_CpSat_GiangVienCoHuu12TietTuan_KhongBiQuaTai()
    {
        var boiCanh = new BoiCanhDanhGia();
        boiCanh.GiangVienTheoMa[1] = new GiangVien { MaGiangVien = 1, HoTen = "Lê Văn Minh", ChucDanh = "Giảng viên" };
        boiCanh.DinhMucTheoGv[1] = new DinhMucGiangVien { MaGiangVien = 1, SoTietToiThieu = 60, SoTietToiDa = 120 };

        var (minTiet, maxTiet) = BoDanhGiaPhuongAn.LayDinhMucTuan(1, boiCanh);
        Assert.Equal(25, maxTiet);

        boiCanh.ChiTietTheoLop[1] = [new() { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 2, TietBatDau = 7, TietKetThuc = 9, TuTuan = 1, DenTuan = 8 }];
        boiCanh.ChiTietTheoLop[2] = [new() { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 2, TietBatDau = 10, TietKetThuc = 12, TuTuan = 1, DenTuan = 8 }];
        boiCanh.ChiTietTheoLop[3] = [new() { MaChiTiet = 3, MaLopHocPhan = 3, Thu = 3, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 8 }];
        boiCanh.ChiTietTheoLop[4] = [new() { MaChiTiet = 4, MaLopHocPhan = 4, Thu = 5, TietBatDau = 10, TietKetThuc = 12, TuTuan = 1, DenTuan = 8 }];

        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(null!, boKiemTra);

        double diemPhat = boDanhGia.TinhDiemCanBangTai(1, [1, 2, 3, 4], boiCanh, out string canhBao);
        Assert.Equal(0, diemPhat);
        Assert.Empty(canhBao);
    }

    [Fact]
    public async Task Test_CpSat_LopKhongCoUngVien_TraVeVoNghiemCoViPhamCung()
    {
        var dbName = "CpSat_NoCand_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        context.LopHocPhan.Add(new LopHocPhan
        {
            MaLopHocPhan = 1,
            MaHocKy = 1,
            MaHocPhan = 999, // Môn không có ai có năng lực
            MaLopHocPhanTruong = "LHP_KHONG_GV",
            TenLop = "Lớp Không Có Giảng Viên"
        });
        context.GiangVien.Add(new GiangVien
        {
            MaGiangVien = 1,
            HoTen = "Thầy A",
            TrangThai = "DangLamViec"
        });
        // Không thêm bản ghi NangLucGiangDay nào
        await context.SaveChangesAsync();

        var result = await cpSatService.ChayAsync(new DuLieuXepLichDto { MaHocKy = 1, ThoiGianChayToiDa = 5.0 });

        Assert.Equal("KhaThiMotPhan", result.TrangThai);
        Assert.Equal(1, result.SoViPhamCung);
        Assert.Equal(1, result.SoLopChuaPhanCong);
        Assert.NotEmpty(result.DanhSachViPham);
    }

    [Fact]
    public async Task Test_CpSat_ChiaNuaKy_KhongBiVoNghiemGia()
    {
        // Kịch bản: 14 lớp học phần, mỗi lớp 3 tiết.
        // 7 lớp dạy nửa kỳ đầu (tuần 1-8), 7 lớp dạy nửa kỳ sau (tuần 9-16).
        // Tổng số tiết cả kỳ = 14 * 3 = 42 tiết (> trần cũ 40).
        // Nhưng mỗi tuần chỉ dạy đúng 21 tiết (<= định mức trần 25 tiết/tuần).
        // Thuật toán CP-SAT mới tính theo tuần phải giải thành công và tìm ra nghiệm khả thi/tối ưu!
        var dbName = "CpSat_SplitSemester_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        int maHocKy = 1;
        int maHp = 10;
        int maGv = 1;

        context.HocKy.Add(new HocKy { MaHocKy = maHocKy, TenHocKy = "HK1" });
        context.GiangVien.Add(new GiangVien
        {
            MaGiangVien = maGv,
            HoTen = "GV Nguyễn Văn A",
            ChucDanh = "Giảng viên",
            TrangThai = "DangLamViec"
        });
        context.NangLucGiangDay.Add(new NangLucGiangDay
        {
            MaGiangVien = maGv,
            MaHocPhan = maHp,
            TrangThai = "DangApDung"
        });

        // 7 slot lịch không trùng nhau:
        // Slot 1: Thứ 2 tiết 1-3
        // Slot 2: Thứ 2 tiết 4-6
        // Slot 3: Thứ 3 tiết 1-3
        // Slot 4: Thứ 3 tiết 4-6
        // Slot 5: Thứ 4 tiết 1-3
        // Slot 6: Thứ 4 tiết 4-6
        // Slot 7: Thứ 5 tiết 1-3
        var slots = new (int Thu, int Tbd, int Tkt)[]
        {
            (2, 1, 3),
            (2, 4, 6),
            (3, 1, 3),
            (3, 4, 6),
            (4, 1, 3),
            (4, 4, 6),
            (5, 1, 3)
        };

        // 7 lớp tuần 1-8
        for (int i = 0; i < 7; i++)
        {
            int maLop = i + 1;
            context.LopHocPhan.Add(new LopHocPhan
            {
                MaLopHocPhan = maLop,
                MaHocKy = maHocKy,
                MaHocPhan = maHp,
                MaLopHocPhanTruong = $"LHP_D1_{i + 1}",
                TenLop = $"Lớp Đợt 1 - {i + 1}"
            });
            context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu
            {
                MaChiTiet = maLop,
                MaLopHocPhan = maLop,
                Thu = slots[i].Thu,
                TietBatDau = slots[i].Tbd,
                TietKetThuc = slots[i].Tkt,
                TuTuan = 1,
                DenTuan = 8
            });
        }

        // 7 lớp tuần 9-16 (cùng slot nhưng khác tuần)
        for (int i = 0; i < 7; i++)
        {
            int maLop = i + 8;
            context.LopHocPhan.Add(new LopHocPhan
            {
                MaLopHocPhan = maLop,
                MaHocKy = maHocKy,
                MaHocPhan = maHp,
                MaLopHocPhanTruong = $"LHP_D2_{i + 1}",
                TenLop = $"Lớp Đợt 2 - {i + 1}"
            });
            context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu
            {
                MaChiTiet = maLop,
                MaLopHocPhan = maLop,
                Thu = slots[i].Thu,
                TietBatDau = slots[i].Tbd,
                TietKetThuc = slots[i].Tkt,
                TuTuan = 9,
                DenTuan = 16
            });
        }

        await context.SaveChangesAsync();

        var result = await cpSatService.ChayAsync(new DuLieuXepLichDto
        {
            MaHocKy = maHocKy,
            ThoiGianChayToiDa = 5.0
        });

        // Kết quả phải thành công (không bị vô nghiệm giả do tổng tiết 42 > 40)
        Assert.True(result.TrangThai == "ToiUuToanCuc" || result.TrangThai == "KhaThi");
        Assert.Equal(0, result.SoViPhamCung);
        Assert.Equal(14, result.DanhSachPhanCong.Count);
        Assert.All(result.DanhSachPhanCong, pc => Assert.Equal(maGv, pc.MaGiangVien));
    }

    [Fact]
    public async Task Test_CpSat_HoTroXepLichNgayChuNhat()
    {
        var dbName = "CpSat_Sunday_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        int maHocKy = 1;
        int maHp = 10;
        int maGv = 1;

        context.GiangVien.Add(new GiangVien
        {
            MaGiangVien = maGv,
            HoTen = "GV Dạy Chủ Nhật",
            ChucDanh = "Giảng viên",
            TrangThai = "DangLamViec"
        });
        context.NangLucGiangDay.Add(new NangLucGiangDay
        {
            MaGiangVien = maGv,
            MaHocPhan = maHp,
            TrangThai = "DangApDung"
        });
        context.LopHocPhan.Add(new LopHocPhan
        {
            MaLopHocPhan = 1,
            MaHocKy = maHocKy,
            MaHocPhan = maHp,
            MaLopHocPhanTruong = "LHP_CN",
            TenLop = "Lớp Chủ Nhật"
        });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu
        {
            MaChiTiet = 1,
            MaLopHocPhan = 1,
            Thu = 8, // Chủ Nhật
            TietBatDau = 1,
            TietKetThuc = 3,
            TuTuan = 1,
            DenTuan = 15
        });

        await context.SaveChangesAsync();

        var result = await cpSatService.ChayAsync(new DuLieuXepLichDto
        {
            MaHocKy = maHocKy,
            ThoiGianChayToiDa = 5.0
        });

        Assert.True(result.TrangThai == "ToiUuToanCuc" || result.TrangThai == "KhaThi");
        Assert.Equal(0, result.SoViPhamCung);
        Assert.Single(result.DanhSachPhanCong);
        Assert.Equal(maGv, result.DanhSachPhanCong[0].MaGiangVien);
    }

    [Fact]
    public async Task Test_CpSat_KhoaChetCungSeminar_BoQuaKhoaCuVaKhongXepTrung()
    {
        var dbName = "CpSat_SeminarLock_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        context.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 10, HoTen = "GV Seminar", TrangThai = "DangLamViec" },
            new GiangVien { MaGiangVien = 20, HoTen = "GV Thay Thế", TrangThai = "DangLamViec" }
        );
        context.NangLucGiangDay.AddRange(
            new NangLucGiangDay { MaGiangVien = 10, MaHocPhan = 101, TrangThai = "DangApDung" },
            new NangLucGiangDay { MaGiangVien = 20, MaHocPhan = 101, TrangThai = "DangApDung" },
            new NangLucGiangDay { MaGiangVien = 10, MaHocPhan = 102, TrangThai = "DangApDung" }
        );
        context.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L1", TenLop = "Lớp 1" },
            new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 1, MaHocPhan = 102, MaLopHocPhanTruong = "L2", TenLop = "Lớp 2" }
        );
        context.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 3, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 10 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 5, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 10 }
        );
        context.PhanCongGiangDay.Add(new PhanCongGiangDay
        {
            MaHocKy = 1,
            MaLopHocPhan = 1,
            MaGiangVien = 10,
            TrangThai = "DaKhoa",
            NguonPhanCong = "ThuCong"
        });
        context.LichBanGiangVien.AddRange(
            new LichBanGiangVien { MaHocKy = 1, MaGiangVien = 10, Thu = 3, TietBatDau = 4, TietKetThuc = 6, LyDo = "[Seminar] Bộ môn" },
            new LichBanGiangVien { MaHocKy = 1, MaGiangVien = 10, Thu = 5, TietBatDau = 1, TietKetThuc = 3, LyDo = "[Seminar] Hội thảo" }
        );
        await context.SaveChangesAsync();

        var result = await cpSatService.ChayAsync(new DuLieuXepLichDto
        {
            MaHocKy = 1,
            ThoiGianChayToiDa = 5.0
        });

        Assert.Equal("KhaThiMotPhan", result.TrangThai);
        Assert.Equal(1, result.SoLopChuaPhanCong);
        Assert.Single(result.DanhSachPhanCong);
        Assert.Equal(1, result.DanhSachPhanCong[0].MaLopHocPhan);
        Assert.Equal(20, result.DanhSachPhanCong[0].MaGiangVien);
        Assert.DoesNotContain(result.DanhSachViPham, v => v.Contains("lịch bận"));
    }

    [Fact]
    public async Task Test_PhanCongThuCong_ChanTuyetDoiKhiTrungSeminar_KeCaBoQuaCanhBao()
    {
        var dbName = "Manual_SeminarLock_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var xepLichService = new XepLichGiangDay.Api.Services.XepLichService(context, new BoKiemTraRangBuoc());

        context.GiangVien.Add(new GiangVien { MaGiangVien = 10, HoTen = "Thầy Cường", TrangThai = "DangLamViec" });
        context.NangLucGiangDay.Add(new NangLucGiangDay { MaGiangVien = 10, MaHocPhan = 101, TrangThai = "DangApDung" });
        context.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L1", TenLop = "Lớp 1" });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 3, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 10 });
        context.LichBanGiangVien.Add(new LichBanGiangVien
        {
            MaHocKy = 1,
            MaGiangVien = 10,
            Thu = 3,
            TietBatDau = 4,
            TietKetThuc = 6,
            LyDo = "[Seminar] Seminar Bộ môn Toán"
        });
        await context.SaveChangesAsync();

        var kqLan1 = await xepLichService.PhanCongThuCongAsync(new YeuCauPhanCongThuCongDto
        {
            MaLopHocPhan = 1,
            MaGiangVien = 10,
            BoQuaCanhBao = false
        });
        Assert.False(kqLan1.ThanhCong);
        Assert.False(kqLan1.CanXacNhan);
        Assert.Contains("KHÔNG THỂ PHÂN CÔNG", kqLan1.ThongBao);
        Assert.Contains(kqLan1.DanhSachViPham, v => v.Contains("KHÓA CHẾT CỨNG SEMINAR"));

        var kqEpGan = await xepLichService.PhanCongThuCongAsync(new YeuCauPhanCongThuCongDto
        {
            MaLopHocPhan = 1,
            MaGiangVien = 10,
            BoQuaCanhBao = true
        });
        Assert.False(kqEpGan.ThanhCong);
        Assert.False(kqEpGan.CanXacNhan);
        Assert.Empty(await context.PhanCongGiangDay.ToListAsync());
    }

    [Fact]
    public async Task Test_TaoLichSeminar_TuDongGoPhanCongDaKhoaTrungGio()
    {
        var dbName = "Service_CreateSeminar_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var nvService = new XepLichGiangDay.Api.Services.NguyenVongService(context);

        context.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 10, HoTen = "GV A", TrangThai = "DangLamViec" },
            new GiangVien { MaGiangVien = 20, HoTen = "GV B", TrangThai = "DangLamViec" }
        );
        context.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L_TRUNG", TenLop = "Lớp Trùng" },
            new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 1, MaHocPhan = 102, MaLopHocPhanTruong = "L_KHONG_TRUNG", TenLop = "Lớp Khác Giờ" }
        );
        context.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 3, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 10 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 5, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 10 }
        );
        context.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 1, MaGiangVien = 10, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" },
            new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 2, MaGiangVien = 20, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" }
        );
        await context.SaveChangesAsync();

        var kq = await nvService.TaoLichSeminarAsync(new XepLichGiangDay.Api.DTOs.NguyenVong.TaoLichSeminarRequest
        {
            MaHocKy = 1,
            TenSeminar = "Sinh hoạt học thuật tháng",
            Thu = 3,
            TietBatDau = 4,
            TietKetThuc = 6,
            DanhSachMaGiangVien = [10, 20]
        });

        Assert.True(kq.ThanhCong);
        Assert.Equal(2, kq.SoGiangVienDaKhoa);
        Assert.Equal(1, kq.SoLopBiGo);

        var dsLb = await context.LichBanGiangVien.Where(x => x.MaHocKy == 1).ToListAsync();
        Assert.Equal(2, dsLb.Count);
        Assert.All(dsLb, lb => Assert.StartsWith("[Seminar]", lb.LyDo));

        var dsPcConLai = await context.PhanCongGiangDay.Where(x => x.MaHocKy == 1).ToListAsync();
        Assert.Single(dsPcConLai);
        Assert.Equal(2, dsPcConLai[0].MaLopHocPhan);
    }

    [Fact]
    public async Task Test_NguyenVongSC01_CaHocKy_MacDinhMem_KhongKhoaCungVaKhongGoLop()
    {
        var dbName = "Service_SoftSc01_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var nvService = new XepLichGiangDay.Api.Services.NguyenVongService(context);

        context.GiangVien.Add(new GiangVien { MaGiangVien = 19, HoTen = "Trần Thị Liễu", TrangThai = "DangLamViec" });
        context.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 10, MaHocKy = 1, MaHocPhan = 100, MaLopHocPhanTruong = "398803_71CS1", TenLop = "71CS1" });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 10, Thu = 4, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 });
        context.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 10, MaGiangVien = 19, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" });
        await context.SaveChangesAsync();

        var created = await nvService.ThemNguyenVongAsync(new XepLichGiangDay.Api.DTOs.NguyenVong.ThemNguyenVongDto
        {
            MaHocKy = 1,
            MaGiangVien = 19,
            MaRangBuocMem = "SC01",
            Thu = 0,
            TietBatDau = 1,
            TietKetThuc = 3,
            TuTuan = 1,
            DenTuan = 20,
            TrongSoPhat = 15,
            LyDo = "Tránh tiết 1-3",
            KhoaCungLichBan = false
        });

        Assert.NotNull(created);
        Assert.Equal("[Cả học kỳ] Tránh tiết 1-3", created!.NoiDungGoc);
        Assert.False(created.KhoaCungLichBan);
        Assert.Equal(1, created.MucDo);

        var kqDuyet = await nvService.DuyetNguyenVongAsync(created.MaNguyenVong);
        Assert.True(kqDuyet.ThanhCong);
        Assert.Equal(0, kqDuyet.SoLopBiHuy);
        Assert.False(kqDuyet.NguyenVong!.KhoaCungLichBan);

        Assert.Empty(await context.LichBanGiangVien.Where(x => x.MaHocKy == 1).ToListAsync());
        Assert.Single(await context.PhanCongGiangDay.Where(x => x.MaHocKy == 1).ToListAsync());

        var ds = await nvService.LayNguyenVongAsync(1, 19);
        var item = Assert.Single(ds);
        Assert.False(item.KhoaCungLichBan);
    }

    [Fact]
    public async Task Test_NguyenVongSC01_KhoaCungLichBan_TaoLichBanVaGoLopTrung()
    {
        var dbName = "Service_HardSc01_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var nvService = new XepLichGiangDay.Api.Services.NguyenVongService(context);

        context.GiangVien.Add(new GiangVien { MaGiangVien = 19, HoTen = "Trần Thị Liễu", TrangThai = "DangLamViec" });
        context.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 11, MaHocKy = 1, MaHocPhan = 100, MaLopHocPhanTruong = "LOP_T5", TenLop = "Lớp T5" });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 11, Thu = 5, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 });
        context.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 11, MaGiangVien = 19, TrangThai = "DuKien", NguonPhanCong = "GA" });
        await context.SaveChangesAsync();

        var created = await nvService.ThemNguyenVongAsync(new XepLichGiangDay.Api.DTOs.NguyenVong.ThemNguyenVongDto
        {
            MaHocKy = 1,
            MaGiangVien = 19,
            MaRangBuocMem = "SC01",
            Thu = 5,
            TietBatDau = 1,
            TietKetThuc = 15,
            TuTuan = 1,
            DenTuan = 20,
            TrongSoPhat = 15,
            LyDo = "Xin nghỉ Thứ 5",
            KhoaCungLichBan = true
        });

        Assert.NotNull(created);
        Assert.True(created!.KhoaCungLichBan);
        Assert.Equal(2, created.MucDo);

        var kqDuyet = await nvService.DuyetNguyenVongAsync(created.MaNguyenVong);
        Assert.True(kqDuyet.ThanhCong);
        Assert.Equal(1, kqDuyet.SoLopBiHuy);
        Assert.True(kqDuyet.NguyenVong!.KhoaCungLichBan);

        Assert.Single(await context.LichBanGiangVien.Where(x => x.MaHocKy == 1).ToListAsync());
        Assert.Empty(await context.PhanCongGiangDay.Where(x => x.MaHocKy == 1).ToListAsync());

        var ds = await nvService.LayNguyenVongAsync(1, 19);
        var item = Assert.Single(ds);
        Assert.True(item.KhoaCungLichBan);
    }

    [Fact]
    public async Task Test_CpSat_TonTrongSC06_GioiHanSoTietToiDaTrongNgay()
    {
        var dbName = "CpSat_SC06_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using var context = new AppDbContext(options);
        var boKiemTra = new BoKiemTraRangBuoc();
        var boDanhGia = new BoDanhGiaPhuongAn(context, boKiemTra);
        var luuKetQuaService = new LuuKetQuaXepLichService(context, boDanhGia);
        var cpSatService = new CpSatAlgorithmService(context, boDanhGia, boKiemTra, luuKetQuaService);

        // 2 GV cùng chuyên môn HP 101; mỗi lớp 3 tiết cùng vào Thứ 2 (tổng 9 tiết nếu dồn 1 người).
        // GV 1 có SC06 giới hạn tối đa 6 tiết/ngày (tối đa 2 lớp vào Thứ 2); GV 2 không bị giới hạn SC06.
        // Để tránh phạt cân bằng thiếu tải gây nhiễu, đặt minTiet = 3 cho cả 2 GV.
        context.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 1, HoTen = "Cô Liễu", ChucDanh = "Giảng viên", TrangThai = "DangLamViec" },
            new GiangVien { MaGiangVien = 2, HoTen = "Thầy Minh", ChucDanh = "Giảng viên", TrangThai = "DangLamViec" }
        );
        context.DinhMucGiangVien.AddRange(
            new DinhMucGiangVien { MaHocKy = 1, MaGiangVien = 1, SoTietToiThieu = 3, SoTietToiDa = 25, TrangThai = "DangApDung" },
            new DinhMucGiangVien { MaHocKy = 1, MaGiangVien = 2, SoTietToiThieu = 3, SoTietToiDa = 25, TrangThai = "DangApDung" }
        );
        context.NangLucGiangDay.AddRange(
            new NangLucGiangDay { MaGiangVien = 1, MaHocPhan = 101, TrangThai = "DangApDung" },
            new NangLucGiangDay { MaGiangVien = 2, MaHocPhan = 101, TrangThai = "DangApDung" }
        );
        context.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L1" },
            new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L2" },
            new LopHocPhan { MaLopHocPhan = 3, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "L3" }
        );
        context.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 2, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 2, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 3, MaLopHocPhan = 3, Thu = 2, TietBatDau = 7, TietKetThuc = 9, TuTuan = 1, DenTuan = 15 }
        );
        context.NguyenVongGiangVien.Add(new NguyenVongGiangVien
        {
            MaHocKy = 1,
            MaGiangVien = 1,
            LoaiNguyenVong = "SC06",
            Thu = null,
            TietBatDau = 1,
            TietKetThuc = 6, // tối đa 6 tiết/ngày
            TrongSoPhat = 100,
            TrangThai = "DaDuyet"
        });
        await context.SaveChangesAsync();

        var result = await cpSatService.ChayAsync(new DuLieuXepLichDto
        {
            MaHocKy = 1,
            ThoiGianChayToiDa = 5.0,
            LuuKetQua = false
        });

        Assert.Equal("ToiUuToanCuc", result.TrangThai);
        Assert.Equal(0, result.SoViPhamCung);
        int soLopGv1 = result.DanhSachPhanCong.Count(x => x.MaGiangVien == 1);
        Assert.True(soLopGv1 <= 2, $"GV 1 bị giao {soLopGv1} lớp (>2 lớp = >6 tiết/ngày).");
        Assert.Equal(0, result.DiemPhatNguyenVong);
    }
}
