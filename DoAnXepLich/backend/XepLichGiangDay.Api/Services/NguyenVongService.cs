using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class NguyenVongService(AppDbContext context)
{
    public async Task<List<LichBanDto>> LayLichBanAsync(int maHocKy, int? maGiangVien = null, int? maBoMon = null, bool? chiSeminar = null)
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

        var danhSach = await query.ToListAsync();
        var dsGvId = danhSach.Select(x => x.MaGiangVien).Distinct().ToList();
        var dsGiangVien = await context.GiangVien
            .Where(x => dsGvId.Contains(x.MaGiangVien))
            .ToDictionaryAsync(x => x.MaGiangVien);
        var dsBoMon = await context.BoMon.AsNoTracking().ToDictionaryAsync(x => x.MaBoMon);

        var ketQua = danhSach.Select(x =>
        {
            dsGiangVien.TryGetValue(x.MaGiangVien, out var gv);
            string? tenBm = null;
            if (gv?.MaBoMon != null && dsBoMon.TryGetValue(gv.MaBoMon.Value, out var bm))
            {
                tenBm = bm.TenBoMon;
            }
            bool laSeminar = !string.IsNullOrWhiteSpace(x.LyDo) &&
                             x.LyDo.Contains("Seminar", StringComparison.OrdinalIgnoreCase);

            return new LichBanDto
            {
                MaLichBan = x.MaLichBan,
                MaGiangVien = x.MaGiangVien,
                TenGiangVien = gv?.HoTen ?? $"GV #{x.MaGiangVien}",
                MaBoMon = gv?.MaBoMon,
                TenBoMon = tenBm,
                MaHocKy = x.MaHocKy,
                Thu = x.Thu,
                TietBatDau = x.TietBatDau,
                TietKetThuc = x.TietKetThuc,
                LyDo = x.LyDo,
                LaSeminar = laSeminar
            };
        });

        if (chiSeminar.HasValue)
        {
            ketQua = ketQua.Where(x => x.LaSeminar == chiSeminar.Value);
        }

        return ketQua
            .OrderBy(x => x.Thu)
            .ThenBy(x => x.TietBatDau)
            .ThenBy(x => x.TenGiangVien)
            .ToList();
    }

    public async Task<KetQuaTaoLichSeminarDto> TaoLichSeminarAsync(TaoLichSeminarRequest req, CancellationToken cancellationToken = default)
    {
        var dsGvIds = req.DanhSachMaGiangVien
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (req.MaHocKy <= 0 || dsGvIds.Count == 0 || req.Thu < 2 || req.Thu > 8 ||
            req.TietBatDau < 1 || req.TietKetThuc > 15 || req.TietBatDau > req.TietKetThuc)
        {
            return new KetQuaTaoLichSeminarDto
            {
                ThanhCong = false,
                ThongDiep = "Dữ liệu lịch Seminar không hợp lệ (kiểm tra học kỳ, thứ 2-8, tiết 1-15 và danh sách giảng viên)."
            };
        }

        string tenSeminar = string.IsNullOrWhiteSpace(req.TenSeminar)
            ? "Seminar Bộ môn"
            : req.TenSeminar.Trim();
        string lyDoChuan = tenSeminar.StartsWith("[Seminar]", StringComparison.OrdinalIgnoreCase)
            ? tenSeminar
            : $"[Seminar] {tenSeminar}";

        var dsLbHienCo = await context.LichBanGiangVien
            .Where(x => x.MaHocKy == req.MaHocKy && dsGvIds.Contains(x.MaGiangVien))
            .ToListAsync(cancellationToken);

        int soDaKhoa = 0;
        int soDaTonTai = 0;

        foreach (var maGv in dsGvIds)
        {
            var daCoSeminarTrung = dsLbHienCo.FirstOrDefault(x =>
                x.MaGiangVien == maGv &&
                x.Thu == req.Thu &&
                x.TietBatDau == req.TietBatDau &&
                x.TietKetThuc == req.TietKetThuc &&
                !string.IsNullOrWhiteSpace(x.LyDo) &&
                x.LyDo.Contains("Seminar", StringComparison.OrdinalIgnoreCase));

            if (daCoSeminarTrung != null)
            {
                daCoSeminarTrung.LyDo = lyDoChuan;
                soDaTonTai++;
            }
            else
            {
                context.LichBanGiangVien.Add(new LichBanGiangVien
                {
                    MaGiangVien = maGv,
                    MaHocKy = req.MaHocKy,
                    Thu = req.Thu,
                    TietBatDau = req.TietBatDau,
                    TietKetThuc = req.TietKetThuc,
                    LyDo = lyDoChuan
                });
                soDaKhoa++;
            }
        }

        // Quét toàn bộ phân công hiện tại (kể cả DaKhoa hay DuKien) trùng giờ Seminar -> gỡ phân công để khóa chết cứng 100%
        var dsPhanCongTrungGv = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == req.MaHocKy && dsGvIds.Contains(x.MaGiangVien))
            .ToListAsync(cancellationToken);

        var danhSachLopBiGo = new List<string>();
        if (dsPhanCongTrungGv.Count > 0)
        {
            var dsLopIds = dsPhanCongTrungGv.Select(x => x.MaLopHocPhan).Distinct().ToList();
            var dsBuoiHoc = await context.ChiTietThoiKhoaBieu
                .Where(x => dsLopIds.Contains(x.MaLopHocPhan))
                .ToListAsync(cancellationToken);
            var buoiTheoLop = dsBuoiHoc.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.ToList());

            var dsLop = await context.LopHocPhan
                .Where(x => dsLopIds.Contains(x.MaLopHocPhan))
                .ToDictionaryAsync(x => x.MaLopHocPhan, cancellationToken);
            var dsGv = await context.GiangVien
                .Where(x => dsGvIds.Contains(x.MaGiangVien))
                .ToDictionaryAsync(x => x.MaGiangVien, cancellationToken);

            foreach (var pc in dsPhanCongTrungGv)
            {
                if (buoiTheoLop.TryGetValue(pc.MaLopHocPhan, out var buoiCuaLop))
                {
                    bool trungKhungSeminar = buoiCuaLop.Any(b =>
                        b.Thu == req.Thu &&
                        Math.Max(b.TietBatDau, req.TietBatDau) <= Math.Min(b.TietKetThuc, req.TietKetThuc));

                    if (trungKhungSeminar)
                    {
                        context.PhanCongGiangDay.Remove(pc);
                        string tenLop = dsLop.TryGetValue(pc.MaLopHocPhan, out var lop)
                            ? $"{lop.MaLopHocPhanTruong} ({lop.TenLop})"
                            : $"Lớp #{pc.MaLopHocPhan}";
                        string tenGv = dsGv.TryGetValue(pc.MaGiangVien, out var gv)
                            ? gv.HoTen
                            : $"GV #{pc.MaGiangVien}";
                        danhSachLopBiGo.Add($"{tenLop} — đã gỡ {tenGv}");
                    }
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        string thuStr = req.Thu == 8 ? "Chủ nhật" : $"Thứ {req.Thu}";
        string thongDiep = $"Đã khóa chết cứng lịch {lyDoChuan} vào {thuStr} (Tiết {req.TietBatDau}-{req.TietKetThuc}) cho {soDaKhoa} giảng viên" +
            (soDaTonTai > 0 ? $" (cập nhật {soDaTonTai} giảng viên đã có lịch này trước đó)" : "") +
            (danhSachLopBiGo.Count > 0 ? $". Đã tự động gỡ phân công {danhSachLopBiGo.Count} lớp học phần trùng giờ Seminar." : ".");

        return new KetQuaTaoLichSeminarDto
        {
            ThanhCong = true,
            ThongDiep = thongDiep,
            SoGiangVienDaKhoa = soDaKhoa,
            SoGiangVienDaTonTai = soDaTonTai,
            SoLopBiGo = danhSachLopBiGo.Count,
            DanhSachLopBiGo = danhSachLopBiGo
        };
    }

    public async Task<bool> XoaLichBanAsync(int maLichBan, CancellationToken cancellationToken = default)
    {
        var item = await context.LichBanGiangVien.FindAsync([maLichBan], cancellationToken);
        if (item is null) return false;

        context.LichBanGiangVien.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> XoaNhomSeminarAsync(XoaNhomSeminarRequest req, CancellationToken cancellationToken = default)
    {
        var query = context.LichBanGiangVien.Where(x =>
            x.MaHocKy == req.MaHocKy &&
            x.Thu == req.Thu &&
            x.TietBatDau == req.TietBatDau &&
            x.TietKetThuc == req.TietKetThuc);

        if (!string.IsNullOrWhiteSpace(req.LyDo))
        {
            var lyDoTrim = req.LyDo.Trim();
            query = query.Where(x => x.LyDo == lyDoTrim);
        }
        else
        {
            query = query.Where(x => x.LyDo != null && x.LyDo.Contains("Seminar"));
        }

        if (req.MaBoMon.HasValue && req.MaBoMon.Value > 0)
        {
            var dsGvBm = await context.GiangVien
                .Where(g => g.MaBoMon == req.MaBoMon.Value)
                .Select(g => g.MaGiangVien)
                .ToListAsync(cancellationToken);
            query = query.Where(x => dsGvBm.Contains(x.MaGiangVien));
        }

        var items = await query.ToListAsync(cancellationToken);
        if (items.Count == 0) return 0;

        context.LichBanGiangVien.RemoveRange(items);
        await context.SaveChangesAsync(cancellationToken);
        return items.Count;
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
        var dsLichBanNonSeminar = await context.LichBanGiangVien
            .AsNoTracking()
            .Where(x => x.MaHocKy == maHocKy && (x.LyDo == null || !x.LyDo.Contains("Seminar")))
            .ToListAsync();
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
            var trangThai = string.IsNullOrEmpty(x.TrangThai) ? "ChoDuyet" : x.TrangThai;
            bool khoaCungLichBan = KiemTraKhoaCungLichBan(x, trangThai, dsLichBanNonSeminar);
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
                KhoaCungLichBan = khoaCungLichBan,
                Thu = x.Thu,
                TietBatDau = x.TietBatDau,
                TietKetThuc = x.TietKetThuc,
                TrongSoPhat = x.TrongSoPhat,
                NoiDungGoc = x.NoiDungGoc,
                LyDo = x.NoiDungGoc,
                TrangThai = trangThai,
                NgayTao = x.NgayTao,
                NgayDuyet = x.NgayDuyet,
                GhiChuDuyet = x.GhiChuDuyet
            };
        }).OrderByDescending(x => x.NgayTao ?? DateTime.MinValue).ThenBy(x => x.TenGiangVien).ToList();
    }

    public async Task<NguyenVongDto?> ThemNguyenVongAsync(ThemNguyenVongDto dto)
    {
        if (dto.MaHocKy <= 0 || dto.MaGiangVien <= 0) return null;

        string loaiNv = string.IsNullOrWhiteSpace(dto.MaRangBuocMem) ? "SC01" : dto.MaRangBuocMem.Trim();
        bool khoaCungLichBan = LaLoaiTranhGio(loaiNv) && dto.KhoaCungLichBan;

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
            LoaiNguyenVong = loaiNv,
            MucDo = khoaCungLichBan ? 2 : 1,
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
            KhoaCungLichBan = khoaCungLichBan,
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

        bool laNguyenVongNghiDay =
            LaLoaiTranhGio(nv.LoaiNguyenVong) &&
            (nv.MucDo >= 2 || (nv.MucDo == 0 && LaNoiDungCaHocKy(nv.NoiDungGoc)));

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

        string thongDiep;
        if (!laNguyenVongNghiDay)
        {
            thongDiep = $"Đã duyệt nguyện vọng mềm của {tenGv} thành công (áp dụng điểm phạt khi xếp lịch, không khóa cứng lịch bận).";
        }
        else if (danhSachLopBiHuy.Count > 0)
        {
            thongDiep = $"Đã duyệt và khóa cứng lịch bận của {tenGv}. Đã hủy phân công của giảng viên tại {danhSachLopBiHuy.Count} lớp trùng ca: {string.Join(", ", danhSachLopBiHuy)}. Các lớp này hiện ở trạng thái Chưa phân công để xếp giảng viên khác.";
        }
        else
        {
            thongDiep = $"Đã duyệt và khóa cứng lịch bận của {tenGv} thành công. (Giảng viên chưa có lớp nào được phân công trùng vào ca này).";
        }

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
                KhoaCungLichBan = laNguyenVongNghiDay,
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

        bool coTheCoLichBanCung = LaLoaiTranhGio(nv.LoaiNguyenVong) &&
            (nv.MucDo >= 2 || (nv.MucDo == 0 && LaNoiDungCaHocKy(nv.NoiDungGoc)));

        if (coTheCoLichBanCung && nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            var cacThuCanXoa = (nv.Thu.HasValue && nv.Thu.Value > 0)
                ? new List<int> { nv.Thu.Value }
                : new List<int> { 2, 3, 4, 5, 6, 7, 8 };

            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == nv.MaGiangVien &&
                x.MaHocKy == nv.MaHocKy &&
                cacThuCanXoa.Contains(x.Thu) &&
                x.TietBatDau == nv.TietBatDau.Value &&
                x.TietKetThuc == nv.TietKetThuc.Value &&
                (x.LyDo == null || !x.LyDo.Contains("Seminar"))).ToListAsync(cancellationToken);
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

    public async Task<int?> LayMaGiangVienCuaNguyenVongAsync(int maNguyenVong)
    {
        return await context.NguyenVongGiangVien
            .AsNoTracking()
            .Where(x => x.MaNguyenVong == maNguyenVong)
            .Select(x => (int?)x.MaGiangVien)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> XoaNguyenVongAsync(int maNguyenVong)
    {
        var item = await context.NguyenVongGiangVien.FindAsync(maNguyenVong);
        if (item is null) return false;

        if (LaLoaiTranhGio(item.LoaiNguyenVong) && item.TietBatDau.HasValue && item.TietKetThuc.HasValue)
        {
            var cacThuCanXoa = (item.Thu.HasValue && item.Thu.Value > 0)
                ? new List<int> { item.Thu.Value }
                : new List<int> { 2, 3, 4, 5, 6, 7, 8 };

            var dsLbCu = await context.LichBanGiangVien.Where(x =>
                x.MaGiangVien == item.MaGiangVien &&
                x.MaHocKy == item.MaHocKy &&
                cacThuCanXoa.Contains(x.Thu) &&
                x.TietBatDau == item.TietBatDau.Value &&
                x.TietKetThuc == item.TietKetThuc.Value &&
                (x.LyDo == null || !x.LyDo.Contains("Seminar"))).ToListAsync();

            var dsLbCanXoa = (item.MucDo >= 2 || item.MucDo == 0)
                ? dsLbCu
                : dsLbCu.Where(x => !string.IsNullOrWhiteSpace(item.NoiDungGoc) &&
                                    !string.IsNullOrWhiteSpace(x.LyDo) &&
                                    x.LyDo.Contains(item.NoiDungGoc)).ToList();

            if (dsLbCanXoa.Count > 0)
            {
                context.LichBanGiangVien.RemoveRange(dsLbCanXoa);
            }
        }

        context.NguyenVongGiangVien.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    private static bool LaLoaiTranhGio(string? loai)
    {
        var ma = loai?.Trim();
        return ma == "SC01" || ma == "TranhDay" || ma == "XinNghi";
    }

    private static bool LaNoiDungCaHocKy(string? noiDung)
    {
        return !string.IsNullOrWhiteSpace(noiDung) && (
            noiDung.Contains("Cả học kỳ", StringComparison.OrdinalIgnoreCase) ||
            noiDung.Contains("Tuần 1-20", StringComparison.OrdinalIgnoreCase) ||
            noiDung.Contains("cả kỳ", StringComparison.OrdinalIgnoreCase));
    }

    private static bool KiemTraKhoaCungLichBan(
        NguyenVongGiangVien nv,
        string trangThai,
        List<LichBanGiangVien> dsLichBanNonSeminar)
    {
        if (!LaLoaiTranhGio(nv.LoaiNguyenVong)) return false;

        if (trangThai == "DaDuyet" && nv.TietBatDau.HasValue && nv.TietKetThuc.HasValue)
        {
            var cacThuApDung = (nv.Thu.HasValue && nv.Thu.Value > 0)
                ? new[] { nv.Thu.Value }
                : new[] { 2, 3, 4, 5, 6, 7, 8 };

            return cacThuApDung.All(thu => dsLichBanNonSeminar.Any(lb =>
                lb.MaGiangVien == nv.MaGiangVien &&
                lb.Thu == thu &&
                lb.TietBatDau == nv.TietBatDau.Value &&
                lb.TietKetThuc == nv.TietKetThuc.Value));
        }

        return nv.MucDo >= 2 || (nv.MucDo == 0 && LaNoiDungCaHocKy(nv.NoiDungGoc));
    }

    public static (string MaRb, string TenRb) PhanTichMaRangBuoc(string? loai, string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(loai)) return ("SC01", "Tránh khung giờ / tiết bận cụ thể");

        return loai.Trim() switch
        {
            "SC01" or "TranhDay" => ("SC01", "Tránh khung giờ / tiết bận cụ thể"),
            "SC02" => ("SC02", "Tránh dạy ca tối (Tiết 10 - 15)"),
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
            .Where(x => x.MaGiangVien == request.MaGiangVien &&
                        x.MaHocKy == request.MaHocKy &&
                        (x.LyDo == null || !x.LyDo.Contains("Seminar")))
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
