using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class NguyenVongService(AppDbContext context)
{
    public async Task<List<LichBanDto>> LayLichBanAsync(int maHocKy, int? maGiangVien = null)
    {
        var query = context.LichBanGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
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

    public async Task<List<NguyenVongDto>> LayNguyenVongAsync(int maHocKy, int? maGiangVien = null)
    {
        var query = context.NguyenVongGiangVien.Where(x => x.MaHocKy == maHocKy);
        if (maGiangVien.HasValue && maGiangVien.Value > 0)
        {
            query = query.Where(x => x.MaGiangVien == maGiangVien.Value);
        }

        var danhSach = await query.ToListAsync();
        var dsGvId = danhSach.Select(x => x.MaGiangVien).Distinct().ToList();
        var dsGiangVien = await context.GiangVien
            .Where(x => dsGvId.Contains(x.MaGiangVien))
            .ToDictionaryAsync(x => x.MaGiangVien);

        return danhSach.Select(x =>
        {
            dsGiangVien.TryGetValue(x.MaGiangVien, out var gv);
            var (maRb, tenRb) = PhanTichMaRangBuoc(x.LoaiNguyenVong, x.NoiDungGoc);
            return new NguyenVongDto
            {
                MaNguyenVong = x.MaNguyenVong,
                MaGiangVien = x.MaGiangVien,
                TenGiangVien = gv?.HoTen ?? $"GV #{x.MaGiangVien}",
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
        if (dto.MaHocKy <= 0) return null;

        // Xác định danh sách giảng viên áp dụng (hỗ trợ Seminar chọn nhiều GV)
        var dsGvId = (dto.DanhSachMaGiangVien != null && dto.DanhSachMaGiangVien.Count > 0)
            ? dto.DanhSachMaGiangVien.Distinct().Where(id => id > 0).ToList()
            : (dto.MaGiangVien > 0 ? new List<int> { dto.MaGiangVien } : new List<int>());

        if (dsGvId.Count == 0) return null;

        // Xác định khoảng Thứ: từ Thu đến DenThu
        int thuBatDau = dto.Thu ?? 2;
        int thuKetThuc = dto.DenThu.HasValue && dto.DenThu.Value >= thuBatDau ? dto.DenThu.Value : thuBatDau;

        bool laSeminar = string.Equals(dto.MaRangBuocMem?.Trim(), "SEMINAR", StringComparison.OrdinalIgnoreCase);

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

        if (laSeminar && !noiDung.Contains("[SEMINAR]"))
        {
            noiDung = string.IsNullOrWhiteSpace(noiDung) ? "[SEMINAR] Sinh hoạt chuyên môn Bộ môn" : $"[SEMINAR] {noiDung}".Trim();
        }

        double trongSo = laSeminar ? 999999.0 : (dto.TrongSoPhat > 0 ? dto.TrongSoPhat : 10.0);
        string loaiNv = laSeminar ? "SEMINAR" : (string.IsNullOrWhiteSpace(dto.MaRangBuocMem) ? "SC01" : dto.MaRangBuocMem.Trim());

        var entities = new List<NguyenVongGiangVien>();

        foreach (var gvId in dsGvId)
        {
            for (int thu = thuBatDau; thu <= thuKetThuc; thu++)
            {
                var entity = new NguyenVongGiangVien
                {
                    MaGiangVien = gvId,
                    MaHocKy = dto.MaHocKy,
                    LoaiNguyenVong = loaiNv,
                    MucDo = 1,
                    Thu = thu,
                    TietBatDau = dto.TietBatDau,
                    TietKetThuc = dto.TietKetThuc,
                    TrongSoPhat = trongSo,
                    NoiDungGoc = noiDung,
                    TrangThai = "ChoDuyet",
                    NgayTao = DateTime.Now
                };
                entities.Add(entity);
            }
        }

        context.NguyenVongGiangVien.AddRange(entities);
        await context.SaveChangesAsync();

        var firstEntity = entities.First();
        var gv = await context.GiangVien.FindAsync(firstEntity.MaGiangVien);
        var (maRb, tenRb) = PhanTichMaRangBuoc(firstEntity.LoaiNguyenVong, firstEntity.NoiDungGoc);

        return new NguyenVongDto
        {
            MaNguyenVong = firstEntity.MaNguyenVong,
            MaGiangVien = firstEntity.MaGiangVien,
            TenGiangVien = gv?.HoTen ?? $"GV #{firstEntity.MaGiangVien}",
            MaHocKy = firstEntity.MaHocKy,
            MaRangBuocMem = maRb,
            TenRangBuocMem = tenRb,
            LoaiNguyenVong = firstEntity.LoaiNguyenVong,
            MucDo = firstEntity.MucDo,
            Thu = firstEntity.Thu,
            TietBatDau = firstEntity.TietBatDau,
            TietKetThuc = firstEntity.TietKetThuc,
            TrongSoPhat = firstEntity.TrongSoPhat,
            NoiDungGoc = firstEntity.NoiDungGoc,
            LyDo = firstEntity.NoiDungGoc,
            TrangThai = firstEntity.TrangThai,
            NgayTao = firstEntity.NgayTao,
            NgayDuyet = firstEntity.NgayDuyet,
            GhiChuDuyet = firstEntity.GhiChuDuyet
        };
    }

    /// <summary>
    /// Trưởng bộ môn DUYỆT yêu cầu xin nghỉ / lịch bận của giảng viên:
    /// - Cập nhật trạng thái thành DaDuyet
    /// - Tự động đồng bộ sang LichBanGiangVien (Ràng buộc cứng H3)
    /// - TỰ ĐỘNG HỦY PHÂN CÔNG (KICK GIÁNG VIÊN RA) nếu GV đang được phân công dạy lớp học phần nào trùng vào ca này
    /// </summary>
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

        // Chỉ sinh Lịch bận cố định (H3) và hủy phân công nếu là đơn nghỉ CẢ HỌC KỲ!
        // Nếu chỉ xin nghỉ 1 hoặc 1 vài tuần (ví dụ: [Tuần 15] hoặc [Tuần 15-16]), đây là RÀNG BUỘC MỀM tạm hoãn,
        // TUYỆT ĐỐI KHÔNG sinh Lịch bận cố định H3 và KHÔNG kick giảng viên khỏi lớp.
        bool laNghiCaHocKy = !string.IsNullOrWhiteSpace(nv.NoiDungGoc) && (
            nv.NoiDungGoc.Contains("Cả học kỳ", StringComparison.OrdinalIgnoreCase) ||
            nv.NoiDungGoc.Contains("Tuần 1-20", StringComparison.OrdinalIgnoreCase) ||
            nv.NoiDungGoc.Contains("cả kỳ", StringComparison.OrdinalIgnoreCase));

        bool laSeminar = nv.LoaiNguyenVong == "SEMINAR";
        bool laNguyenVongNghiDay =
            laSeminar ||
            (((nv.LoaiNguyenVong == "SC01" ||
              nv.LoaiNguyenVong == "TranhDay" ||
              nv.LoaiNguyenVong == "XinNghi") &&
             laNghiCaHocKy));

        if (laNguyenVongNghiDay && nv.Thu.HasValue && nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            // 1. Đồng bộ sang Lịch bận cố định (H3) để thuật toán hoặc phân công tay không bao giờ xếp GV vào ca này
            var daCoLichBan = await context.LichBanGiangVien.AnyAsync(x =>
                x.MaGiangVien == nv.MaGiangVien &&
                x.MaHocKy == nv.MaHocKy &&
                x.Thu == nv.Thu.Value &&
                x.TietBatDau == nv.TietBatDau.Value &&
                x.TietKetThuc == nv.TietKetThuc.Value, cancellationToken);

            if (!daCoLichBan)
            {
                context.LichBanGiangVien.Add(new LichBanGiangVien
                {
                    MaGiangVien = nv.MaGiangVien,
                    MaHocKy = nv.MaHocKy,
                    Thu = nv.Thu.Value,
                    TietBatDau = nv.TietBatDau.Value,
                    TietKetThuc = nv.TietKetThuc.Value,
                    LyDo = string.IsNullOrWhiteSpace(nv.NoiDungGoc) ? "Lịch bận đã duyệt" : $"[Đã duyệt] {nv.NoiDungGoc}"
                });
            }

            // 2. KICK GIÁO VIÊN XIN NGHỈ RA KHỎI LỚP HỌC PHẦN TRÙNG CA ĐÓ (Hủy phân công hiện tại)
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
                        b.Thu == nv.Thu.Value &&
                        !(b.TietKetThuc < nv.TietBatDau.Value || b.TietBatDau > nv.TietKetThuc.Value));

                    if (trungCaNghi)
                    {
                        // Hủy phân công của giảng viên tại lớp này
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

    /// <summary>
    /// Trưởng bộ môn TỪ CHỐI yêu cầu xin nghỉ / lịch bận của giảng viên
    /// </summary>
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

        // Nếu trước đó đã sinh Lịch bận cố định, xóa bỏ để không bị chặn
        if (nv.Thu.HasValue && nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == nv.MaGiangVien &&
                x.MaHocKy == nv.MaHocKy &&
                x.Thu == nv.Thu.Value &&
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

        // Nếu có lịch bận tương ứng được sinh ra, xóa cùng
        if (item.Thu.HasValue && item.TietBatDau.HasValue && item.TietKetThuc.HasValue)
        {
            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == item.MaGiangVien &&
                x.MaHocKy == item.MaHocKy &&
                x.Thu == item.Thu.Value &&
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
            "SEMINAR" => ("SEMINAR", "Sinh hoạt chuyên môn / Seminar Bộ môn (Bắt buộc)"),
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
        // 1. Cập nhật Lịch bận cố định (BUSY)
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

        // 2. Cập nhật Nguyện vọng giảng dạy (PREFERRED / AVOID)
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
