namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class KhoiTaoQuanThe
{
    private readonly Random _random;

    public KhoiTaoQuanThe(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Khởi tạo quần thể ban đầu kết hợp Heuristic Seeding và Random để vừa có cá thể chất lượng cao, vừa bảo tồn đa dạng di truyền.
    /// Nếu có lớp nào không có bất kỳ ứng viên nào, ném lỗi rõ ràng chỉ rõ mã lớp.
    /// </summary>
    public List<CaThe> TaoQuanTheBanDau(
        int kichThuocQuanThe,
        List<int> danhSachMaLop,
        Dictionary<int, List<int>> ungVienTheoLop,
        Dictionary<int, int>? phanCongCoDinh = null,
        Dictionary<int, List<ThoiGianLop>>? thoiGianTheoLop = null,
        Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv = null,
        bool choPhepLopBoTrong = false)
    {
        if (danhSachMaLop == null || danhSachMaLop.Count == 0)
        {
            return [];
        }

        // Kiểm tra tiền điều kiện: mọi lớp chưa khóa phải có ít nhất 1 ứng viên (trừ khi cho phép bỏ trống do lịch bận/Seminar khóa cứng)
        if (!choPhepLopBoTrong)
        {
            foreach (var maLop in danhSachMaLop)
            {
                bool daKhoa = phanCongCoDinh != null && phanCongCoDinh.TryGetValue(maLop, out var gvKhoa) && gvKhoa > 0;
                if (!daKhoa)
                {
                    if (!ungVienTheoLop.TryGetValue(maLop, out var dsUngVien) || dsUngVien == null || dsUngVien.Count == 0)
                    {
                        throw new InvalidOperationException($"Lớp học phần ID {maLop} không có bất kỳ giảng viên ứng viên đủ điều kiện chuyên môn nào.");
                    }
                }
            }
        }

        var quanThe = new List<CaThe>(kichThuocQuanThe);

        // 1. TẠO CÁ THỂ HEURISTIC HẠT GIỐNG (Nếu có thông tin thời gian lớp)
        if (thoiGianTheoLop != null && thoiGianTheoLop.Count > 0 && kichThuocQuanThe >= 2)
        {
            var caTheHeuristic = TaoCaTheHeuristicLeastLoaded(
                danhSachMaLop, ungVienTheoLop, phanCongCoDinh, thoiGianTheoLop, lichBanTheoGv);
            quanThe.Add(caTheHeuristic);
        }

        // 2. KHỞI TẠO CÁC CÁ THỂ CÒN LẠI BẰNG RANDOM TỪ TẬP ỨNG VIÊN HỢP LỆ
        while (quanThe.Count < kichThuocQuanThe)
        {
            var caThe = new CaThe();
            foreach (var maLop in danhSachMaLop)
            {
                int? maGv = null;

                // Nếu lớp đã bị khóa/ghim cố định thì giữ nguyên
                if (phanCongCoDinh != null && phanCongCoDinh.TryGetValue(maLop, out var gvKhoa) && gvKhoa > 0)
                {
                    maGv = gvKhoa;
                }
                else if (ungVienTheoLop.TryGetValue(maLop, out var dsUngVien) && dsUngVien.Count > 0)
                {
                    maGv = dsUngVien[_random.Next(dsUngVien.Count)];
                }

                caThe.DanhSachGen.Add(new Gen
                {
                    MaLopHocPhan = maLop,
                    MaGiangVien = maGv
                });
            }
            quanThe.Add(caThe);
        }

        return quanThe;
    }

    /// <summary>
    /// Tạo 1 cá thể Heuristic hạt giống: Ưu tiên lớp có ít ứng viên hơn (MRV),
    /// gán cho giảng viên ít tải nhất mà không gây trùng lịch và không vướng lịch bận.
    /// </summary>
    private static CaThe TaoCaTheHeuristicLeastLoaded(
        List<int> danhSachMaLop,
        Dictionary<int, List<int>> ungVienTheoLop,
        Dictionary<int, int>? phanCongCoDinh,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop,
        Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv)
    {
        var caThe = new CaThe();
        var mapKetQua = new Dictionary<int, int>();
        var lopTheoGv = new Dictionary<int, List<int>>();
        var soTietTheoGv = new Dictionary<int, int>();

        // 1. Xử lý các lớp đã khóa trước
        foreach (var maLop in danhSachMaLop)
        {
            if (phanCongCoDinh != null && phanCongCoDinh.TryGetValue(maLop, out var gvKhoa) && gvKhoa > 0)
            {
                mapKetQua[maLop] = gvKhoa;
                if (!lopTheoGv.TryGetValue(gvKhoa, out var ds))
                {
                    ds = [];
                    lopTheoGv[gvKhoa] = ds;
                }
                ds.Add(maLop);

                int soTiet = 0;
                if (thoiGianTheoLop.TryGetValue(maLop, out var tgLop))
                {
                    soTiet = tgLop.Sum(t => Math.Max(0, t.TietKetThuc - t.TietBatDau + 1));
                }
                soTietTheoGv[gvKhoa] = soTietTheoGv.GetValueOrDefault(gvKhoa, 0) + soTiet;
            }
        }

        // 2. Sắp xếp các lớp chưa khóa: lớp ít ứng viên hơn xếp trước (MRV)
        var cacLopChuaKhoa = danhSachMaLop
            .Where(l => !mapKetQua.ContainsKey(l))
            .OrderBy(l => ungVienTheoLop.TryGetValue(l, out var uvs) ? uvs.Count : 999)
            .ToList();

        foreach (var maLop in cacLopChuaKhoa)
        {
            thoiGianTheoLop.TryGetValue(maLop, out var tgLop);
            tgLop ??= [];

            if (!ungVienTheoLop.TryGetValue(maLop, out var dsUv) || dsUv.Count == 0)
            {
                mapKetQua[maLop] = 0;
                continue;
            }

            int gvTotNhat = 0;
            int minTai = int.MaxValue;
            bool timThayKhongXungDot = false;

            // Duyệt tìm ứng viên không xung đột và có tải thấp nhất
            foreach (var uvId in dsUv)
            {
                // Kiểm tra lịch bận
                bool biBan = false;
                if (lichBanTheoGv != null && lichBanTheoGv.TryGetValue(uvId, out var dsBan))
                {
                    biBan = tgLop.Any(tLop => dsBan.Any(tBan => tBan.TrungGioVoi(tLop)));
                }
                if (biBan) continue;

                // Kiểm tra trùng giờ với lớp sẵn có của uvId
                bool biTrung = false;
                if (lopTheoGv.TryGetValue(uvId, out var dsLopSanCo))
                {
                    foreach (var maSc in dsLopSanCo)
                    {
                        if (thoiGianTheoLop.TryGetValue(maSc, out var tgSc))
                        {
                            if (tgLop.Any(t1 => tgSc.Any(t2 => t1.TrungGioVoi(t2))))
                            {
                                biTrung = true;
                                break;
                            }
                        }
                    }
                }
                if (biTrung) continue;

                int taiHienTai = soTietTheoGv.GetValueOrDefault(uvId, 0);
                if (taiHienTai < minTai)
                {
                    minTai = taiHienTai;
                    gvTotNhat = uvId;
                    timThayKhongXungDot = true;
                }
            }

            // Nếu không có ứng viên nào thỏa mãn 100%, chọn ứng viên có tải thấp nhất
            if (!timThayKhongXungDot)
            {
                gvTotNhat = dsUv.OrderBy(u => soTietTheoGv.GetValueOrDefault(u, 0)).First();
            }

            mapKetQua[maLop] = gvTotNhat;
            if (!lopTheoGv.TryGetValue(gvTotNhat, out var dsMoi))
            {
                dsMoi = [];
                lopTheoGv[gvTotNhat] = dsMoi;
            }
            dsMoi.Add(maLop);

            int soTietLop = tgLop.Sum(t => Math.Max(0, t.TietKetThuc - t.TietBatDau + 1));
            soTietTheoGv[gvTotNhat] = soTietTheoGv.GetValueOrDefault(gvTotNhat, 0) + soTietLop;
        }

        // Đóng gói cá thể theo đúng thứ tự danhSachMaLop ban đầu
        foreach (var maLop in danhSachMaLop)
        {
            int gvVal = mapKetQua.GetValueOrDefault(maLop, 0);
            caThe.DanhSachGen.Add(new Gen
            {
                MaLopHocPhan = maLop,
                MaGiangVien = gvVal > 0 ? gvVal : null
            });
        }

        return caThe;
    }
}


