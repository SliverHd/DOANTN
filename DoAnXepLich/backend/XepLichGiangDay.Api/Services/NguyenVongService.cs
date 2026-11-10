using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class NguyenVongService(AppDbContext context)
{
    public async Task<List<LichBanDto>> LayLichBanAsync(int maHocKy, int? maGiangVien = null, int? maBoMon = null)
    {
        var query = context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
        }
        else if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            var dsGvBm = await context.GiangVien
                .Where(g => g.MaBoMon == maBoMon.Value)
                .Select(g => g.MaGiangVien)
                .ToListAsync();
            query = query.Where(x => dsGvBm.Contains(x.MaGiangVien));
        }

        return await query.Select(x => new LichBanDto
        {
            MaLichBan = x.MaLichBan,
            MaGiangVien = x.MaGiangVien,
            MaHocKy = x.MaHocKy,
            Thu = x.Thu,
            TietBatDau = x.TietBatDau,
            TietKetThuc = x.TietKetThuc,
            LyDo = x.LyDo
        }).ToListAsync();
    }

    public async Task<List<NguyenVongDto>> LayNguyenVongAsync(int maHocKy, int? maGiangVien = null, int? maBoMon = null)
    {
        var query = context.NguyenVongGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
        }
        else if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            var dsGvBm = await context.GiangVien
                .Where(g => g.MaBoMon == maBoMon.Value)
                .Select(g => g.MaGiangVien)
                .ToListAsync();
            query = query.Where(x => dsGvBm.Contains(x.MaGiangVien));
        }

        var danhSach = await query.ToListAsync();
        var dsGvId = danhSach.Select(x => x.MaGiangVien).Distinct().ToList();
        var dsGiangVien = await context.GiangVien
            .Where(x => dsGvId.Contains(x.MaGiangVien))
            .ToDictionaryAsync(x => x.MaGiangVien);
        var dsBoMon = await context.BoMon.AsNoTracking().ToDictionaryAsync(x => x.MaBoMon);

        return danhSach.Select(x =>
        {
            dsGiangVien.TryGetValue(x.MaGiangVien, out var gv);
            string? tenBm = null;
            if (gv?.MaBoMon != null && dsBoMon.TryGetValue(gv.MaBoMon.Value, out var bm))
            {
                tenBm = bm.TenBoMon;
            }
            var (maRb, tenRb) = PhanTichMaRangBuoc(x.LoaiNguyenVong, x.NoiDungGoc);
            return new NguyenVongDto
            {
                MaNguyenVong = x.MaNguyenVong,
                MaGiangVien = x.MaGiangVien,
                TenGiangVien = gv?.HoTen ?? $"GV #{x.MaGiangVien}",
                MaBoMon = gv?.MaBoMon,
                TenBoMon = tenBm,
                MaHocKy = x.MaHocKy,
                MaRangBuocMem = maRb,
                TenRangBuocMem = tenRb,
                LoaiNguyenVong = x.LoaiNguyenVong,
                MucDo = x.MucDo,
                Thu = x.Thu,
                TietBatDau = x.TietBatDau,
                TietKetThuc = x.TietKetThuc,
                TrongSoPhat = x.TrongSoPhat,
                NoiDungGoc = x.NoiDungGoc,
                LyDo = x.NoiDungGoc,
                TrangThai = string.IsNullOrEmpty(x.TrangThai) ? "ChoDuyet" : x.TrangThai,
                NgayTao = x.NgayTao,
                NgayDuyet = x.NgayDuyet,
                GhiChuDuyet = x.GhiChuDuyet
            };
        }).OrderByDescending(x => x.NgayTao ?? DateTime.MinValue).ThenBy(x => x.TenGiangVien).ToList();
    }

    public async Task<NguyenVongDto?> ThemNguyenVongAsync(ThemNguyenVongDto dto)
    {
        if (dto.MaHocKy <= 0 || dto.MaGiangVien <= 0) return null;

        string noiDung = dto.LyDo?.Trim() ?? string.Empty;
        if (dto.TuTuan.HasValue && dto.DenTuan.HasValue)
        {
            if (dto.TuTuan.Value == 1 && dto.DenTuan.Value == 20)
            {
                noiDung = string.IsNullOrWhiteSpace(noiDung) ? "Cả học kỳ (Tuần 1-20)" : $"[Cả học kỳ] {noiDung}";
            }
            else if (dto.TuTuan.Value == dto.DenTuan.Value)
            {
                noiDung = string.IsNullOrWhiteSpace(noiDung)
                    ? $"Tạm nghỉ Tuần {dto.TuTuan}"
                    : $"[Tuần {dto.TuTuan}] {noiDung}";
            }
            else
            {
                noiDung = string.IsNullOrWhiteSpace(noiDung)
                    ? $"Tạm nghỉ Tuần {dto.TuTuan}-{dto.DenTuan}"
                    : $"[Tuần {dto.TuTuan}-{dto.DenTuan}] {noiDung}";
            }
        }

        var entity = new NguyenVongGiangVien
        {
            MaGiangVien = dto.MaGiangVien,
            MaHocKy = dto.MaHocKy,
            LoaiNguyenVong = string.IsNullOrWhiteSpace(dto.MaRangBuocMem) ? "SC01" : dto.MaRangBuocMem.Trim(),
            MucDo = 1,
            Thu = dto.Thu.HasValue && dto.Thu.Value > 0 ? dto.Thu.Value : null,
            TietBatDau = dto.TietBatDau,
            TietKetThuc = dto.TietKetThuc,
            TrongSoPhat = dto.TrongSoPhat > 0 ? dto.TrongSoPhat : 10.0,
            NoiDungGoc = noiDung,
            TrangThai = "ChoDuyet",
            NgayTao = DateTime.Now
        };

        context.NguyenVongGiangVien.Add(entity);
        await context.SaveChangesAsync();

        var gv = await context.GiangVien.FindAsync(dto.MaGiangVien);
        string? tenBm = null;
        if (gv?.MaBoMon != null)
        {
            var bm = await context.BoMon.FindAsync(gv.MaBoMon.Value);
            tenBm = bm?.TenBoMon;
        }
        var (maRb, tenRb) = PhanTichMaRangBuoc(entity.LoaiNguyenVong, entity.NoiDungGoc);

        return new NguyenVongDto
        {
            MaNguyenVong = entity.MaNguyenVong,
            MaGiangVien = entity.MaGiangVien,
            TenGiangVien = gv?.HoTen ?? $"GV #{entity.MaGiangVien}",
            MaBoMon = gv?.MaBoMon,
            TenBoMon = tenBm,
            MaHocKy = entity.MaHocKy,
            MaRangBuocMem = maRb,
            TenRangBuocMem = tenRb,
            LoaiNguyenVong = entity.LoaiNguyenVong,
            MucDo = entity.MucDo,
            Thu = entity.Thu,
            TietBatDau = entity.TietBatDau,
            TietKetThuc = entity.TietKetThuc,
            TrongSoPhat = entity.TrongSoPhat,
            NoiDungGoc = entity.NoiDungGoc,
            LyDo = entity.NoiDungGoc,
            TrangThai = entity.TrangThai,
            NgayTao = entity.NgayTao,
            NgayDuyet = entity.NgayDuyet,
            GhiChuDuyet = entity.GhiChuDuyet
        };
    }

    public async Task<KetQuaDuyetNguyenVongDto> DuyetNguyenVongAsync(int maNguyenVong, string? ghiChu = null, CancellationToken cancellationToken = default)
    {
        var nv = await context.NguyenVongGiangVien.FindAsync([maNguyenVong], cancellationToken);
        if (nv is null)
        {
            return new KetQuaDuyetNguyenVongDto
            {
                ThanhCong = false,
                ThongDiep = "Không tìm thấy yêu cầu nguyện vọng cần duyệt."
            };
        }

        nv.TrangThai = "DaDuyet";
        nv.NgayDuyet = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(ghiChu))
        {
            nv.GhiChuDuyet = ghiChu.Trim();
        }

        var danhSachLopBiHuy = new List<string>();

        bool laNghiCaHocKy = !string.IsNullOrWhiteSpace(nv.NoiDungGoc) && (
            nv.NoiDungGoc.Contains("Cả học kỳ", StringComparison.OrdinalIgnoreCase) ||
            nv.NoiDungGoc.Contains("Tuần 1-20", StringComparison.OrdinalIgnoreCase) ||
            nv.NoiDungGoc.Contains("cả kỳ", StringComparison.OrdinalIgnoreCase));

        bool laNguyenVongNghiDay =
            (nv.LoaiNguyenVong == "SC01" ||
             nv.LoaiNguyenVong == "TranhDay" ||
             nv.LoaiNguyenVong == "XinNghi") &&
            laNghiCaHocKy;

        if (laNguyenVongNghiDay && nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            var cacThuApDung = (nv.Thu.HasValue && nv.Thu.Value > 0)
                ? new List<int> { nv.Thu.Value }
                : new List<int> { 2, 3, 4, 5, 6, 7, 8 };

            foreach (var thuApDung in cacThuApDung)
            {
                var daCoLichBan = await context.LichBanGiangVien.AnyAsync(x =>
                    x.MaGiangVien == nv.MaGiangVien &&
                    x.MaHocKy == nv.MaHocKy &&
                    x.Thu == thuApDung &&
                    x.TietBatDau == nv.TietBatDau.Value &&
                    x.TietKetThuc == nv.TietKetThuc.Value, cancellationToken);

                if (!daCoLichBan)
                {
                    context.LichBanGiangVien.Add(new LichBanGiangVien
                    {
                        MaGiangVien = nv.MaGiangVien,
                        MaHocKy = nv.MaHocKy,
                        Thu = thuApDung,
                        TietBatDau = nv.TietBatDau.Value,
                        TietKetThuc = nv.TietKetThuc.Value,
                        LyDo = string.IsNullOrWhiteSpace(nv.NoiDungGoc) ? "Lịch bận đã duyệt" : $"[Đã duyệt] {nv.NoiDungGoc}"
                    });
                }
            }

            var dsPhanCongGv = await context.PhanCongGiangDay
                .Where(x => x.MaHocKy == nv.MaHocKy && x.MaGiangVien == nv.MaGiangVien)
                .ToListAsync(cancellationToken);

            var dsLopId = dsPhanCongGv.Select(x => x.MaLopHocPhan).ToList();
            var dsBuoi = await context.ChiTietThoiKhoaBieu
                .Where(x => dsLopId.Contains(x.MaLopHocPhan))
                .ToListAsync(cancellationToken);
            var buoiTheoLop = dsBuoi.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.ToList());

            var dsLopHp = await context.LopHocPhan
                .Where(x => dsLopId.Contains(x.MaLopHocPhan))
                .ToDictionaryAsync(x => x.MaLopHocPhan, cancellationToken);

            foreach (var pc in dsPhanCongGv)
            {
                if (buoiTheoLop.TryGetValue(pc.MaLopHocPhan, out var buoiCuaLop))
                {
                    bool trungCaNghi = buoiCuaLop.Any(b =>
                        cacThuApDung.Contains(b.Thu) &&
                        !(b.TietKetThuc < nv.TietBatDau.Value || b.TietBatDau > nv.TietKetThuc.Value));

                    if (trungCaNghi)
                    {
                        context.PhanCongGiangDay.Remove(pc);

                        string tenLop = dsLopHp.TryGetValue(pc.MaLopHocPhan, out var lop)
                            ? $"{lop.MaLopHocPhanTruong} ({lop.TenLop})"
                            : $"Lớp #{pc.MaLopHocPhan}";
                        danhSachLopBiHuy.Add(tenLop);
                    }
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var gv = await context.GiangVien.FindAsync([nv.MaGiangVien], cancellationToken);
        var tenGv = gv?.HoTen ?? $"GV #{nv.MaGiangVien}";
        string? tenBm = null;
        if (gv?.MaBoMon != null)
        {
            var bm = await context.BoMon.FindAsync([gv.MaBoMon.Value], cancellationToken);
            tenBm = bm?.TenBoMon;
        }

        string thongDiep = danhSachLopBiHuy.Count > 0
            ? $"Đã duyệt đơn xin nghỉ của {tenGv}. Đã hủy phân công của giảng viên tại {danhSachLopBiHuy.Count} lớp trùng ca: {string.Join(", ", danhSachLopBiHuy)}. Các lớp này hiện ở trạng thái Chưa phân công để xếp giảng viên khác."
            : $"Đã duyệt đơn xin nghỉ của {tenGv} thành công. (Giảng viên chưa có lớp nào được phân công trùng vào ca này).";

        var (maRb, tenRb) = PhanTichMaRangBuoc(nv.LoaiNguyenVong, nv.NoiDungGoc);

        return new KetQuaDuyetNguyenVongDto
        {
            ThanhCong = true,
            ThongDiep = thongDiep,
            SoLopBiHuy = danhSachLopBiHuy.Count,
            DanhSachLopBiHuy = danhSachLopBiHuy,
            NguyenVong = new NguyenVongDto
            {
                MaNguyenVong = nv.MaNguyenVong,
                MaGiangVien = nv.MaGiangVien,
                TenGiangVien = tenGv,
                MaBoMon = gv?.MaBoMon,
                TenBoMon = tenBm,
                MaHocKy = nv.MaHocKy,
                MaRangBuocMem = maRb,
                TenRangBuocMem = tenRb,
                LoaiNguyenVong = nv.LoaiNguyenVong,
                MucDo = nv.MucDo,
                Thu = nv.Thu,
                TietBatDau = nv.TietBatDau,
                TietKetThuc = nv.TietKetThuc,
                TrongSoPhat = nv.TrongSoPhat,
                NoiDungGoc = nv.NoiDungGoc,
                LyDo = nv.NoiDungGoc,
                TrangThai = nv.TrangThai,
                NgayTao = nv.NgayTao,
                NgayDuyet = nv.NgayDuyet,
                GhiChuDuyet = nv.GhiChuDuyet
            }
        };
    }

    public async Task<KetQuaDuyetNguyenVongDto> TuChoiNguyenVongAsync(int maNguyenVong, string? ghiChu = null, CancellationToken cancellationToken = default)
    {
        var nv = await context.NguyenVongGiangVien.FindAsync([maNguyenVong], cancellationToken);
        if (nv is null)
        {
            return new KetQuaDuyetNguyenVongDto
            {
                ThanhCong = false,
                ThongDiep = "Không tìm thấy yêu cầu nguyện vọng cần từ chối."
            };
        }

        nv.TrangThai = "TuChoi";
        nv.NgayDuyet = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(ghiChu))
        {
            nv.GhiChuDuyet = ghiChu.Trim();
        }

        if (nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            var cacThuCanXoa = (nv.Thu.HasValue && nv.Thu.Value > 0)
                ? new List<int> { nv.Thu.Value }
                : new List<int> { 2, 3, 4, 5, 6, 7, 8 };

            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == nv.MaGiangVien &&
                x.MaHocKy == nv.MaHocKy &&
                cacThuCanXoa.Contains(x.Thu) &&
                x.TietBatDau == nv.TietBatDau.Value &&
                x.TietKetThuc == nv.TietKetThuc.Value).ToListAsync(cancellationToken);
            if (dsLbCu.Count > 0)
            {
                context.LichBanGiangVien.RemoveRange(dsLbCu);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var gv = await context.GiangVien.FindAsync([nv.MaGiangVien], cancellationToken);
        var tenGv = gv?.HoTen ?? $"GV #{nv.MaGiangVien}";

        return new KetQuaDuyetNguyenVongDto
        {
            ThanhCong = true,
            ThongDiep = $"Đã từ chối yêu cầu của giảng viên {tenGv}.",
            SoLopBiHuy = 0,
            DanhSachLopBiHuy = []
        };
    }

    public async Task<bool> XoaNguyenVongAsync(int maNguyenVong)
    {
        var item = await context.NguyenVongGiangVien.FindAsync(maNguyenVong);
        if (item is null) return false;

        if (item.TietBatDau.HasValue && item.TietKetThuc.HasValue)
        {
            var cacThuCanXoa = (item.Thu.HasValue && item.Thu.Value > 0)
                ? new List<int> { item.Thu.Value }
                : new List<int> { 2, 3, 4, 5, 6, 7, 8 };

            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == item.MaGiangVien &&
                x.MaHocKy == item.MaHocKy &&
                cacThuCanXoa.Contains(x.Thu) &&
                x.TietBatDau == item.TietBatDau.Value &&
                x.TietKetThuc == item.TietKetThuc.Value).ToListAsync();
            if (dsLbCu.Count > 0)
            {
                context.LichBanGiangVien.RemoveRange(dsLbCu);
            }
        }

        context.NguyenVongGiangVien.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    public static (string MaRb, string TenRb) PhanTichMaRangBuoc(string? loai, string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(loai)) return ("SC01", "Tránh khung giờ / tiết bận cụ thể");

        return loai.Trim() switch
        {
            "SC01" or "TranhDay" => ("SC01", "Tránh khung giờ / tiết bận cụ thể"),
            "SC02" => ("SC02", "Tránh dạy ca tối (Tiết 10 - 12)"),
            "SC03" => ("SC03", "Tránh dạy ngày Thứ Bảy / Chủ Nhật"),
            "SC04" or "ThichDay" => ("SC04", "Ưu tiên dạy buổi sáng (Tiết 1 - 6)"),
            "SC05" => ("SC05", "Ưu tiên dạy buổi chiều (Tiết 7 - 12)"),
            "SC06" => ("SC06", "Giới hạn số tiết tối đa / ngày"),
            "SC07" => ("SC07", "Hạn chế số ngày dạy trong tuần (gom lịch)"),
            _ => (loai, "Nguyện vọng giảng viên")
        };
    }

    public async Task<bool> LuuMaTranAsync(LuuMaTranNguyenVongRequest request)
    {
        var oldLichBan = await context.LichBanGiangVien
            .Where(x => x.MaGiangVien == request.MaGiangVien && x.MaHocKy == request.MaHocKy)
            .ToListAsync();
        context.LichBanGiangVien.RemoveRange(oldLichBan);

        foreach (var lb in request.DanhSachLichBan)
        {
            context.LichBanGiangVien.Add(new LichBanGiangVien
            {
                MaGiangVien = request.MaGiangVien,
                MaHocKy = request.MaHocKy,
                Thu = lb.Thu,
                TietBatDau = lb.TietBatDau,
                TietKetThuc = lb.TietKetThuc,
                LyDo = lb.LyDo ?? "Bận"
            });
        }

        var oldNguyenVong = await context.NguyenVongGiangVien
            .Where(x => x.MaGiangVien == request.MaGiangVien && x.MaHocKy == request.MaHocKy)
            .ToListAsync();
        context.NguyenVongGiangVien.RemoveRange(oldNguyenVong);

        foreach (var nv in request.DanhSachNguyenVong)
        {
            context.NguyenVongGiangVien.Add(new NguyenVongGiangVien
            {
                MaGiangVien = request.MaGiangVien,
                MaHocKy = request.MaHocKy,
                LoaiNguyenVong = nv.LoaiNguyenVong,
                MucDo = nv.MucDo > 0 ? nv.MucDo : 1,
                Thu = nv.Thu,
                TietBatDau = nv.TietBatDau,
                TietKetThuc = nv.TietKetThuc,
                TrongSoPhat = nv.LoaiNguyenVong == "TranhDay" ? 10.0 : 0.0,
                NoiDungGoc = nv.GhiChu,
                TrangThai = "ChoDuyet",
                NgayTao = DateTime.Now
            });
        }

        await context.SaveChangesAsync();
        return true;
    }
}
