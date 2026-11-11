using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Tests;

public class NguyenVongAndSeminarTests
{
    private static AppDbContext TaoInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Test_PhanCongThuCong_ChanTuyetDoiKhiTrungSeminar_KeCaBoQuaCanhBao()
    {
        using var context = TaoInMemoryDb("Manual_SeminarLock_" + Guid.NewGuid());
        var xepLichService = new XepLichService(context, new BoKiemTraRangBuoc());

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
        using var context = TaoInMemoryDb("Service_CreateSeminar_" + Guid.NewGuid());
        var nvService = new NguyenVongService(context);

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

        var kq = await nvService.TaoLichSeminarAsync(new TaoLichSeminarRequest
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
        using var context = TaoInMemoryDb("Service_SoftSc01_" + Guid.NewGuid());
        var nvService = new NguyenVongService(context);

        context.GiangVien.Add(new GiangVien { MaGiangVien = 19, HoTen = "Trần Thị Liễu", TrangThai = "DangLamViec" });
        context.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 10, MaHocKy = 1, MaHocPhan = 100, MaLopHocPhanTruong = "398803_71CS1", TenLop = "71CS1" });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 10, Thu = 4, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 });
        context.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 10, MaGiangVien = 19, TrangThai = "DaKhoa", NguonPhanCong = "ThuCong" });
        await context.SaveChangesAsync();

        var created = await nvService.ThemNguyenVongAsync(new ThemNguyenVongDto
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
        using var context = TaoInMemoryDb("Service_HardSc01_" + Guid.NewGuid());
        var nvService = new NguyenVongService(context);

        context.GiangVien.Add(new GiangVien { MaGiangVien = 19, HoTen = "Trần Thị Liễu", TrangThai = "DangLamViec" });
        context.LopHocPhan.Add(new LopHocPhan { MaLopHocPhan = 11, MaHocKy = 1, MaHocPhan = 100, MaLopHocPhanTruong = "LOP_T5", TenLop = "Lớp T5" });
        context.ChiTietThoiKhoaBieu.Add(new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 11, Thu = 5, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 });
        context.PhanCongGiangDay.Add(new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 11, MaGiangVien = 19, TrangThai = "DuKien", NguonPhanCong = "GA" });
        await context.SaveChangesAsync();

        var created = await nvService.ThemNguyenVongAsync(new ThemNguyenVongDto
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
    public void Test_BoDanhGiaPhuongAn_MienTruLopGhep_VaTinhDiemPhatSC08()
    {
        var boDanhGia = new BoDanhGiaPhuongAn();

        var gv = new GiangVien { MaGiangVien = 10, HoTen = "Nguyễn Thị Lệ Hải", TrangThai = "DangLamViec" };
        var lop1 = new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "396602_71IT1" };
        var lop2 = new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 1, MaHocPhan = 101, MaLopHocPhanTruong = "396602_71KT6" };

        var ct1 = new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 4, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 15, PhongHoc = "507.H1" };
        var ct2 = new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 4, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 15, PhongHoc = "H3.43" };

        var boiCanh = new BoiCanhDanhGia
        {
            MaHocKy = 1,
            DanhSachLop = [lop1, lop2],
            LopTheoMa = new Dictionary<int, LopHocPhan> { [1] = lop1, [2] = lop2 },
            ChiTietTheoLop = new Dictionary<int, List<ChiTietThoiKhoaBieu>>
            {
                [1] = [ct1],
                [2] = [ct2]
            },
            GiangVienTheoMa = new Dictionary<int, GiangVien> { [10] = gv },
            NangLucTheoGv = new Dictionary<int, HashSet<int>> { [10] = [101] },
            NguyenVongTheoGv = new Dictionary<int, List<NguyenVongGiangVien>>
            {
                [10] =
                [
                    new NguyenVongGiangVien
                    {
                        MaHocKy = 1,
                        MaGiangVien = 10,
                        LoaiNguyenVong = "SC08",
                        Thu = 4,
                        TietBatDau = 4,
                        TietKetThuc = 6,
                        TrongSoPhat = 50.0,
                        TrangThai = "DaDuyet"
                    }
                ]
            }
        };

        var phanCong = new Dictionary<int, int?> { [1] = 10, [2] = 10 };
        var kq = boDanhGia.DanhGia(phanCong, boiCanh);

        Assert.Equal(0, kq.SoViPhamTrungLich);
        Assert.Equal(0, kq.SoViPhamCung);
        Assert.True(kq.DiemPhatNguyenVong >= 300.0); // 2 lớp * 50 * 3.0
        Assert.Contains(kq.DanhSachCanhBaoMem, w => w.Contains("xin nghỉ đột xuất"));
    }

    [Fact]
    public async Task Test_LayBangPhanCongAsync_ChiTamHoanVoiSC08_KhongTamHoanVoiSC01ToiSC07()
    {
        using var db = TaoInMemoryDb(nameof(Test_LayBangPhanCongAsync_ChiTamHoanVoiSC08_KhongTamHoanVoiSC01ToiSC07));
        db.HocKy.Add(new HocKy { MaHocKy = 1, MaNamHoc = 1, TenHocKy = "HK1" });
        db.HocPhan.Add(new HocPhan { MaHocPhan = 1, TenHocPhan = "Giải tích 2", SoTinChi = 3 });
        db.GiangVien.AddRange(
            new GiangVien { MaGiangVien = 10, HoTen = "GV Có SC01", TrangThai = "DangLamViec" },
            new GiangVien { MaGiangVien = 20, HoTen = "GV Có SC08", TrangThai = "DangLamViec" }
        );
        db.LopHocPhan.AddRange(
            new LopHocPhan { MaLopHocPhan = 1, MaHocKy = 1, MaHocPhan = 1, MaLopHocPhanTruong = "LOP_SC01" },
            new LopHocPhan { MaLopHocPhan = 2, MaHocKy = 1, MaHocPhan = 1, MaLopHocPhanTruong = "LOP_SC08" }
        );
        db.ChiTietThoiKhoaBieu.AddRange(
            new ChiTietThoiKhoaBieu { MaChiTiet = 1, MaLopHocPhan = 1, Thu = 2, TietBatDau = 1, TietKetThuc = 3, TuTuan = 1, DenTuan = 15 },
            new ChiTietThoiKhoaBieu { MaChiTiet = 2, MaLopHocPhan = 2, Thu = 4, TietBatDau = 4, TietKetThuc = 6, TuTuan = 1, DenTuan = 15 }
        );
        db.PhanCongGiangDay.AddRange(
            new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 1, MaGiangVien = 10, NguonPhanCong = "GA", TrangThai = "DaKhoa" },
            new PhanCongGiangDay { MaHocKy = 1, MaLopHocPhan = 2, MaGiangVien = 20, NguonPhanCong = "GA", TrangThai = "DaKhoa" }
        );
        db.NguyenVongGiangVien.AddRange(
            new NguyenVongGiangVien
            {
                MaHocKy = 1,
                MaGiangVien = 10,
                LoaiNguyenVong = "SC01",
                Thu = 2,
                TietBatDau = 1,
                TietKetThuc = 3,
                TrangThai = "DaDuyet",
                NoiDungGoc = "Tránh tiết 1-3"
            },
            new NguyenVongGiangVien
            {
                MaHocKy = 1,
                MaGiangVien = 20,
                LoaiNguyenVong = "SC08",
                Thu = 4,
                TietBatDau = 4,
                TietKetThuc = 6,
                TrangThai = "DaDuyet",
                NoiDungGoc = "[Tuần 12] Đi công tác"
            }
        );
        await db.SaveChangesAsync();

        var service = new XepLichService(db, new BoKiemTraRangBuoc());
        var bang = await service.LayBangPhanCongAsync(1);

        var lopSc01 = bang.DanhSachLop.Single(x => x.MaLopHocPhan == 1);
        var lopSc08 = bang.DanhSachLop.Single(x => x.MaLopHocPhan == 2);

        Assert.False(lopSc01.CoLichTamNghi);
        Assert.True(lopSc08.CoLichTamNghi);
        Assert.Equal("12", lopSc08.TuanTamNghi);
    }
}
