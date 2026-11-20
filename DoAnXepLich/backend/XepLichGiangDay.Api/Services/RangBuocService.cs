using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.RangBuoc;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services;

public class RangBuocService(
    AppDbContext context,
    BoKiemTraRangBuoc boKiemTra,
    TaoDanhSachUngVien taoUngVien,
    BoDanhGiaPhuongAn boDanhGia)
{
    public async Task<List<RangBuocHocKyDto>> LayDanhSachRangBuocAsync(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var ds = await context.RangBuocHocKy
            .Where(x => x.MaHocKy == maHocKy)
            .ToListAsync();

        if (ds.Count == 0)
        {
            // Tự động khởi tạo 8 ràng buộc mẫu chuẩn cho học kỳ
            var danhSachMau = new List<RangBuocHocKy>
            {
                new() { MaHocKy = maHocKy, TenRangBuoc = "Đúng chuyên môn giảng dạy", LoaiRangBuoc = "Cung", TrongSoPhat = 1000, MoTa = "Giảng viên chỉ dạy các học phần thuộc năng lực chuyên môn" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Không trùng lịch dạy", LoaiRangBuoc = "Cung", TrongSoPhat = 1000, MoTa = "Một giảng viên không dạy 2 lớp trùng thứ và tiết trong cùng tuần" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Tránh lịch bận của giảng viên", LoaiRangBuoc = "Cung", TrongSoPhat = 1000, MoTa = "Không phân công vào các buổi giảng viên đã đăng ký bận" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Phủ kín các lớp học phần", LoaiRangBuoc = "Cung", TrongSoPhat = 1000, MoTa = "Tất cả các lớp học phần mở trong kỳ đều phải có giảng viên" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Thỏa mãn nguyện vọng thời gian", LoaiRangBuoc = "Mem", TrongSoPhat = 10, MoTa = "Ưu tiên xếp lớp theo thứ và buổi giảng viên mong muốn" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Gom lịch & hạn chế tiết trống", LoaiRangBuoc = "Mem", TrongSoPhat = 8, MoTa = "Ưu tiên gom lớp vào các buổi liền kề, tránh đi dạy rải rác" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Cân bằng định mức số tiết", LoaiRangBuoc = "Mem", TrongSoPhat = 15, MoTa = "Số tiết phân công nằm trong khoảng định mức tối thiểu và tối đa" },
                new() { MaHocKy = maHocKy, TenRangBuoc = "Ưu tiên giảng viên cơ hữu", LoaiRangBuoc = "Mem", TrongSoPhat = 5, MoTa = "Ưu tiên phân công giảng viên cơ hữu đủ tải trước thỉnh giảng" }
            };

            context.RangBuocHocKy.AddRange(danhSachMau);
            await context.SaveChangesAsync();
            ds = danhSachMau;
        }

        return ds.Select(x => new RangBuocHocKyDto
        {
            MaRangBuoc = x.MaRangBuoc,
            MaHocKy = x.MaHocKy,
            TenRangBuoc = x.TenRangBuoc,
            LoaiRangBuoc = x.LoaiRangBuoc,
            TrongSoPhat = x.TrongSoPhat,
            MoTa = x.MoTa
        }).ToList();
    }

    public async Task<bool> CapNhatTrongSoAsync(int id, double trongSoPhat)
    {
        if (trongSoPhat < 0)
        {
            throw new ArgumentException("Trong so phat phai lon hon hoac bang 0.");
        }

        var rb = await context.RangBuocHocKy.FindAsync(id);
        if (rb == null) return false;

        rb.TrongSoPhat = trongSoPhat;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DinhMucGiangVienDto>> LayDanhSachDinhMucAsync(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var dsGiangVien = await context.GiangVien.Where(x => x.TrangThai != "NghiViec").ToListAsync();
        var dsDinhMuc = await context.DinhMucGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync();

        return dsGiangVien.Select(gv =>
        {
            var dm = dsDinhMuc.FirstOrDefault(x => x.MaGiangVien == gv.MaGiangVien);
            bool isThinhGiang = (gv.ChucDanh != null && gv.ChucDanh.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase)) ||
                                gv.HoTen.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase);
            bool isTroGiang = (gv.ChucDanh != null && gv.ChucDanh.Contains("trợ giảng", StringComparison.OrdinalIgnoreCase)) ||
                              gv.HoTen.Contains("Trợ giảng", StringComparison.OrdinalIgnoreCase);
            int minMacDinh = isTroGiang ? 0 : (isThinhGiang ? 3 : 6);
            int maxMacDinh = isTroGiang ? 12 : (isThinhGiang ? 16 : 25);

            return new DinhMucGiangVienDto
            {
                MaDinhMuc = dm?.MaDinhMuc ?? 0,
                MaGiangVien = gv.MaGiangVien,
                TenGiangVien = gv.HoTen,
                Email = gv.Email,
                MaHocKy = maHocKy,
                SoTietToiThieu = dm?.SoTietToiThieu ?? minMacDinh,
                SoTietToiDa = dm?.SoTietToiDa ?? maxMacDinh
            };
        }).ToList();
    }

    public async Task<bool> CapNhatDinhMucAsync(CapNhatDinhMucDto dto)
    {
        if (dto.MaHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        if (dto.SoTietToiThieu < 0)
        {
            throw new ArgumentException("So tiet toi thieu phai lon hon hoac bang 0.");
        }

        if (dto.SoTietToiDa < dto.SoTietToiThieu)
        {
            throw new ArgumentException("So tiet toi da phai lon hon hoac bang so tiet toi thieu.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == dto.MaHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var gvTonTai = await context.GiangVien.AnyAsync(x => x.MaGiangVien == dto.MaGiangVien);
        if (!gvTonTai)
        {
            throw new ArgumentException("Giang vien khong ton tai trong he thong.");
        }

        var dm = await context.DinhMucGiangVien
            .FirstOrDefaultAsync(x => x.MaGiangVien == dto.MaGiangVien && x.MaHocKy == dto.MaHocKy);

        if (dm != null)
        {
            dm.SoTietToiThieu = dto.SoTietToiThieu;
            dm.SoTietToiDa = dto.SoTietToiDa;
        }
        else
        {
            context.DinhMucGiangVien.Add(new DinhMucGiangVien
            {
                MaGiangVien = dto.MaGiangVien,
                MaHocKy = dto.MaHocKy,
                SoTietToiThieu = dto.SoTietToiThieu,
                SoTietToiDa = dto.SoTietToiDa,
                TrangThai = "DangApDung"
            });
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApDungDinhMucChungAsync(ApDungDinhMucChungDto dto)
    {
        if (dto.MaHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        if (dto.SoTietToiThieu < 0)
        {
            throw new ArgumentException("So tiet toi thieu phai lon hon hoac bang 0.");
        }

        if (dto.SoTietToiDa < dto.SoTietToiThieu)
        {
            throw new ArgumentException("So tiet toi da phai lon hon hoac bang so tiet toi thieu.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == dto.MaHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var dsGiangVien = await context.GiangVien.Where(x => x.TrangThai != "NghiViec").ToListAsync();
        var dsDinhMuc = await context.DinhMucGiangVien.Where(x => x.MaHocKy == dto.MaHocKy).ToListAsync();

        foreach (var gv in dsGiangVien)
        {
            var dm = dsDinhMuc.FirstOrDefault(x => x.MaGiangVien == gv.MaGiangVien);
            if (dm != null)
            {
                dm.SoTietToiThieu = dto.SoTietToiThieu;
                dm.SoTietToiDa = dto.SoTietToiDa;
            }
            else
            {
                context.DinhMucGiangVien.Add(new DinhMucGiangVien
                {
                    MaGiangVien = gv.MaGiangVien,
                    MaHocKy = dto.MaHocKy,
                    SoTietToiThieu = dto.SoTietToiThieu,
                    SoTietToiDa = dto.SoTietToiDa,
                    TrangThai = "DangApDung"
                });
            }
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<KetQuaKiemTraRangBuoc> KiemTraRangBuocCungHocKyAsync(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var dsLop = await context.LopHocPhan.Where(x => x.MaHocKy == maHocKy).ToListAsync();
        var dsMaLop = dsLop.Select(x => x.MaLopHocPhan).ToList();
        var dsChiTiet = await context.ChiTietThoiKhoaBieu.Where(x => dsMaLop.Contains(x.MaLopHocPhan)).ToListAsync();
        var dsPhanCong = await context.PhanCongGiangDay.Where(x => x.MaHocKy == maHocKy).ToListAsync();
        var dsNangLuc = await context.NangLucGiangDay.ToListAsync();
        var dsLichBan = await context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy).ToListAsync();
        var dsGiangVien = await context.GiangVien.ToListAsync();

        var dictPhanCong = new Dictionary<int, int?>();
        foreach (var lop in dsLop)
        {
            var pc = dsPhanCong.FirstOrDefault(x => x.MaLopHocPhan == lop.MaLopHocPhan);
            dictPhanCong[lop.MaLopHocPhan] = pc != null ? pc.MaGiangVien : null;
        }

        return boKiemTra.KiemTraToanBoPhuongAn(dictPhanCong, dsLop, dsChiTiet, dsNangLuc, dsLichBan, dsGiangVien);
    }

    public async Task<List<UngVienLopHocPhanDto>> LayDanhSachUngVienChiTietAsync(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        return await taoUngVien.LayDanhSachUngVienChiTietAsync(maHocKy);
    }

    public async Task<KetQuaDanhGiaDto> DanhGiaPhuongAnHienTaiAsync(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            throw new ArgumentException("Ma hoc ky khong hop le.");
        }

        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var boiCanh = await boDanhGia.TaoBoiCanhAsync(maHocKy);
        var dsPhanCong = await context.PhanCongGiangDay.Where(x => x.MaHocKy == maHocKy).ToListAsync();

        var dictPhanCong = new Dictionary<int, int?>();
        foreach (var lop in boiCanh.DanhSachLop)
        {
            var pc = dsPhanCong.FirstOrDefault(x => x.MaLopHocPhan == lop.MaLopHocPhan);
            dictPhanCong[lop.MaLopHocPhan] = pc?.MaGiangVien;
        }

        var ketQua = boDanhGia.DanhGia(dictPhanCong, boiCanh);

        return new KetQuaDanhGiaDto
        {
            TongDiemPhat = ketQua.TongDiemPhat,
            DoThichNghi = ketQua.DoThichNghi,
            DiemPhatRangBuocCung = ketQua.DiemPhatRangBuocCung,
            DiemPhatNguyenVong = ketQua.DiemPhatNguyenVong,
            DiemPhatCanBangTai = ketQua.DiemPhatCanBangTai,
            DiemPhatGomLich = ketQua.DiemPhatGomLich,
            SoViPhamCung = ketQua.SoViPhamChuyenMon + ketQua.SoViPhamTrungLich + ketQua.SoViPhamLichBan + ketQua.SoLopChuaPhanCong,
            HopLe = ketQua.HopLe,
            DanhSachViPham = ketQua.DanhSachViPham
        };
    }
}

