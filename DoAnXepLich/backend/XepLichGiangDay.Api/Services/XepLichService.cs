using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.XepLich;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Services.RangBuoc;

namespace XepLichGiangDay.Api.Services;

public class XepLichService(
    AppDbContext context,
    BoKiemTraRangBuoc boKiemTra)
{
    /// <summary>
    /// Lấy bảng dữ liệu phân công giảng dạy tổng hợp cho toàn bộ học kỳ.
    /// </summary>
    public async Task<BangPhanCongHocKyDto> LayBangPhanCongAsync(int maHocKy, int? maBoMon = null, CancellationToken cancellationToken = default)
    {
        var hocKy = await context.HocKy.FirstOrDefaultAsync(x => x.MaHocKy == maHocKy, cancellationToken);
        var tenHocKy = hocKy != null ? hocKy.TenHocKy : $"Học kỳ {maHocKy}";

        // 1. Lấy danh sách lớp học phần trong kỳ
        var queryLop = context.LopHocPhan.Where(x => x.MaHocKy == maHocKy);
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            var dsMaHpCuaBm = await context.HocPhan
                .Where(x => x.MaBoMon == maBoMon.Value)
                .Select(x => x.MaHocPhan)
                .ToListAsync(cancellationToken);
            queryLop = queryLop.Where(x => dsMaHpCuaBm.Contains(x.MaHocPhan));
        }
        var dsLop = await queryLop.OrderBy(x => x.MaLopHocPhanTruong).ToListAsync(cancellationToken);

        var dsMaLop = dsLop.Select(x => x.MaLopHocPhan).ToList();
        var dsMaHp = dsLop.Select(x => x.MaHocPhan).Distinct().ToList();

        // 2. Lấy thông tin học phần và chi tiết thời khóa biểu
        var dsHocPhan = await context.HocPhan
            .Where(x => dsMaHp.Contains(x.MaHocPhan))
            .ToDictionaryAsync(x => x.MaHocPhan, cancellationToken);

        var dsChiTiet = await context.ChiTietThoiKhoaBieu
            .Where(x => dsMaLop.Contains(x.MaLopHocPhan))
            .ToListAsync(cancellationToken);
        var chiTietTheoLop = dsChiTiet.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.ToList());

        // 3. Lấy phân công hiện tại
        var dsPhanCong = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == maHocKy)
            .ToListAsync(cancellationToken);
        var phanCongTheoLop = dsPhanCong.GroupBy(x => x.MaLopHocPhan).ToDictionary(g => g.Key, g => g.First());

        // 4. Lấy giảng viên, năng lực giảng dạy, lịch bận
        var queryGv = context.GiangVien.Where(x => x.TrangThai != "NghiViec");
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            queryGv = queryGv.Where(x => x.MaBoMon == maBoMon.Value);
        }
        var dsGiangVien = await queryGv.ToListAsync(cancellationToken);
        var dictGiangVien = dsGiangVien.ToDictionary(x => x.MaGiangVien);

        var dsNangLuc = await context.NangLucGiangDay.ToListAsync(cancellationToken);
        var dsLichBan = await context.LichBanGiangVien
            .Where(x => x.MaHocKy == maHocKy)
            .ToListAsync(cancellationToken);

        var nangLucTheoHp = dsNangLuc
            .Where(nl => string.IsNullOrEmpty(nl.TrangThai) || nl.TrangThai == "DangApDung")
            .GroupBy(x => x.MaHocPhan)
            .ToDictionary(g => g.Key, g => g.Select(x => x.MaGiangVien).ToHashSet());

        var lichBanTheoGv = dsLichBan
            .GroupBy(x => x.MaGiangVien)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Nhóm phân công hiện tại theo giảng viên để kiểm tra trùng giờ dạy giữa các lớp
        var phanCongHienTaiTheoGv = dsPhanCong
            .GroupBy(x => x.MaGiangVien)
            .ToDictionary(g => g.Key, g => g.Select(x => x.MaLopHocPhan).ToList());

        // Chỉ lấy các đơn xin nghỉ đột xuất đã duyệt (SC08 / NGHI_DOT_XUAT) trong học kỳ để hiển thị ghi chú tạm hoãn.
        // Các nguyện vọng tránh bận / ưu tiên ca thông thường (SC01 - SC07) KHÔNG được coi là tạm hoãn lớp.
        var dsNguyenVongTamHoan = await context.NguyenVongGiangVien
            .Where(x => x.MaHocKy == maHocKy &&
                        x.TrangThai == "DaDuyet" &&
                        (x.LoaiNguyenVong == "SC08" ||
                         x.LoaiNguyenVong == "NGHI_DOT_XUAT" ||
                         x.LoaiNguyenVong == "XinNghiDotXuat" ||
                         x.LoaiNguyenVong == "TamNghi"))
            .ToListAsync(cancellationToken);
        var nguyenVongTamHoanTheoGv = dsNguyenVongTamHoan
            .GroupBy(x => x.MaGiangVien)
            .ToDictionary(g => g.Key, g => g.ToList());

        var danhSachDong = new List<DongPhanCongLopDto>();

        foreach (var lop in dsLop)
        {
            var hp = dsHocPhan.TryGetValue(lop.MaHocPhan, out var hocPhan) ? hocPhan : null;
            var cacBuoiHoc = (chiTietTheoLop.TryGetValue(lop.MaLopHocPhan, out var dsCtLopHoc) ? dsCtLopHoc : [])
                .OrderBy(b => b.Thu)
                .ThenBy(b => b.TietBatDau)
                .ThenBy(b => b.TuTuan)
                .ToList();
            var ct = cacBuoiHoc.FirstOrDefault();
            var pc = phanCongTheoLop.TryGetValue(lop.MaLopHocPhan, out var phanCong) ? phanCong : null;

            var dsBuoiHocDto = cacBuoiHoc.Select(b => new ChiTietBuoiHocDto
            {
                Thu = b.Thu,
                TietBatDau = b.TietBatDau,
                TietKetThuc = b.TietKetThuc,
                PhongHoc = b.PhongHoc ?? string.Empty,
                TuTuan = b.TuTuan,
                DenTuan = b.DenTuan
            }).ToList();

            string thoiGianTongHop = string.Join("; ", cacBuoiHoc.Select(b =>
                $"{(b.Thu == 8 ? "Chủ nhật" : $"Thứ {b.Thu}")} (T{b.TietBatDau}-{b.TietKetThuc}){(string.IsNullOrWhiteSpace(b.PhongHoc) ? "" : $" P.{b.PhongHoc}")}"));

            var dong = new DongPhanCongLopDto
            {
                MaLopHocPhan = lop.MaLopHocPhan,
                MaLopHocPhanTruong = lop.MaLopHocPhanTruong,
                MaHocPhan = lop.MaHocPhan,
                TenHocPhan = hp != null ? hp.TenHocPhan : string.Empty,
                SoTinChi = hp != null ? hp.SoTinChi : 0,
                TenLop = lop.TenLop,
                SoLuongSinhVien = lop.SoLuongSinhVien,
                Thu = ct != null ? ct.Thu : 0,
                TietBatDau = ct != null ? ct.TietBatDau : 0,
                TietKetThuc = ct != null ? ct.TietKetThuc : 0,
                PhongHoc = ct != null ? ct.PhongHoc : string.Empty,
                TuTuan = ct != null ? ct.TuTuan : 0,
                DenTuan = ct != null ? ct.DenTuan : 0,
                ThoiGianHocTongHop = thoiGianTongHop,
                DanhSachBuoiHoc = dsBuoiHocDto
            };

            if (pc != null)
            {
                dong.MaPhanCong = pc.MaPhanCong;
                dong.MaGiangVien = pc.MaGiangVien;
                dong.NguonPhanCong = pc.NguonPhanCong;
                dong.TrangThai = pc.TrangThai;
                dong.DiemPhat = pc.DiemPhat;

                if (dictGiangVien.TryGetValue(pc.MaGiangVien, out var gv))
                {
                    dong.TenGiangVien = gv.HoTen;
                    dong.EmailGiangVien = gv.Email;
                }

                // Kiểm tra xem giảng viên này có đơn xin nghỉ đột xuất đã duyệt (SC08) trùng vào bất kỳ buổi học nào của lớp không
                if (nguyenVongTamHoanTheoGv.TryGetValue(pc.MaGiangVien, out var dsNvTamHoan))
                {
                    var nvNghi = dsNvTamHoan.FirstOrDefault(nv =>
                        cacBuoiHoc.Any(b =>
                            (!nv.Thu.HasValue || nv.Thu.Value == 0 || nv.Thu.Value == b.Thu) &&
                            Math.Max(nv.TietBatDau ?? 1, b.TietBatDau) <= Math.Min(nv.TietKetThuc ?? 12, b.TietKetThuc)));

                    if (nvNghi != null)
                    {
                        dong.CoLichTamNghi = true;
                        var m = System.Text.RegularExpressions.Regex.Match(nvNghi.NoiDungGoc ?? "", @"Tuần\s*(\d+(?:\s*-\s*\d+)?)");
                        if (m.Success)
                        {
                            var rawTuan = m.Groups[1].Value.Replace(" ", "");
                            if (rawTuan.Contains('-'))
                            {
                                var parts = rawTuan.Split('-');
                                dong.TuanTamNghi = (parts.Length == 2 && parts[0] == parts[1]) ? parts[0] : rawTuan;
                            }
                            else
                            {
                                dong.TuanTamNghi = rawTuan;
                            }
                        }
                        else
                        {
                            dong.TuanTamNghi = dong.TuTuan > 0 ? $"{dong.TuTuan}" : "Tạm hoãn";
                        }

                        dong.GhiChuTamNghi = !string.IsNullOrWhiteSpace(nvNghi.NoiDungGoc)
                            ? nvNghi.NoiDungGoc
                            : "Tạm hoãn giảng dạy theo đơn xin nghỉ đột xuất đã duyệt";
                    }
                }
            }

            // Gợi ý danh sách ứng viên cho lớp này
            var dsGvIdCoNangLuc = nangLucTheoHp.TryGetValue(lop.MaHocPhan, out var gvIds) ? gvIds : [];
            var dsUngVien = new List<UngVienLopDto>();

            foreach (var gv in dsGiangVien)
            {
                bool coNl = dsGvIdCoNangLuc.Contains(gv.MaGiangVien);
                if (!coNl) continue; // Chỉ gợi ý những GV có chuyên môn

                bool vuongLb = false;
                bool vuongSeminar = false;
                string? tenSeminarTrung = null;
                if (cacBuoiHoc.Count > 0 && lichBanTheoGv.TryGetValue(gv.MaGiangVien, out var dsLbCuaGv))
                {
                    foreach (var b in cacBuoiHoc)
                    {
                        foreach (var lb in dsLbCuaGv)
                        {
                            if (lb.Thu == b.Thu && Math.Max(lb.TietBatDau, b.TietBatDau) <= Math.Min(lb.TietKetThuc, b.TietKetThuc))
                            {
                                vuongLb = true;
                                if (!string.IsNullOrWhiteSpace(lb.LyDo) &&
                                    lb.LyDo.Contains("Seminar", StringComparison.OrdinalIgnoreCase))
                                {
                                    vuongSeminar = true;
                                    tenSeminarTrung ??= lb.LyDo;
                                }
                            }
                        }
                    }
                }

                bool trungLd = false;
                if (cacBuoiHoc.Count > 0 && phanCongHienTaiTheoGv.TryGetValue(gv.MaGiangVien, out var cacMaLopCuaGv))
                {
                    foreach (var otherMaLop in cacMaLopCuaGv)
                    {
                        if (otherMaLop == lop.MaLopHocPhan) continue; // Bỏ qua chính lớp đang xét
                        if (chiTietTheoLop.TryGetValue(otherMaLop, out var otherCacBuoi) && otherCacBuoi != null)
                        {
                            foreach (var bCurrent in cacBuoiHoc)
                            {
                                foreach (var bOther in otherCacBuoi)
                                {
                                    if (boKiemTra.CoTrungLich(
                                        bCurrent.Thu, bCurrent.TietBatDau, bCurrent.TietKetThuc, bCurrent.TuTuan, bCurrent.DenTuan,
                                        bOther.Thu, bOther.TietBatDau, bOther.TietKetThuc, bOther.TuTuan, bOther.DenTuan))
                                    {
                                        trungLd = true;
                                        break;
                                    }
                                }
                                if (trungLd) break;
                            }
                        }
                        if (trungLd) break;
                    }
                }


                var lyDo = new List<string>();
                if (vuongSeminar)
                {
                    lyDo.Add($"Trùng lịch Seminar ({tenSeminarTrung})");
                }
                else if (vuongLb)
                {
                    lyDo.Add("Vướng lịch bận cá nhân");
                }
                if (trungLd) lyDo.Add("Trùng giờ dạy với lớp khác");

                dsUngVien.Add(new UngVienLopDto
                {
                    MaGiangVien = gv.MaGiangVien,
                    HoTen = gv.HoTen,
                    Email = gv.Email,
                    ChucDanh = gv.ChucDanh,
                    CoNangLuc = true,
                    BiVuongLichBan = vuongLb,
                    BiKhoaCungSeminar = vuongSeminar,
                    BiTrungLichDay = trungLd,
                    LyDoKhongHopLe = string.Join("; ", lyDo)
                });
            }

            // Sắp xếp ứng viên hợp lệ lên đầu
            dong.DanhSachUngVien = dsUngVien
                .OrderByDescending(x => x.HopLe)
                .ThenBy(x => x.HoTen)
                .ToList();

            danhSachDong.Add(dong);
        }

        return new BangPhanCongHocKyDto
        {
            MaHocKy = maHocKy,
            TenHocKy = tenHocKy,
            TongSoLop = danhSachDong.Count,
            SoLopDaPhanCong = danhSachDong.Count(x => x.MaGiangVien.HasValue && x.MaGiangVien > 0),
            SoLopChuaPhanCong = danhSachDong.Count(x => !x.MaGiangVien.HasValue || x.MaGiangVien <= 0),
            SoLopDaKhoa = danhSachDong.Count(x => x.DaKhoa),
            DanhSachLop = danhSachDong
        };
    }

    /// <summary>
    /// Can thiệp gán hoặc đổi giảng viên cho một lớp học phần bằng tay.
    /// Có kiểm tra ràng buộc tức thì (chuyên môn, lịch bận, trùng giờ).
    /// </summary>
    public async Task<KetQuaPhanCongThuCongDto> PhanCongThuCongAsync(YeuCauPhanCongThuCongDto req, CancellationToken cancellationToken = default)
    {
        var lop = await context.LopHocPhan.FirstOrDefaultAsync(x => x.MaLopHocPhan == req.MaLopHocPhan, cancellationToken);
        if (lop == null)
        {
            return new KetQuaPhanCongThuCongDto
            {
                ThanhCong = false,
                ThongBao = "Không tìm thấy lớp học phần."
            };
        }

        var gv = await context.GiangVien.FirstOrDefaultAsync(x => x.MaGiangVien == req.MaGiangVien, cancellationToken);
        if (gv == null)
        {
            return new KetQuaPhanCongThuCongDto
            {
                ThanhCong = false,
                ThongBao = "Không tìm thấy giảng viên."
            };
        }

        var viPham = new List<string>();
        bool coViPhamSeminar = false;

        // 1. Kiểm tra năng lực chuyên môn
        var coNangLuc = await context.NangLucGiangDay.AnyAsync(
            x => x.MaGiangVien == req.MaGiangVien && x.MaHocPhan == lop.MaHocPhan && (string.IsNullOrEmpty(x.TrangThai) || x.TrangThai == "DangApDung"),
            cancellationToken);

        if (!coNangLuc)
        {
            viPham.Add($"Giảng viên {gv.HoTen} không có năng lực giảng dạy học phần này.");
        }

        // 2. Lấy toàn bộ các buổi học của lớp (hỗ trợ đầy đủ các lớp có nhiều buổi/tuần)
        var dsCtLop = await context.ChiTietThoiKhoaBieu
            .Where(x => x.MaLopHocPhan == req.MaLopHocPhan)
            .ToListAsync(cancellationToken);

        if (dsCtLop.Count > 0)
        {
            // 2.1 Kiểm tra vướng lịch bận cá nhân cho tất cả các buổi
            var dsLb = await context.LichBanGiangVien
                .Where(x => x.MaGiangVien == req.MaGiangVien && x.MaHocKy == lop.MaHocKy)
                .ToListAsync(cancellationToken);

            foreach (var ctLop in dsCtLop)
            {
                var dsLbTrung = dsLb.Where(lb =>
                    lb.Thu == ctLop.Thu &&
                    Math.Max(lb.TietBatDau, ctLop.TietBatDau) <= Math.Min(lb.TietKetThuc, ctLop.TietKetThuc)).ToList();

                if (dsLbTrung.Count > 0)
                {
                    var lbSeminar = dsLbTrung.FirstOrDefault(lb =>
                        !string.IsNullOrWhiteSpace(lb.LyDo) &&
                        lb.LyDo.Contains("Seminar", StringComparison.OrdinalIgnoreCase));
                    if (lbSeminar != null)
                    {
                        coViPhamSeminar = true;
                        viPham.Add($"[KHÓA CHẾT CỨNG SEMINAR] Giảng viên {gv.HoTen} đang có lịch {lbSeminar.LyDo} vào Thứ {ctLop.Thu}, tiết {ctLop.TietBatDau}-{ctLop.TietKetThuc}.");
                    }
                    else
                    {
                        viPham.Add($"Giảng viên {gv.HoTen} bị vướng lịch bận cá nhân vào Thứ {ctLop.Thu}, tiết {ctLop.TietBatDau}-{ctLop.TietKetThuc}.");
                    }
                }
            }

            // 2.2 Kiểm tra trùng giờ dạy với lớp khác trong cùng học kỳ
            var dsLopKhacCuaGv = await context.PhanCongGiangDay
                .Where(x => x.MaHocKy == lop.MaHocKy && x.MaGiangVien == req.MaGiangVien && x.MaLopHocPhan != req.MaLopHocPhan)
                .Select(x => x.MaLopHocPhan)
                .ToListAsync(cancellationToken);

            if (dsLopKhacCuaGv.Count > 0)
            {
                var dsChiTietKhac = await context.ChiTietThoiKhoaBieu
                    .Where(x => dsLopKhacCuaGv.Contains(x.MaLopHocPhan))
                    .ToListAsync(cancellationToken);

                var mapLopKhac = await context.LopHocPhan
                    .Where(x => dsLopKhacCuaGv.Contains(x.MaLopHocPhan))
                    .ToDictionaryAsync(x => x.MaLopHocPhan, cancellationToken);

                foreach (var ctLop in dsCtLop)
                {
                    foreach (var ctKhac in dsChiTietKhac)
                    {
                        if (boKiemTra.CoTrungLich(
                            ctLop.Thu, ctLop.TietBatDau, ctLop.TietKetThuc, ctLop.TuTuan, ctLop.DenTuan,
                            ctKhac.Thu, ctKhac.TietBatDau, ctKhac.TietKetThuc, ctKhac.TuTuan, ctKhac.DenTuan))
                        {
                            string maLopKhac = mapLopKhac.TryGetValue(ctKhac.MaLopHocPhan, out var lk) ? lk.MaLopHocPhanTruong : $"#{ctKhac.MaLopHocPhan}";
                            viPham.Add($"Trùng giờ dạy buổi Thứ {ctLop.Thu} (tiết {ctLop.TietBatDau}-{ctLop.TietKetThuc}) với lớp [{maLopKhac}] (Thứ {ctKhac.Thu}, tiết {ctKhac.TietBatDau}-{ctKhac.TietKetThuc}).");
                        }
                    }
                }
            }
        }


        // Lịch Seminar là bắt buộc cố định: không cho phép ép gán (kể cả BoQuaCanhBao = true)
        if (coViPhamSeminar)
        {
            return new KetQuaPhanCongThuCongDto
            {
                ThanhCong = false,
                ThongBao = $"KHÔNG THỂ PHÂN CÔNG: Giảng viên {gv.HoTen} đang có lịch Seminar trùng giờ với lớp học phần này.",
                DanhSachViPham = viPham,
                CanXacNhan = false
            };
        }

        // Nếu có vi phạm và người dùng chưa xác nhận bỏ qua cảnh báo
        if (viPham.Count > 0 && !req.BoQuaCanhBao)
        {
            return new KetQuaPhanCongThuCongDto
            {
                ThanhCong = false,
                ThongBao = "Phát hiện vi phạm ràng buộc phân công.",
                DanhSachViPham = viPham,
                CanXacNhan = true
            };
        }

        // Lưu / cập nhật bảng phan_cong_giang_day
        var phanCong = await context.PhanCongGiangDay
            .FirstOrDefaultAsync(x => x.MaLopHocPhan == req.MaLopHocPhan, cancellationToken);

        if (phanCong != null)
        {
            phanCong.MaGiangVien = req.MaGiangVien;
            phanCong.NguonPhanCong = "ThuCong";
            phanCong.TrangThai = req.KhoaNgay ? "DaKhoa" : "DuKien";
        }
        else
        {
            phanCong = new PhanCongGiangDay
            {
                MaLopHocPhan = req.MaLopHocPhan,
                MaGiangVien = req.MaGiangVien,
                MaHocKy = lop.MaHocKy,
                NguonPhanCong = "ThuCong",
                TrangThai = req.KhoaNgay ? "DaKhoa" : "DuKien",
                DiemPhat = 0
            };
            context.PhanCongGiangDay.Add(phanCong);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new KetQuaPhanCongThuCongDto
        {
            ThanhCong = true,
            ThongBao = $"Đã phân công thành công cho giảng viên {gv.HoTen}.",
            DanhSachViPham = viPham
        };
    }

    /// <summary>
    /// Khóa hoặc mở khóa trạng thái phân công của lớp.
    /// </summary>
    public async Task<bool> DoiTrangThaiKhoaAsync(int maLopHocPhan, bool daKhoa, CancellationToken cancellationToken = default)
    {
        var phanCong = await context.PhanCongGiangDay
            .FirstOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, cancellationToken);

        if (phanCong == null) return false;

        phanCong.TrangThai = daKhoa ? "DaKhoa" : "DuKien";
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Hủy phân công của một lớp học phần.
    /// </summary>
    public async Task<bool> HuyPhanCongAsync(int maLopHocPhan, CancellationToken cancellationToken = default)
    {
        var phanCong = await context.PhanCongGiangDay
            .FirstOrDefaultAsync(x => x.MaLopHocPhan == maLopHocPhan, cancellationToken);

        if (phanCong == null) return false;

        context.PhanCongGiangDay.Remove(phanCong);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Đặt lại phân công học kỳ: xóa toàn bộ các phân công dự kiến (chưa khóa) để đưa bảng về trống, giữ nguyên các lớp đã khóa.
    /// </summary>
    public async Task<int> DatLaiPhanCongAsync(int maHocKy, CancellationToken cancellationToken = default)
    {
        var cacPhanCongDuKien = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == maHocKy && x.TrangThai != "DaKhoa")
            .ToListAsync(cancellationToken);

        if (cacPhanCongDuKien.Count > 0)
        {
            context.PhanCongGiangDay.RemoveRange(cacPhanCongDuKien);
            await context.SaveChangesAsync(cancellationToken);
        }

        return cacPhanCongDuKien.Count;
    }

    /// <summary>
    /// Thống kê khối lượng tải giảng dạy của tất cả giảng viên trong học kỳ và so sánh với định mức.
    /// </summary>
    public async Task<List<ThongKeTaiGvDto>> LayThongKeTaiAsync(int maHocKy, int? maBoMon = null, CancellationToken cancellationToken = default)
    {
        var queryGv = context.GiangVien.Where(x => x.TrangThai != "NghiViec");
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            queryGv = queryGv.Where(x => x.MaBoMon == maBoMon.Value);
        }
        var dsGiangVien = await queryGv.OrderBy(x => x.HoTen).ToListAsync(cancellationToken);

        var dsPhanCong = await context.PhanCongGiangDay
            .Where(x => x.MaHocKy == maHocKy)
            .ToListAsync(cancellationToken);

        var dsMaLop = dsPhanCong.Select(x => x.MaLopHocPhan).Distinct().ToList();

        var dsLop = await context.LopHocPhan
            .Where(x => dsMaLop.Contains(x.MaLopHocPhan))
            .ToDictionaryAsync(x => x.MaLopHocPhan, cancellationToken);

        var dsMaHp = dsLop.Values.Select(x => x.MaHocPhan).Distinct().ToList();
        var dsHocPhan = await context.HocPhan
            .Where(x => dsMaHp.Contains(x.MaHocPhan))
            .ToDictionaryAsync(x => x.MaHocPhan, cancellationToken);

        var dsChiTiet = await context.ChiTietThoiKhoaBieu
            .Where(x => dsMaLop.Contains(x.MaLopHocPhan))
            .ToListAsync(cancellationToken);
        var chiTietTheoLop = dsChiTiet
            .GroupBy(x => x.MaLopHocPhan)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dsDinhMucRaw = await context.DinhMucGiangVien
            .Where(x => x.MaHocKy == maHocKy && (string.IsNullOrEmpty(x.TrangThai) || x.TrangThai == "DangApDung"))
            .ToListAsync(cancellationToken);
        var dsDinhMuc = dsDinhMucRaw
            .GroupBy(x => x.MaGiangVien)
            .ToDictionary(g => g.Key, g => g.First());

        var phanCongTheoGv = dsPhanCong.GroupBy(x => x.MaGiangVien).ToDictionary(g => g.Key, g => g.ToList());

        var ketQua = new List<ThongKeTaiGvDto>();

        foreach (var gv in dsGiangVien)
        {
            var item = new ThongKeTaiGvDto
            {
                MaGiangVien = gv.MaGiangVien,
                HoTen = gv.HoTen,
                Email = gv.Email,
                ChucDanh = gv.ChucDanh
            };

            if (dsDinhMuc.TryGetValue(gv.MaGiangVien, out var dm))
            {
                item.SoTietToiThieu = dm.SoTietToiThieu;
                item.SoTietToiDa = dm.SoTietToiDa;
            }

            int maxTietTuan = 0;

            if (phanCongTheoGv.TryGetValue(gv.MaGiangVien, out var cacPhanCong))
            {
                item.SoLopPhanCong = cacPhanCong.Count;
                var cacBuoiHocRaw = new List<ChiTietThoiKhoaBieu>();

                foreach (var pc in cacPhanCong)
                {
                    if (dsLop.TryGetValue(pc.MaLopHocPhan, out var lop) && dsHocPhan.TryGetValue(lop.MaHocPhan, out var hp))
                    {
                        item.TongSoTinChi += hp.SoTinChi;
                    }

                    if (chiTietTheoLop.TryGetValue(pc.MaLopHocPhan, out var dsCtLop))
                    {
                        cacBuoiHocRaw.AddRange(dsCtLop);
                    }
                }

                // Khử trùng lặp lớp ghép học chung môn, cùng thứ/tiết/tuần/phòng học
                var cacBuoiHoc = BoKiemTraRangBuoc.KhuTrungLopGhep(cacBuoiHocRaw, dsLop);

                if (cacBuoiHoc.Count > 0)
                {
                    int minTuanHk = cacBuoiHoc.Min(b => b.TuTuan > 0 ? b.TuTuan : 1);
                    int maxTuanHk = cacBuoiHoc.Max(b => b.DenTuan > 0 ? b.DenTuan : 15);
                    if (maxTuanHk < minTuanHk) maxTuanHk = minTuanHk;

                    for (int w = minTuanHk; w <= maxTuanHk; w++)
                    {
                        int soTietTuanW = 0;
                        foreach (var b in cacBuoiHoc)
                        {
                            int tu = b.TuTuan > 0 ? b.TuTuan : 1;
                            int den = b.DenTuan > 0 ? Math.Max(tu, b.DenTuan) : maxTuanHk;
                            if (w >= tu && w <= den && b.TietKetThuc >= b.TietBatDau)
                            {
                                soTietTuanW += b.TietKetThuc - b.TietBatDau + 1;
                            }
                        }

                        if (soTietTuanW > maxTietTuan)
                        {
                            maxTietTuan = soTietTuanW;
                        }
                    }
                }
            }

            item.TongSoTietTuan = maxTietTuan;

            // Đánh giá trạng thái tải tuần so với định mức chuẩn
            bool isThinhGiang = (gv.ChucDanh != null && gv.ChucDanh.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase)) ||
                                gv.HoTen.Contains("Thỉnh giảng", StringComparison.OrdinalIgnoreCase);
            bool isTroGiang = (gv.ChucDanh != null && gv.ChucDanh.Contains("trợ giảng", StringComparison.OrdinalIgnoreCase)) ||
                              gv.HoTen.Contains("Trợ giảng", StringComparison.OrdinalIgnoreCase);
            int maxTuanChuan = isTroGiang ? 12 : (isThinhGiang ? 16 : 25);
            int minTuanChuan = isTroGiang ? 0 : (isThinhGiang ? 3 : 6);
            int maxTietTuanGv = (item.SoTietToiDa > 0 && item.SoTietToiDa <= maxTuanChuan) ? item.SoTietToiDa : maxTuanChuan;
            int minTietTuanGv = minTuanChuan;
            if (dsDinhMuc.ContainsKey(gv.MaGiangVien))
            {
                if (item.SoTietToiThieu >= 0 && (!isTroGiang || item.SoTietToiThieu > 0))
                {
                    minTietTuanGv = item.SoTietToiThieu > 30
                        ? (int)Math.Ceiling(item.SoTietToiThieu / 15.0)
                        : item.SoTietToiThieu;
                }
            }
            item.SoTietToiThieu = minTietTuanGv;
            item.SoTietToiDa = maxTietTuanGv;

            if (item.TongSoTietTuan > maxTietTuanGv)
            {
                item.TrangThaiTai = "QuaTai";
            }
            else if (item.TongSoTietTuan < minTietTuanGv)
            {
                item.TrangThaiTai = "ThieuTai";
            }
            else
            {
                item.TrangThaiTai = "BinhThuong";
            }

            ketQua.Add(item);
        }

        return ketQua;
    }

    /// <summary>
    /// Xuất toàn bộ bảng phân công thời khóa biểu và thống kê tải của học kỳ ra file Excel.
    /// </summary>
    /// <summary>
    /// Lấy số liệu tổng quan quy mô học kỳ phục vụ Dashboard.
    /// </summary>
    public async Task<SoSanhThuatToanDto> LayThongTinTongQuanHocKyAsync(int maHocKy, int? maBoMon = null, CancellationToken cancellationToken = default)
    {
        var hocKy = await context.HocKy.FirstOrDefaultAsync(x => x.MaHocKy == maHocKy, cancellationToken);
        var tenHocKy = hocKy?.TenHocKy ?? $"Học kỳ {maHocKy}";
        var queryLop = context.LopHocPhan.Where(x => x.MaHocKy == maHocKy);
        var queryGv = context.GiangVien.Where(x => x.TrangThai != "NghiViec");
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            var dsMaHp = await context.HocPhan.Where(x => x.MaBoMon == maBoMon.Value).Select(x => x.MaHocPhan).ToListAsync(cancellationToken);
            queryLop = queryLop.Where(x => dsMaHp.Contains(x.MaHocPhan));
            queryGv = queryGv.Where(x => x.MaBoMon == maBoMon.Value);
        }
        var tongSoLop = await queryLop.CountAsync(cancellationToken);
        var tongSoGv = await queryGv.CountAsync(cancellationToken);

        return new SoSanhThuatToanDto
        {
            MaHocKy = maHocKy,
            TenHocKy = tenHocKy,
            TongSoLop = tongSoLop,
            TongSoGiangVien = tongSoGv
        };
    }

    public async Task<byte[]> XuatExcelBangPhanCongAsync(int maHocKy, int? maBoMon = null, CancellationToken cancellationToken = default)
    {
        var bangPhanCong = await LayBangPhanCongAsync(maHocKy, maBoMon, cancellationToken);
        var thongKeTai = await LayThongKeTaiAsync(maHocKy, maBoMon, cancellationToken);

        using var workbook = new XLWorkbook();

        // 1. Sheet 1: Thời khóa biểu & Phân công
        var ws1 = workbook.Worksheets.Add("ThoiKhoaBieu");

        // Tiêu đề
        ws1.Cell(1, 1).Value = $"BẢNG THỜI KHÓA BIỂU VÀ PHÂN CÔNG GIẢNG DẠY - {bangPhanCong.TenHocKy.ToUpperInvariant()}";
        ws1.Range(1, 1, 1, 15).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Header
        string[] headers1 = [
            "STT", "Mã lớp HP", "Tên học phần", "Số TC", "Lớp sinh viên", "SL SV",
            "Thứ", "Tiết bắt đầu", "Tiết kết thúc", "Số tiết", "Phòng", "Tuần học",
            "Giảng viên phân công", "Trạng thái", "Nguồn phân công"
        ];

        for (int i = 0; i < headers1.Length; i++)
        {
            var cell = ws1.Cell(3, i + 1);
            cell.Value = headers1[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EFEFEF"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row1 = 4;
        int stt1 = 1;
        foreach (var lop in bangPhanCong.DanhSachLop)
        {
            var cacBuoiXuat = lop.DanhSachBuoiHoc != null && lop.DanhSachBuoiHoc.Count > 0
                ? lop.DanhSachBuoiHoc
                : new List<ChiTietBuoiHocDto>
                {
                    new()
                    {
                        Thu = lop.Thu,
                        TietBatDau = lop.TietBatDau,
                        TietKetThuc = lop.TietKetThuc,
                        PhongHoc = lop.PhongHoc,
                        TuTuan = lop.TuTuan,
                        DenTuan = lop.DenTuan
                    }
                };

            string tenGv = !string.IsNullOrEmpty(lop.TenGiangVien) ? lop.TenGiangVien : "Chưa phân công";
            string trangThai = lop.DaKhoa ? "Đã khóa" : (!string.IsNullOrEmpty(lop.TenGiangVien) ? "Dự kiến" : "Chưa phân công");
            string nguonPc = lop.NguonPhanCong == "ThuCong" ? "Thủ công" : lop.NguonPhanCong;

            foreach (var buoi in cacBuoiXuat)
            {
                int soTietBuoi = buoi.TietKetThuc >= buoi.TietBatDau && buoi.TietBatDau > 0
                    ? (buoi.TietKetThuc - buoi.TietBatDau + 1)
                    : 0;
                string thuStr = buoi.Thu == 8 ? "Chủ nhật" : (buoi.Thu > 0 ? $"Thứ {buoi.Thu}" : "-");

                ws1.Cell(row1, 1).Value = stt1++;
                ws1.Cell(row1, 2).Value = lop.MaLopHocPhanTruong;
                ws1.Cell(row1, 3).Value = lop.TenHocPhan;
                ws1.Cell(row1, 4).Value = lop.SoTinChi;
                ws1.Cell(row1, 5).Value = lop.TenLop;
                ws1.Cell(row1, 6).Value = lop.SoLuongSinhVien;
                ws1.Cell(row1, 7).Value = thuStr;
                ws1.Cell(row1, 8).Value = buoi.TietBatDau > 0 ? buoi.TietBatDau : "-";
                ws1.Cell(row1, 9).Value = buoi.TietKetThuc > 0 ? buoi.TietKetThuc : "-";
                ws1.Cell(row1, 10).Value = soTietBuoi;
                ws1.Cell(row1, 11).Value = buoi.PhongHoc;
                ws1.Cell(row1, 12).Value = buoi.TuTuan > 0 ? $"{buoi.TuTuan}-{buoi.DenTuan}" : "-";
                ws1.Cell(row1, 13).Value = tenGv;
                ws1.Cell(row1, 14).Value = trangThai;
                ws1.Cell(row1, 15).Value = nguonPc;

                // Kẻ viền và căn chỉnh
                for (int col = 1; col <= 15; col++)
                {
                    ws1.Cell(row1, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                ws1.Cell(row1, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 11).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 12).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 14).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws1.Cell(row1, 15).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                row1++;
            }
        }
        ws1.Columns().AdjustToContents();

        // 2. Sheet 2: Thống kê định mức tải giảng viên
        var ws2 = workbook.Worksheets.Add("ThongKeTaiGiangVien");

        ws2.Cell(1, 1).Value = $"BẢNG TỔNG HỢP TẢI GIẢNG DẠY GIẢNG VIÊN - {bangPhanCong.TenHocKy.ToUpperInvariant()}";
        ws2.Range(1, 1, 1, 9).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        string[] headers2 = [
            "STT", "Họ và tên", "Email", "Chức danh", "Số lớp đảm nhiệm",
            "Tổng số tín chỉ", "Tổng số tiết/tuần", "Định mức (Min - Max)", "Trạng thái tải"
        ];

        for (int i = 0; i < headers2.Length; i++)
        {
            var cell = ws2.Cell(3, i + 1);
            cell.Value = headers2[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EFEFEF"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int row2 = 4;
        int stt2 = 1;
        foreach (var gv in thongKeTai)
        {
            string dinhMucStr = gv.SoTietToiThieu > 0 || gv.SoTietToiDa > 0 ? $"{gv.SoTietToiThieu} - {gv.SoTietToiDa} tiết" : "Chưa cấu hình";
            string trangThaiStr = gv.TrangThaiTai switch
            {
                "ThieuTai" => "Thiếu tải",
                "QuaTai" => "Quá tải",
                _ => "Đạt định mức"
            };

            ws2.Cell(row2, 1).Value = stt2++;
            ws2.Cell(row2, 2).Value = gv.HoTen;
            ws2.Cell(row2, 3).Value = gv.Email;
            ws2.Cell(row2, 4).Value = gv.ChucDanh ?? "-";
            ws2.Cell(row2, 5).Value = gv.SoLopPhanCong;
            ws2.Cell(row2, 6).Value = gv.TongSoTinChi;
            ws2.Cell(row2, 7).Value = gv.TongSoTietTuan;
            ws2.Cell(row2, 8).Value = dinhMucStr;
            ws2.Cell(row2, 9).Value = trangThaiStr;

            for (int col = 1; col <= 9; col++)
            {
                ws2.Cell(row2, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws2.Cell(row2, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws2.Cell(row2, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row2++;
        }
        ws2.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Xuất ma trận Thời khóa biểu & Seminar cả kỳ dạng tối giản trắng đen (5 Ca × Thứ 2–CN):
    /// - Sheet 1: TongHop_BoMon (tổng hợp cả bộ môn kèm phòng học, tuần học và lịch Seminar)
    /// - Sheet 2: ThongKe_GiangVien (số lớp HP, tổng số buổi cả kỳ, tổng số tiết cả kỳ, lịch Seminar)
    /// - Các sheet tiếp theo: Ma trận lịch cá nhân cho từng giảng viên (căn sẵn 1 trang A4 ngang)
    /// </summary>
    public async Task<byte[]> XuatExcelMaTranTkbAsync(int maHocKy, int? maBoMon = null, CancellationToken cancellationToken = default)
    {
        var bangPhanCong = await LayBangPhanCongAsync(maHocKy, maBoMon, cancellationToken);

        string tenBoMon = "TẤT CẢ BỘ MÔN";
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            var bm = await context.BoMon.AsNoTracking().FirstOrDefaultAsync(x => x.MaBoMon == maBoMon.Value, cancellationToken);
            if (bm != null && !string.IsNullOrWhiteSpace(bm.TenBoMon))
                tenBoMon = bm.TenBoMon.Trim();
        }

        // Lấy danh sách giảng viên thuộc phạm vi (để map lịch Seminar)
        var gvQuery = context.GiangVien.AsNoTracking().AsQueryable();
        if (maBoMon.HasValue && maBoMon.Value > 0)
            gvQuery = gvQuery.Where(x => x.MaBoMon == maBoMon.Value);
        var gvMap = await gvQuery.ToDictionaryAsync(x => x.MaGiangVien, x => x.HoTen, cancellationToken);

        // Lấy lịch Seminar cứng trong học kỳ
        var dsLichBanHk = await context.LichBanGiangVien.AsNoTracking()
            .Where(x => x.MaHocKy == maHocKy && x.LyDo != null)
            .ToListAsync(cancellationToken);
        var dsSeminar = dsLichBanHk
            .Where(x => x.LyDo!.Contains("Seminar", StringComparison.OrdinalIgnoreCase) && gvMap.ContainsKey(x.MaGiangVien))
            .ToList();

        // Định nghĩa 5 ca học chuẩn
        var cacCaHoc = new (string TenCa, string TietStr, int TietMin, int TietMax)[]
        {
            ("Ca 1 · Sáng", "Tiết 1–3", 1, 3),
            ("Ca 2 · Sáng", "Tiết 4–6", 4, 6),
            ("Ca 3 · Chiều", "Tiết 7–9", 7, 9),
            ("Ca 4 · Chiều", "Tiết 10–12", 10, 12),
            ("Ca 5 · Tối", "Tiết 13–15", 13, 15)
        };
        string[] thuHeaders = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];

        // Xây dựng bảng viết tắt tên học phần (chuẩn hóa các môn Toán + tự sinh cho môn khác)
        var tenHpDistinct = bangPhanCong.DanhSachLop
            .Where(x => !string.IsNullOrWhiteSpace(x.TenHocPhan))
            .Select(x => x.TenHocPhan.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var vietTatTheoTenHp = TaoBangVietTatHocPhan(tenHpDistinct);

        // Lưu dữ liệu tổng hợp theo (caIdx, dayIdx) và theo từng giảng viên
        var allCellClasses = new Dictionary<(int Ca, int Day), List<(string TenGv, string NoiDung)>>();
        var allCellSeminars = new Dictionary<(int Ca, int Day), SortedSet<string>>();
        var gvGrid = new Dictionary<string, Dictionary<(int Ca, int Day), List<string>>>(StringComparer.CurrentCultureIgnoreCase);
        var gvSeminarLabels = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
        var gvLopIds = new Dictionary<string, HashSet<int>>(StringComparer.CurrentCultureIgnoreCase);
        var gvTongBuoi = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        var gvTongTiet = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

        void EnsureGv(string tenGv)
        {
            if (!gvGrid.ContainsKey(tenGv)) gvGrid[tenGv] = [];
            if (!gvSeminarLabels.ContainsKey(tenGv)) gvSeminarLabels[tenGv] = [];
            if (!gvLopIds.ContainsKey(tenGv)) gvLopIds[tenGv] = [];
            if (!gvTongBuoi.ContainsKey(tenGv)) gvTongBuoi[tenGv] = 0;
            if (!gvTongTiet.ContainsKey(tenGv)) gvTongTiet[tenGv] = 0;
        }

        // 1. Nạp lịch Seminar
        foreach (var sem in dsSeminar)
        {
            if (sem.Thu < 2 || sem.Thu > 8) continue;
            int dayIdx = sem.Thu - 2;
            string tenGv = gvMap[sem.MaGiangVien].Trim();
            EnsureGv(tenGv);

            for (int caIdx = 0; caIdx < cacCaHoc.Length; caIdx++)
            {
                var ca = cacCaHoc[caIdx];
                if (Math.Max(sem.TietBatDau, ca.TietMin) <= Math.Min(sem.TietKetThuc, ca.TietMax))
                {
                    var key = (caIdx, dayIdx);
                    if (!allCellSeminars.TryGetValue(key, out var setGv))
                    {
                        setGv = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
                        allCellSeminars[key] = setGv;
                    }
                    setGv.Add(tenGv);

                    if (!gvGrid[tenGv].TryGetValue(key, out var listCell))
                    {
                        listCell = [];
                        gvGrid[tenGv][key] = listCell;
                    }
                    const string semText = "[SEMINAR] Sinh hoạt Bộ môn";
                    if (!listCell.Contains(semText))
                        listCell.Add(semText);

                    string labelSem = $"{thuHeaders[dayIdx]}, {ca.TenCa} ({ca.TietStr})";
                    if (!gvSeminarLabels[tenGv].Contains(labelSem))
                        gvSeminarLabels[tenGv].Add(labelSem);
                }
            }
        }

        // 2. Nạp lịch giảng dạy từng lớp học phần đã phân công
        foreach (var lop in bangPhanCong.DanhSachLop)
        {
            if (string.IsNullOrWhiteSpace(lop.TenGiangVien)) continue;
            string tenGv = lop.TenGiangVien.Trim();
            EnsureGv(tenGv);
            gvLopIds[tenGv].Add(lop.MaLopHocPhan);

            string tenHp = (lop.TenHocPhan ?? "").Trim();
            string maMon = vietTatTheoTenHp.TryGetValue(tenHp, out var vt) ? vt : tenHp;
            string tenLopHienThi = !string.IsNullOrWhiteSpace(lop.TenLop) ? lop.TenLop.Trim() : lop.MaLopHocPhanTruong.Trim();

            var cacBuoi = lop.DanhSachBuoiHoc != null && lop.DanhSachBuoiHoc.Count > 0
                ? lop.DanhSachBuoiHoc
                : new List<ChiTietBuoiHocDto>
                {
                    new()
                    {
                        Thu = lop.Thu,
                        TietBatDau = lop.TietBatDau,
                        TietKetThuc = lop.TietKetThuc,
                        PhongHoc = lop.PhongHoc,
                        TuTuan = lop.TuTuan,
                        DenTuan = lop.DenTuan
                    }
                };

            foreach (var buoi in cacBuoi)
            {
                if (buoi.Thu < 2 || buoi.Thu > 8 || buoi.TietBatDau <= 0 || buoi.TietKetThuc <= 0) continue;
                int dayIdx = buoi.Thu - 2;
                int soTuan = (buoi.TuTuan > 0 && buoi.DenTuan >= buoi.TuTuan) ? (buoi.DenTuan - buoi.TuTuan + 1) : 1;

                var metaParts = new List<string> { tenLopHienThi };
                string phongRaw = (buoi.PhongHoc ?? "").Trim();
                if (!string.IsNullOrEmpty(phongRaw))
                {
                    string phongFmt = phongRaw.StartsWith("P.", StringComparison.OrdinalIgnoreCase) ? phongRaw : $"P.{phongRaw}";
                    metaParts.Add(phongFmt);
                }
                if (buoi.TuTuan > 0 && buoi.DenTuan >= buoi.TuTuan)
                {
                    metaParts.Add($"T{buoi.TuTuan}-{buoi.DenTuan}");
                }
                string detailText = $"{maMon} ({string.Join(", ", metaParts)})";

                for (int caIdx = 0; caIdx < cacCaHoc.Length; caIdx++)
                {
                    var ca = cacCaHoc[caIdx];
                    int ovStart = Math.Max(buoi.TietBatDau, ca.TietMin);
                    int ovEnd = Math.Min(buoi.TietKetThuc, ca.TietMax);
                    if (ovStart <= ovEnd)
                    {
                        int soTietTrongCa = ovEnd - ovStart + 1;
                        gvTongBuoi[tenGv] += soTuan;
                        gvTongTiet[tenGv] += soTuan * soTietTrongCa;

                        var key = (caIdx, dayIdx);
                        if (!allCellClasses.TryGetValue(key, out var clsList))
                        {
                            clsList = [];
                            allCellClasses[key] = clsList;
                        }
                        clsList.Add((tenGv, $"{tenGv} - {detailText}"));

                        if (!gvGrid[tenGv].TryGetValue(key, out var gvList))
                        {
                            gvList = [];
                            gvGrid[tenGv][key] = gvList;
                        }
                        gvList.Add(detailText);
                    }
                }
            }
        }

        // Sắp xếp danh sách giảng viên theo tên chính tiếng Việt (từ cuối cùng)
        var dsGiangVienSapXep = gvGrid.Keys
            .OrderBy(LayTenChinhVietNam, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        // Chuỗi chú giải viết tắt môn học
        string chuGiaiMonHoc = vietTatTheoTenHp.Count > 0
            ? "Chú giải viết tắt môn học: " + string.Join("  |  ", vietTatTheoTenHp
                .OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase)
                .Select(kv => $"{kv.Value}: {kv.Key}"))
            : "Chú giải: Lịch giảng dạy và sinh hoạt chuyên môn trong học kỳ";

        // Dựng workbook ClosedXML trắng đen tối giản
        using var workbook = new XLWorkbook();
        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void CauHinhTrangInA4Ngang(IXLWorksheet ws)
        {
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 1);
            ws.PageSetup.Margins.Left = 0.35;
            ws.PageSetup.Margins.Right = 0.35;
            ws.PageSetup.Margins.Top = 0.45;
            ws.PageSetup.Margins.Bottom = 0.45;
            ws.ShowGridLines = true;
        }

        static void KeVienHeader(IXLCell cell)
        {
            cell.Style.Font.FontName = "Times New Roman";
            cell.Style.Font.FontSize = 10.5;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.Black;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.LeftBorderColor = XLColor.FromHtml("#404040");
            cell.Style.Border.RightBorderColor = XLColor.FromHtml("#404040");
            cell.Style.Border.TopBorderColor = XLColor.Black;
            cell.Style.Border.BottomBorderColor = XLColor.Black;
        }

        static void KeVienO(IXLCell cell)
        {
            cell.Style.Fill.BackgroundColor = XLColor.White;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#404040");
        }

        string[] maTranHeaders = ["Ca học", "Tiết", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];

        // =========================================================
        // SHEET 1: TỔNG HỢP BỘ MÔN
        // =========================================================
        var wsAll = workbook.Worksheets.Add("TongHop_BoMon");
        usedSheetNames.Add("TongHop_BoMon");
        CauHinhTrangInA4Ngang(wsAll);

        wsAll.Cell(1, 1).Value = $"BẢNG TỔNG HỢP LỊCH GIẢNG DẠY & SEMINAR CẢ KỲ ({tenBoMon.ToUpperInvariant()} — {bangPhanCong.TenHocKy.ToUpperInvariant()})";
        var rTitleAll = wsAll.Range(1, 1, 1, 9).Merge();
        rTitleAll.Style.Font.FontName = "Times New Roman";
        rTitleAll.Style.Font.FontSize = 14;
        rTitleAll.Style.Font.Bold = true;
        rTitleAll.Style.Font.FontColor = XLColor.Black;
        rTitleAll.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rTitleAll.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        wsAll.Row(1).Height = 28;

        wsAll.Cell(2, 1).Value = "Định dạng rút gọn: Tên GV - Môn (Mã lớp, Phòng học, Tuần) | Bao gồm cả lịch Seminar";
        var rSubAll = wsAll.Range(2, 1, 2, 9).Merge();
        rSubAll.Style.Font.FontName = "Times New Roman";
        rSubAll.Style.Font.FontSize = 10.5;
        rSubAll.Style.Font.Italic = true;
        rSubAll.Style.Font.FontColor = XLColor.FromHtml("#262626");
        rSubAll.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rSubAll.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        wsAll.Row(2).Height = 20;
        wsAll.Row(3).Height = 8;

        wsAll.Row(4).Height = 26;
        for (int c = 0; c < maTranHeaders.Length; c++)
        {
            var cell = wsAll.Cell(4, c + 1);
            cell.Value = maTranHeaders[c];
            KeVienHeader(cell);
        }

        for (int caIdx = 0; caIdx < cacCaHoc.Length; caIdx++)
        {
            int rowNum = 5 + caIdx;
            int maxLines = 2;

            var cCa = wsAll.Cell(rowNum, 1);
            cCa.Value = cacCaHoc[caIdx].TenCa;
            KeVienO(cCa);
            cCa.Style.Font.FontName = "Times New Roman";
            cCa.Style.Font.FontSize = 10;
            cCa.Style.Font.Bold = true;
            cCa.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cCa.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cCa.Style.Alignment.WrapText = true;

            var cTiet = wsAll.Cell(rowNum, 2);
            cTiet.Value = cacCaHoc[caIdx].TietStr;
            KeVienO(cTiet);
            cTiet.Style.Font.FontName = "Times New Roman";
            cTiet.Style.Font.FontSize = 9.5;
            cTiet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cTiet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cTiet.Style.Alignment.WrapText = true;

            for (int dayIdx = 0; dayIdx < 7; dayIdx++)
            {
                var key = (caIdx, dayIdx);
                var lines = new List<string>();
                if (allCellSeminars.TryGetValue(key, out var semGvs) && semGvs.Count > 0)
                {
                    lines.Add("[SEMINAR] " + string.Join(", ", semGvs));
                }
                if (allCellClasses.TryGetValue(key, out var clsItems))
                {
                    lines.AddRange(clsItems
                        .OrderBy(x => x.TenGv, StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(x => x.NoiDung, StringComparer.CurrentCultureIgnoreCase)
                        .Select(x => x.NoiDung));
                }

                string cellText = string.Join("\n", lines);
                var cell = wsAll.Cell(rowNum, 3 + dayIdx);
                cell.Value = cellText;
                KeVienO(cell);
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 9.5;
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                cell.Style.Alignment.WrapText = true;

                if (lines.Count > 0)
                {
                    int est = lines.Sum(ln => Math.Max(1, (ln.Length / 36) + 1));
                    if (est > maxLines) maxLines = est;
                }
            }
            wsAll.Row(rowNum).Height = Math.Max(36, maxLines * 14.5 + 8);
        }

        wsAll.Row(10).Height = 8;
        wsAll.Cell(11, 1).Value = chuGiaiMonHoc;
        var rLegAll = wsAll.Range(11, 1, 11, 9).Merge();
        rLegAll.Style.Font.FontName = "Times New Roman";
        rLegAll.Style.Font.FontSize = 9.5;
        rLegAll.Style.Font.Italic = true;
        rLegAll.Style.Font.FontColor = XLColor.FromHtml("#262626");
        rLegAll.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        rLegAll.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        rLegAll.Style.Alignment.WrapText = true;
        rLegAll.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rLegAll.Style.Border.OutsideBorderColor = XLColor.FromHtml("#404040");
        wsAll.Row(11).Height = 24;

        double[] colWidthsAll = [15, 11.5, 40, 44, 42, 42, 42, 40, 14];
        for (int i = 0; i < colWidthsAll.Length; i++)
            wsAll.Column(i + 1).Width = colWidthsAll[i];
        wsAll.SheetView.Freeze(4, 2);

        // =========================================================
        // SHEET 2: THỐNG KÊ KHỐI LƯỢNG GIẢNG VIÊN
        // =========================================================
        var wsTk = workbook.Worksheets.Add("ThongKe_GiangVien");
        usedSheetNames.Add("ThongKe_GiangVien");
        CauHinhTrangInA4Ngang(wsTk);

        wsTk.Cell(1, 1).Value = $"BẢNG TỔNG HỢP KHỐI LƯỢNG GIẢNG DẠY & SEMINAR CẢ KỲ ({tenBoMon.ToUpperInvariant()} — {bangPhanCong.TenHocKy.ToUpperInvariant()})";
        var rTitleTk = wsTk.Range(1, 1, 1, 6).Merge();
        rTitleTk.Style.Font.FontName = "Times New Roman";
        rTitleTk.Style.Font.FontSize = 14;
        rTitleTk.Style.Font.Bold = true;
        rTitleTk.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rTitleTk.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        wsTk.Row(1).Height = 28;

        wsTk.Cell(2, 1).Value = $"Tổng số giảng viên: {dsGiangVienSapXep.Count}  |  Tổng buổi và tổng tiết cả kỳ được tính theo đúng số tuần học của từng lớp";
        var rSubTk = wsTk.Range(2, 1, 2, 6).Merge();
        rSubTk.Style.Font.FontName = "Times New Roman";
        rSubTk.Style.Font.FontSize = 10.5;
        rSubTk.Style.Font.Italic = true;
        rSubTk.Style.Font.FontColor = XLColor.FromHtml("#262626");
        rSubTk.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rSubTk.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        wsTk.Row(2).Height = 20;

        string[] tkHeaders = ["STT", "Họ và tên giảng viên", "Số lớp HP", "Tổng số buổi cả kỳ", "Tổng số tiết cả kỳ", "Lịch Seminar"];
        wsTk.Row(4).Height = 26;
        for (int c = 0; c < tkHeaders.Length; c++)
        {
            var cell = wsTk.Cell(4, c + 1);
            cell.Value = tkHeaders[c];
            KeVienHeader(cell);
        }

        int tongLopBm = 0, tongBuoiBm = 0, tongTietBm = 0, soGvCoSeminar = 0;
        for (int i = 0; i < dsGiangVienSapXep.Count; i++)
        {
            int rnum = 5 + i;
            wsTk.Row(rnum).Height = 22;
            string tenGv = dsGiangVienSapXep[i];
            int nLop = gvLopIds[tenGv].Count;
            int nBuoi = gvTongBuoi[tenGv];
            int nTiet = gvTongTiet[tenGv];
            bool coSem = gvSeminarLabels[tenGv].Count > 0;
            string semStr = coSem ? string.Join("; ", gvSeminarLabels[tenGv]) : "Không";

            tongLopBm += nLop;
            tongBuoiBm += nBuoi;
            tongTietBm += nTiet;
            if (coSem) soGvCoSeminar++;

            wsTk.Cell(rnum, 1).Value = i + 1;
            wsTk.Cell(rnum, 2).Value = tenGv;
            wsTk.Cell(rnum, 3).Value = nLop;
            wsTk.Cell(rnum, 4).Value = nBuoi;
            wsTk.Cell(rnum, 5).Value = nTiet;
            wsTk.Cell(rnum, 6).Value = semStr;

            for (int c = 1; c <= 6; c++)
            {
                var cell = wsTk.Cell(rnum, c);
                KeVienO(cell);
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = c == 6 ? 10 : 10.5;
                cell.Style.Font.Bold = (c == 2) || (c == 6 && coSem);
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Alignment.Horizontal = c == 2 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        int totRow = 5 + dsGiangVienSapXep.Count;
        wsTk.Row(totRow).Height = 24;
        wsTk.Cell(totRow, 1).Value = "";
        wsTk.Cell(totRow, 2).Value = "TỔNG CỘNG TOÀN BỘ MÔN";
        wsTk.Cell(totRow, 3).Value = tongLopBm;
        wsTk.Cell(totRow, 4).Value = tongBuoiBm;
        wsTk.Cell(totRow, 5).Value = tongTietBm;
        wsTk.Cell(totRow, 6).Value = $"{soGvCoSeminar} giảng viên tham gia";
        for (int c = 1; c <= 6; c++)
        {
            var cell = wsTk.Cell(totRow, c);
            KeVienHeader(cell);
            cell.Style.Alignment.Horizontal = c == 2 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
        }

        double[] colWidthsTk = [7, 28, 14, 20, 20, 36];
        for (int i = 0; i < colWidthsTk.Length; i++)
            wsTk.Column(i + 1).Width = colWidthsTk[i];

        // =========================================================
        // SHEETS 3..N: LỊCH CÁ NHÂN TỪNG GIẢNG VIÊN
        // =========================================================
        double[] colWidthsGv = [15, 11.5, 28, 28, 28, 28, 28, 28, 14];
        foreach (var tenGv in dsGiangVienSapXep)
        {
            string sheetName = TaoTenSheetHopLe(tenGv, usedSheetNames);
            var ws = workbook.Worksheets.Add(sheetName);
            CauHinhTrangInA4Ngang(ws);

            int nLop = gvLopIds[tenGv].Count;
            int nBuoi = gvTongBuoi[tenGv];
            int nTiet = gvTongTiet[tenGv];
            string semInfo = gvSeminarLabels[tenGv].Count > 0 ? string.Join("; ", gvSeminarLabels[tenGv]) : "Không có";

            ws.Cell(1, 1).Value = $"LỊCH GIẢNG DẠY & SEMINAR CÁ NHÂN — {tenGv.ToUpperInvariant()}";
            var rTitleGv = ws.Range(1, 1, 1, 9).Merge();
            rTitleGv.Style.Font.FontName = "Times New Roman";
            rTitleGv.Style.Font.FontSize = 14;
            rTitleGv.Style.Font.Bold = true;
            rTitleGv.Style.Font.FontColor = XLColor.Black;
            rTitleGv.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rTitleGv.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 28;

            ws.Cell(2, 1).Value = $"{tenBoMon} ({bangPhanCong.TenHocKy})  |  Số lớp phụ trách: {nLop} lớp  |  Tổng số buổi cả kỳ: {nBuoi} buổi ({nTiet} tiết)  |  Seminar: {semInfo}";
            var rSubGv = ws.Range(2, 1, 2, 9).Merge();
            rSubGv.Style.Font.FontName = "Times New Roman";
            rSubGv.Style.Font.FontSize = 10.5;
            rSubGv.Style.Font.Italic = true;
            rSubGv.Style.Font.FontColor = XLColor.FromHtml("#262626");
            rSubGv.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rSubGv.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 20;
            ws.Row(3).Height = 8;

            ws.Row(4).Height = 26;
            for (int c = 0; c < maTranHeaders.Length; c++)
            {
                var cell = ws.Cell(4, c + 1);
                cell.Value = maTranHeaders[c];
                KeVienHeader(cell);
            }

            var grid = gvGrid[tenGv];
            for (int caIdx = 0; caIdx < cacCaHoc.Length; caIdx++)
            {
                int rowNum = 5 + caIdx;
                int maxLines = 3;

                var cCa = ws.Cell(rowNum, 1);
                cCa.Value = cacCaHoc[caIdx].TenCa;
                KeVienO(cCa);
                cCa.Style.Font.FontName = "Times New Roman";
                cCa.Style.Font.FontSize = 10;
                cCa.Style.Font.Bold = true;
                cCa.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cCa.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cCa.Style.Alignment.WrapText = true;

                var cTiet = ws.Cell(rowNum, 2);
                cTiet.Value = cacCaHoc[caIdx].TietStr;
                KeVienO(cTiet);
                cTiet.Style.Font.FontName = "Times New Roman";
                cTiet.Style.Font.FontSize = 9.5;
                cTiet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cTiet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cTiet.Style.Alignment.WrapText = true;

                for (int dayIdx = 0; dayIdx < 7; dayIdx++)
                {
                    var cell = ws.Cell(rowNum, 3 + dayIdx);
                    KeVienO(cell);
                    if (grid.TryGetValue((caIdx, dayIdx), out var entries) && entries.Count > 0)
                    {
                        bool laSem = entries.Any(e => e.StartsWith("[SEMINAR]", StringComparison.Ordinal));
                        cell.Value = string.Join("\n", entries);
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Font.FontSize = 10.5;
                        cell.Style.Font.Bold = laSem;
                        cell.Style.Font.FontColor = XLColor.Black;
                        cell.Style.Alignment.Horizontal = laSem ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Left;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Alignment.WrapText = true;

                        int est = entries.Sum(e => Math.Max(1, (e.Length / 26) + 1));
                        if (est > maxLines) maxLines = est;
                    }
                    else
                    {
                        cell.Value = "";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                }
                ws.Row(rowNum).Height = Math.Max(56, maxLines * 17 + 12);
            }

            ws.Row(10).Height = 8;
            ws.Cell(11, 1).Value = chuGiaiMonHoc;
            var rLegGv = ws.Range(11, 1, 11, 9).Merge();
            rLegGv.Style.Font.FontName = "Times New Roman";
            rLegGv.Style.Font.FontSize = 9.5;
            rLegGv.Style.Font.Italic = true;
            rLegGv.Style.Font.FontColor = XLColor.FromHtml("#262626");
            rLegGv.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            rLegGv.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rLegGv.Style.Alignment.WrapText = true;
            rLegGv.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rLegGv.Style.Border.OutsideBorderColor = XLColor.FromHtml("#404040");
            ws.Row(11).Height = 24;

            for (int i = 0; i < colWidthsGv.Length; i++)
                ws.Column(i + 1).Width = colWidthsGv[i];
            ws.SheetView.Freeze(4, 2);
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string LayTenChinhVietNam(string hoTen)
    {
        var parts = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : hoTen;
    }

    private static string TaoTenSheetHopLe(string rawName, HashSet<string> usedNames)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        string cleaned = new string(rawName.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrEmpty(cleaned)) cleaned = "GiangVien";
        if (cleaned.Length > 31) cleaned = cleaned[..31].Trim();

        string candidate = cleaned;
        int suffix = 2;
        while (!usedNames.Add(candidate))
        {
            string tag = $" ({suffix++})";
            candidate = cleaned[..Math.Min(cleaned.Length, 31 - tag.Length)] + tag;
        }
        return candidate;
    }

    private static Dictionary<string, string> TaoBangVietTatHocPhan(List<string> dsTenHocPhan)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedAbbr = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static string? TraVietTatChuan(string tenHp)
        {
            string norm = tenHp.Trim().ToLowerInvariant();
            return norm switch
            {
                "giải tích 1" => "GT1",
                "giải tích 2" => "GT2",
                "giải tích 3" => "GT3",
                "giải tích ứng dụng kỹ thuật" or "giải tích ứng dụng" => "GTUD",
                "đại số tuyến tính kỹ thuật" => "ĐSKT",
                "đại số tuyến tính" => "ĐSTT",
                "nhập môn giải tích kỹ thuật" or "nhập môn giải tích" => "NMGT",
                "xác suất thống kê" or "xác suất và thống kê" => "XSTK",
                _ => null
            };
        }

        foreach (var tenHp in dsTenHocPhan)
        {
            string abbr = TraVietTatChuan(tenHp) ?? SinhVietTatTuDong(tenHp);
            string uniqueAbbr = abbr;
            int idx = 2;
            while (!usedAbbr.Add(uniqueAbbr))
            {
                uniqueAbbr = $"{abbr}{idx++}";
            }
            result[tenHp] = uniqueAbbr;
        }
        return result;
    }

    private static string SinhVietTatTuDong(string tenHp)
    {
        var tokens = tenHp.Split([' ', '-', '_', '(', ')', ',', '/'], StringSplitOptions.RemoveEmptyEntries);
        var chars = new List<string>();
        foreach (var tok in tokens)
        {
            if (tok.All(char.IsDigit))
                chars.Add(tok);
            else
                chars.Add(char.ToUpperInvariant(tok[0]).ToString());
        }
        string raw = string.Concat(chars);
        return string.IsNullOrWhiteSpace(raw) ? tenHp[..Math.Min(6, tenHp.Length)].ToUpperInvariant() : raw;
    }
}
