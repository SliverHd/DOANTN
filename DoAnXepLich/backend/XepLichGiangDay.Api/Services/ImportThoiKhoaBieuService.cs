using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.Import;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class ImportThoiKhoaBieuService(AppDbContext context)
{
    public async Task<KetQuaPreviewImportDto> PreviewAsync(IFormFile file, int maHocKy, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra học kỳ tồn tại
        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == maHocKy, cancellationToken);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        // 2. Kiểm tra phần mở rộng file
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            throw new ArgumentException("Dinh dang file khong hop le. Chi chap nhan file Excel (.xlsx, .xls).");
        }

        var ketQua = new KetQuaPreviewImportDto();
        var dsHocPhan = await context.HocPhan.AsNoTracking().ToListAsync(cancellationToken);

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                throw new ArgumentException("File Excel không chứa trang tính (worksheet) hợp lệ.");
            }

            var allRows = worksheet.RowsUsed().ToList();
            if (allRows.Count == 0)
            {
                throw new ArgumentException("File Excel không chứa dữ liệu hợp lệ.");
            }

            // 1. Tự động tìm các dòng header (quét từ dòng 1 đến 15)
            var colMap = new Dictionary<string, int>();
            int dataStartRowIdx = -1;

            for (int r = 0; r < Math.Min(15, allRows.Count); r++)
            {
                var row = allRows[r];
                var cellTexts = row.CellsUsed().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();
                bool coHocPhan = cellTexts.Any(t => t.Contains("học phần") || t.Contains("hoc phan") || t.Contains("mã môn") || t.Contains("ma mon") || t.Contains("tên môn") || t.Contains("ten mon"));
                bool coLop = cellTexts.Any(t => t.Contains("mã lớp") || t.Contains("ma lop") || t.Contains("lớp") || t.Contains("lop"));
                bool coThu = cellTexts.Any(t => t.Contains("thứ") || t.Contains("thu"));
                bool coTiet = cellTexts.Any(t => t.Contains("tiết") || t.Contains("tiet"));

                if ((coHocPhan && coThu) || (coHocPhan && coTiet) || (coHocPhan && coLop) || (coThu && coTiet))
                {
                    foreach (var cell in row.CellsUsed())
                    {
                        var text = cell.GetString().Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(text) && !colMap.ContainsKey(text))
                            colMap[text] = cell.Address.ColumnNumber;
                    }

                    dataStartRowIdx = r + 1;

                    // Nếu dòng kế tiếp cũng là tiêu đề con (chứa Thứ, Tiết, Phòng học, Bắt đầu, v.v.)
                    if (r + 1 < allRows.Count)
                    {
                        var nextRow = allRows[r + 1];
                        var nextTexts = nextRow.CellsUsed().Select(c => c.GetString().Trim().ToLowerInvariant()).ToList();
                        if (nextTexts.Any(t => t.Contains("thứ") || t.Contains("thu") || t.Contains("tiết") || t.Contains("tiet") || t.Contains("phòng") || t.Contains("phong") || t.Contains("bắt đầu") || t.Contains("kết thúc")))
                        {
                            foreach (var cell in nextRow.CellsUsed())
                            {
                                var text = cell.GetString().Trim().ToLowerInvariant();
                                if (!string.IsNullOrEmpty(text) && !colMap.ContainsKey(text))
                                    colMap[text] = cell.Address.ColumnNumber;
                            }
                            dataStartRowIdx = r + 2;
                        }
                    }
                    break;
                }
            }

            // 2. Xác định vị trí cột
            int colMaHp = 2, colTenMon = 3, colMaLop = 4, colTenLop = 5, colThu = 6, colTiet = 7, colTietKetThuc = 8, colPhong = 8, colSoLuongSv = -1, colTuanHoc = 13, colTuTuan = -1, colDenTuan = -1;
            bool singleTietCol = true;

            if (colMap.Count > 0)
            {
                int TimCot(params string[] keywords)
                {
                    foreach (var kw in keywords)
                    {
                        foreach (var kv in colMap)
                        {
                            if (kv.Key.Contains(kw)) return kv.Value;
                        }
                    }
                    return -1;
                }

                int fMaHp = TimCot("mã học phần", "ma hoc phan", "mã môn", "ma mon");
                int fTenMon = TimCot("tên môn", "ten mon", "tên học phần", "ten hoc phan");
                int fMaLop = TimCot("mã lớp học", "ma lop hoc", "mã lớp hp", "ma lop hp", "mã lớp", "ma lop");
                int fTenLop = TimCot("lớp ghép", "lop ghep", "tên lớp", "ten lop");
                int fThu = TimCot("thứ", "thu");
                int fTiet = TimCot("tiết", "tiet");
                int fTietBatDau = TimCot("tiết bắt đầu", "tiet bat dau", "tiết đầu", "tiet dau");
                int fTietKetThuc = TimCot("tiết kết thúc", "tiet ket thuc", "tiết cuối", "tiet cuoi");
                int fPhong = TimCot("phòng", "phong");
                int fSoLuong = TimCot("số lượng", "so luong", "sĩ số", "si so");
                int fTuanHoc = TimCot("tuần học", "tuan hoc");
                int fTuTuan = TimCot("từ tuần", "tu tuan");
                int fDenTuan = TimCot("đến tuần", "den tuan");

                if (fMaHp > 0) colMaHp = fMaHp;
                if (fTenMon > 0) colTenMon = fTenMon;
                if (fMaLop > 0) colMaLop = fMaLop;
                if (fTenLop > 0) colTenLop = fTenLop;
                if (fThu > 0) colThu = fThu;
                if (fPhong > 0) colPhong = fPhong;
                if (fSoLuong > 0) colSoLuongSv = fSoLuong;
                if (fTuanHoc > 0) colTuanHoc = fTuanHoc;

                if (fTietBatDau > 0 && fTietKetThuc > 0)
                {
                    colTiet = fTietBatDau;
                    colTietKetThuc = fTietKetThuc;
                    singleTietCol = false;
                }
                else if (fTiet > 0)
                {
                    colTiet = fTiet;
                    singleTietCol = true;
                }

                if (fTuTuan > 0 && fDenTuan > 0)
                {
                    colTuTuan = fTuTuan;
                    colDenTuan = fDenTuan;
                }
            }

            var rows = allRows.Skip(dataStartRowIdx >= 0 ? dataStartRowIdx : 1).ToList();
            if (rows.Count == 0)
            {
                throw new ArgumentException("File Excel không chứa dữ liệu lớp học phần sau dòng tiêu đề.");
            }

            int stt = 1;

            foreach (var row in rows)
            {
                if (!row.CellsUsed().Any()) continue;

                string rawMaHp = colMaHp > 0 ? row.Cell(colMaHp).GetString().Trim() : "";
                string rawTenMon = colTenMon > 0 ? row.Cell(colTenMon).GetString().Trim() : "";
                string rawMaLop = colMaLop > 0 ? row.Cell(colMaLop).GetString().Trim() : "";
                string rawTenLop = colTenLop > 0 ? row.Cell(colTenLop).GetString().Trim() : "";
                string rawPhong = colPhong > 0 ? row.Cell(colPhong).GetString().Trim() : "";

                if (string.IsNullOrWhiteSpace(rawMaHp) && string.IsNullOrWhiteSpace(rawTenMon) && string.IsNullOrWhiteSpace(rawMaLop))
                {
                    continue;
                }

                string maLopHpTruong = !string.IsNullOrWhiteSpace(rawMaLop)
                    ? (!string.IsNullOrWhiteSpace(rawMaHp) && rawMaLop != rawMaHp ? $"{rawMaHp}_{rawMaLop}" : rawMaLop)
                    : rawMaHp;

                string tenLop = !string.IsNullOrWhiteSpace(rawTenLop) ? rawTenLop : (!string.IsNullOrWhiteSpace(rawMaLop) ? rawMaLop : rawTenMon);

                // Parse Thứ
                int thu = 0;
                if (colThu > 0)
                {
                    var thuStr = row.Cell(colThu).GetString().Trim();
                    if (int.TryParse(thuStr, out var t)) thu = t;
                    else if (thuStr.Contains("hai", StringComparison.OrdinalIgnoreCase)) thu = 2;
                    else if (thuStr.Contains("ba", StringComparison.OrdinalIgnoreCase)) thu = 3;
                    else if (thuStr.Contains("tư", StringComparison.OrdinalIgnoreCase) || thuStr.Contains("tu", StringComparison.OrdinalIgnoreCase)) thu = 4;
                    else if (thuStr.Contains("năm", StringComparison.OrdinalIgnoreCase) || thuStr.Contains("nam", StringComparison.OrdinalIgnoreCase)) thu = 5;
                    else if (thuStr.Contains("sáu", StringComparison.OrdinalIgnoreCase) || thuStr.Contains("sau", StringComparison.OrdinalIgnoreCase)) thu = 6;
                    else if (thuStr.Contains("bảy", StringComparison.OrdinalIgnoreCase) || thuStr.Contains("bay", StringComparison.OrdinalIgnoreCase)) thu = 7;
                    else if (thuStr.Contains("nhật", StringComparison.OrdinalIgnoreCase) || thuStr.Contains("nhat", StringComparison.OrdinalIgnoreCase)) thu = 8;
                }

                // Parse Tiết
                int tietBatDau = 0, tietKetThuc = 0;
                if (singleTietCol && colTiet > 0)
                {
                    var tietStr = row.Cell(colTiet).GetString().Trim();
                    var parts = tietStr.Split(new[] { '-', ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[0], out var tbd) && int.TryParse(parts[1], out var tkt))
                    {
                        tietBatDau = tbd;
                        tietKetThuc = tkt;
                    }
                    else if (parts.Length == 1 && int.TryParse(parts[0], out var t1))
                    {
                        tietBatDau = t1;
                        tietKetThuc = t1;
                    }
                }
                else
                {
                    if (colTiet > 0 && row.Cell(colTiet).TryGetValue<int>(out var tbd)) tietBatDau = tbd;
                    if (colTietKetThuc > 0 && row.Cell(colTietKetThuc).TryGetValue<int>(out var tkt)) tietKetThuc = tkt;
                }

                // Parse Tuần
                int tuTuan = 1, denTuan = 16;
                if (colTuanHoc > 0)
                {
                    var cellTuan = row.Cell(colTuanHoc);
                    string tuanStr = cellTuan.GetText();
                    if (string.IsNullOrEmpty(tuanStr))
                    {
                        tuanStr = cellTuan.GetString();
                    }
                    if (!string.IsNullOrWhiteSpace(tuanStr))
                    {
                        int firstWeek = -1;
                        int lastWeek = -1;
                        for (int i = 0; i < tuanStr.Length; i++)
                        {
                            char ch = tuanStr[i];
                            if (ch != ' ' && ch != '-' && ch != '_')
                            {
                                if (firstWeek == -1) firstWeek = i + 1;
                                lastWeek = i + 1;
                            }
                        }

                        if (firstWeek > 0 && lastWeek >= firstWeek)
                        {
                            tuTuan = firstWeek;
                            denTuan = lastWeek;
                        }
                        else if (int.TryParse(tuanStr.Trim(), out var singleTuan) && singleTuan > 0 && singleTuan <= 53)
                        {
                            tuTuan = singleTuan;
                            denTuan = singleTuan;
                        }
                    }
                }
                else if (colTuTuan > 0 && colDenTuan > 0)
                {
                    if (row.Cell(colTuTuan).TryGetValue<int>(out var tt) && tt > 0 && tt <= 53) tuTuan = tt;
                    if (row.Cell(colDenTuan).TryGetValue<int>(out var dt) && dt > 0 && dt <= 53) denTuan = dt;
                }

                int soLuongSv = 40;
                if (colSoLuongSv > 0 && row.Cell(colSoLuongSv).TryGetValue<int>(out var sl) && sl > 0)
                {
                    soLuongSv = sl;
                }

                var dto = new DongThoiKhoaBieuImportDto
                {
                    SoThuTu = stt++,
                    MaLopHocPhanTruong = maLopHpTruong,
                    TenLop = tenLop,
                    MaHocPhanTruong = rawMaHp,
                    TenHocPhan = rawTenMon,
                    SoLuongSinhVien = soLuongSv,
                    Thu = thu,
                    TietBatDau = tietBatDau,
                    TietKetThuc = tietKetThuc,
                    PhongHoc = rawPhong,
                    TuTuan = tuTuan,
                    DenTuan = denTuan,
                };

                // Kiểm tra mã lớp học phần
                if (string.IsNullOrWhiteSpace(dto.MaLopHocPhanTruong))
                {
                    dto.DanhSachLoi.Add("Mã lớp học phần không được để trống.");
                }

                // Kiểm tra thứ trong tuần
                if (dto.Thu < 2 || dto.Thu > 8)
                {
                    dto.DanhSachLoi.Add("Thứ phải từ 2 đến 8 (8 là Chủ nhật).");
                }

                // Kiểm tra tiết bắt đầu & kết thúc (đại học có ca tối từ 13 đến 17)
                if (dto.TietBatDau < 1 || dto.TietBatDau > 17)
                {
                    dto.DanhSachLoi.Add("Tiết bắt đầu phải từ 1 đến 17.");
                }

                if (dto.TietKetThuc < dto.TietBatDau || dto.TietKetThuc > 17)
                {
                    dto.DanhSachLoi.Add("Tiết kết thúc phải từ tiết bắt đầu đến 17.");
                }

                // Kiểm tra tuần học
                if (dto.TuTuan < 1 || dto.TuTuan > 53)
                {
                    dto.DanhSachLoi.Add("Từ tuần phải từ 1 đến 53.");
                }

                if (dto.DenTuan < dto.TuTuan || dto.DenTuan > 53)
                {
                    dto.DanhSachLoi.Add("Đến tuần phải lớn hơn hoặc bằng từ tuần và tối đa 53.");
                }

                // Khớp học phần trong hệ thống
                int.TryParse(dto.MaHocPhanTruong, out int maHpInt);
                var hp = dsHocPhan.FirstOrDefault(x =>
                    (maHpInt > 0 && x.MaHocPhan == maHpInt) ||
                    (!string.IsNullOrWhiteSpace(dto.TenHocPhan) && x.TenHocPhan.Equals(dto.TenHocPhan, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(dto.MaHocPhanTruong) && x.TenHocPhan.Equals(dto.MaHocPhanTruong, StringComparison.OrdinalIgnoreCase)));

                if (hp != null)
                {
                    dto.TenHocPhan = hp.TenHocPhan;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.TenHocPhan))
                    {
                        dto.TenHocPhan = dto.MaHocPhanTruong;
                    }
                }

                dto.HopLe = dto.DanhSachLoi.Count == 0;
                if (dto.HopLe)
                {
                    ketQua.SoDongHopLe++;
                }
                else
                {
                    ketQua.SoDongLoi++;
                }

                ketQua.ChiTiet.Add(dto);
            }

            ketQua.TongSoDong = ketQua.ChiTiet.Count;
            return ketQua;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (ext == ".xls" || ex.Message.Contains("corrupted", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("package", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("File bạn tải lên là định dạng Excel cũ (.xls). Vui lòng mở file trong Microsoft Excel và chọn File -> Save As (Lưu dưới dạng) -> Excel Workbook (*.xlsx) rồi tải lại lên hệ thống.");
            }
            throw new InvalidOperationException($"Không thể đọc file Excel: {ex.Message}");
        }
    }

    public async Task<int> XacNhanImportAsync(XacNhanImportRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra học kỳ tồn tại
        var hocKyTonTai = await context.HocKy.AnyAsync(x => x.MaHocKy == request.MaHocKy, cancellationToken);
        if (!hocKyTonTai)
        {
            throw new ArgumentException("Hoc ky khong ton tai trong he thong.");
        }

        var dsHopLe = request.DanhSachDong.Where(x => x.HopLe).ToList();
        if (dsHopLe.Count == 0)
        {
            throw new ArgumentException("Khong co dong hop le nao de import.");
        }

        var dsHocPhan = await context.HocPhan.ToListAsync(cancellationToken);
        int soDongThanhCong = 0;

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var dong in dsHopLe)
            {
                // Khớp học phần trong hệ thống (nếu chưa có thì tự động tạo mới khi import)
                int.TryParse(dong.MaHocPhanTruong, out int maHpInt);
                var hp = dsHocPhan.FirstOrDefault(x =>
                    (maHpInt > 0 && x.MaHocPhan == maHpInt) ||
                    (!string.IsNullOrWhiteSpace(dong.TenHocPhan) && x.TenHocPhan.Equals(dong.TenHocPhan, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(dong.MaHocPhanTruong) && x.TenHocPhan.Equals(dong.MaHocPhanTruong, StringComparison.OrdinalIgnoreCase)));

                if (hp == null)
                {
                    string tenHpMoi = !string.IsNullOrWhiteSpace(dong.TenHocPhan) ? dong.TenHocPhan : dong.MaHocPhanTruong;
                    if (string.IsNullOrWhiteSpace(tenHpMoi)) tenHpMoi = "Hoc phan " + dong.MaLopHocPhanTruong;

                    hp = new HocPhan
                    {
                        TenHocPhan = tenHpMoi,
                        SoTinChi = 3,
                        TrangThai = "DangApDung"
                    };
                    context.HocPhan.Add(hp);
                    await context.SaveChangesAsync(cancellationToken);
                    dsHocPhan.Add(hp);
                }

                int maHocPhan = hp.MaHocPhan;

                // Tìm hoặc tạo Lớp học phần (tránh nhân bản khi re-import)
                var lop = await context.LopHocPhan.FirstOrDefaultAsync(
                    x => x.MaHocKy == request.MaHocKy && x.MaLopHocPhanTruong == dong.MaLopHocPhanTruong,
                    cancellationToken);

                if (lop == null)
                {
                    lop = new LopHocPhan
                    {
                        MaLopHocPhanTruong = dong.MaLopHocPhanTruong,
                        MaHocPhan = maHocPhan,
                        MaHocKy = request.MaHocKy,
                        TenLop = string.IsNullOrWhiteSpace(dong.TenLop) ? dong.MaLopHocPhanTruong : dong.TenLop,
                        SoLuongSinhVien = dong.SoLuongSinhVien > 0 ? dong.SoLuongSinhVien : 40,
                        TrangThai = "DangMo"
                    };
                    context.LopHocPhan.Add(lop);
                    await context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Cập nhật thông tin nếu có thay đổi
                    lop.MaHocPhan = maHocPhan;
                    if (!string.IsNullOrWhiteSpace(dong.TenLop)) lop.TenLop = dong.TenLop;
                    if (dong.SoLuongSinhVien > 0) lop.SoLuongSinhVien = dong.SoLuongSinhVien;
                    await context.SaveChangesAsync(cancellationToken);
                }

                // Thêm chi tiết thời khóa biểu nếu chưa có buổi học tương tự
                var daCoChiTiet = await context.ChiTietThoiKhoaBieu.AnyAsync(
                    x => x.MaLopHocPhan == lop.MaLopHocPhan &&
                         x.Thu == dong.Thu &&
                         x.TietBatDau == dong.TietBatDau &&
                         x.TietKetThuc == dong.TietKetThuc &&
                         x.TuTuan == dong.TuTuan &&
                         x.DenTuan == dong.DenTuan,
                    cancellationToken);

                if (!daCoChiTiet)
                {
                    var chiTiet = new ChiTietThoiKhoaBieu
                    {
                        MaLopHocPhan = lop.MaLopHocPhan,
                        Thu = dong.Thu,
                        TietBatDau = dong.TietBatDau,
                        TietKetThuc = dong.TietKetThuc,
                        PhongHoc = dong.PhongHoc,
                        TuTuan = dong.TuTuan,
                        DenTuan = dong.DenTuan
                    };
                    context.ChiTietThoiKhoaBieu.Add(chiTiet);
                    await context.SaveChangesAsync(cancellationToken);
                }

                soDongThanhCong++;
            }

            // Lưu lịch sử import
            var lichSu = new LichSuImport
            {
                MaHocKy = request.MaHocKy,
                TenFile = string.IsNullOrWhiteSpace(request.TenFile) ? "ThoiKhoaBieu.xlsx" : request.TenFile,
                NgayImport = DateTime.Now,
                TongSoDong = request.DanhSachDong.Count,
                SoDongThanhCong = soDongThanhCong,
                SoDongLoi = request.DanhSachDong.Count - soDongThanhCong,
                TrangThai = soDongThanhCong == request.DanhSachDong.Count ? "ThanhCong" : (soDongThanhCong > 0 ? "MotPhan" : "Loi"),
                GhiChu = $"Import thanh cong {soDongThanhCong}/{request.DanhSachDong.Count} dong."
            };

            context.LichSuImport.Add(lichSu);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return soDongThanhCong;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<LichSuImportDto>> LayLichSuImportAsync(int maHocKy)
    {
        return await context.LichSuImport
            .Where(x => x.MaHocKy == maHocKy)
            .OrderByDescending(x => x.NgayImport)
            .Select(x => new LichSuImportDto
            {
                MaLichSu = x.MaLichSu,
                MaHocKy = x.MaHocKy,
                TenFile = x.TenFile,
                NgayImport = x.NgayImport,
                TongSoDong = x.TongSoDong,
                SoDongThanhCong = x.SoDongThanhCong,
                SoDongLoi = x.SoDongLoi,
                TrangThai = x.TrangThai,
                GhiChu = x.GhiChu
            })
            .ToListAsync();
    }

    public byte[] TaoFileMauExcel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("ThoiKhoaBieu");

        // Tiêu đề cột
        ws.Cell(1, 1).Value = "Mã lớp HP";
        ws.Cell(1, 2).Value = "Tên lớp";
        ws.Cell(1, 3).Value = "Mã môn học";
        ws.Cell(1, 4).Value = "Tên môn học";
        ws.Cell(1, 5).Value = "Số lượng SV";
        ws.Cell(1, 6).Value = "Thứ";
        ws.Cell(1, 7).Value = "Tiết bắt đầu";
        ws.Cell(1, 8).Value = "Tiết kết thúc";
        ws.Cell(1, 9).Value = "Phòng học";
        ws.Cell(1, 10).Value = "Từ tuần";
        ws.Cell(1, 11).Value = "Đến tuần";

        // Dữ liệu mẫu dòng 1
        ws.Cell(2, 1).Value = "IT101_01";
        ws.Cell(2, 2).Value = "Lập trình C# - K20A";
        ws.Cell(2, 3).Value = "1";
        ws.Cell(2, 4).Value = "Nhập môn lập trình";
        ws.Cell(2, 5).Value = 45;
        ws.Cell(2, 6).Value = 2;
        ws.Cell(2, 7).Value = 1;
        ws.Cell(2, 8).Value = 3;
        ws.Cell(2, 9).Value = "A101";
        ws.Cell(2, 10).Value = 1;
        ws.Cell(2, 11).Value = 15;

        // Dữ liệu mẫu dòng 2
        ws.Cell(3, 1).Value = "IT102_01";
        ws.Cell(3, 2).Value = "Cơ sở dữ liệu - K20B";
        ws.Cell(3, 3).Value = "2";
        ws.Cell(3, 4).Value = "Cơ sở dữ liệu";
        ws.Cell(3, 5).Value = 40;
        ws.Cell(3, 6).Value = 4;
        ws.Cell(3, 7).Value = 4;
        ws.Cell(3, 8).Value = 6;
        ws.Cell(3, 9).Value = "B202";
        ws.Cell(3, 10).Value = 1;
        ws.Cell(3, 11).Value = 15;

        ws.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }
}
