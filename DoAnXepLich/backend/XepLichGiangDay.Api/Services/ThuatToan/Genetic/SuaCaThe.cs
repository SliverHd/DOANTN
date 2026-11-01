namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class ThoiGianLop
{
    public int MaLopHocPhan { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public int TuTuan { get; set; } = 1;
    public int DenTuan { get; set; } = 53;

    public bool TrungGioVoi(ThoiGianLop other)
    {
        if (Thu != other.Thu) return false;
        bool giaoTuan = Math.Max(TuTuan, other.TuTuan) <= Math.Min(DenTuan, other.DenTuan);
        if (!giaoTuan) return false;
        return !(TietKetThuc < other.TietBatDau || TietBatDau > other.TietKetThuc);
    }
}

public class ThoiGianBan
{
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }

    public bool TrungGioVoi(ThoiGianLop lop)
    {
        if (Thu != lop.Thu) return false;
        return Math.Max(TietBatDau, lop.TietBatDau) <= Math.Min(TietKetThuc, lop.TietKetThuc);
    }
}

public class SuaCaThe
{
    private readonly Random _random;

    public SuaCaThe(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Toán tử sửa cá thể toàn diện (Comprehensive Repair Operator):
    /// 1. Quét và sửa các lớp bị vướng lịch bận cố định của giảng viên.
    /// 2. Quét và sửa các lớp trùng lịch giữa 2 lớp của cùng một giảng viên (hỗ trợ thử đổi cả 2 chiều).
    /// 3. Khi chọn ứng viên thay thế: Đảm bảo người mới KHÔNG bị trùng lịch với các lớp sẵn có VÀ KHÔNG vướng lịch bận cố định.
    /// 4. Bảo toàn tuyệt đối các lớp đã bị khóa ghim (cacLopCoDinh).
    /// </summary>
    public CaThe SuaNeuViPham(
        CaThe caThe,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop,
        Dictionary<int, List<int>> ungVienTheoLop,
        HashSet<int>? cacLopCoDinh = null,
        Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv = null,
        int maxPasses = 5)
    {
        if (caThe == null || caThe.DanhSachGen.Count == 0) return caThe!;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool coThayDoi = false;

            // Xây dựng bản đồ phân công hiện tại
            var lopTheoGv = new Dictionary<int, List<int>>();
            var genTheoLop = new Dictionary<int, Gen>();

            foreach (var gen in caThe.DanhSachGen)
            {
                genTheoLop[gen.MaLopHocPhan] = gen;
                if (gen.MaGiangVien.HasValue && gen.MaGiangVien.Value > 0)
                {
                    int gvId = gen.MaGiangVien.Value;
                    if (!lopTheoGv.TryGetValue(gvId, out var dsLop))
                    {
                        dsLop = [];
                        lopTheoGv[gvId] = dsLop;
                    }
                    dsLop.Add(gen.MaLopHocPhan);
                }
            }

            // ========================================================
            // BƯỚC 1: SỬA CÁC LỚP BỊ VƯỚNG LỊCH BẬN CỐ ĐỊNH CỦA GIẢNG VIÊN
            // ========================================================
            if (lichBanTheoGv != null && lichBanTheoGv.Count > 0)
            {
                foreach (var gen in caThe.DanhSachGen)
                {
                    if (!gen.MaGiangVien.HasValue || gen.MaGiangVien.Value <= 0) continue;
                    int maLop = gen.MaLopHocPhan;
                    int maGv = gen.MaGiangVien.Value;

                    // Không sửa lớp đã khóa ghim
                    if (cacLopCoDinh != null && cacLopCoDinh.Contains(maLop)) continue;

                    if (!thoiGianTheoLop.TryGetValue(maLop, out var tgLop) || tgLop.Count == 0) continue;

                    // Kiểm tra xem maGv có bị vướng lịch bận vào giờ của lớp maLop không
                    bool vuongLichBan = false;
                    if (lichBanTheoGv.TryGetValue(maGv, out var dsBan))
                    {
                        vuongLichBan = tgLop.Any(tLop => dsBan.Any(tBan => tBan.TrungGioVoi(tLop)));
                    }

                    if (vuongLichBan && ungVienTheoLop.TryGetValue(maLop, out var dsUngVien))
                    {
                        // Tìm ứng viên thay thế không bị bận và không trùng lịch
                        var ungVienTot = TimUngVienHopLe(
                            maGv, tgLop, dsUngVien, lopTheoGv, thoiGianTheoLop, lichBanTheoGv);

                        if (ungVienTot.Count > 0)
                        {
                            int gvMoi = ungVienTot[_random.Next(ungVienTot.Count)];
                            gen.MaGiangVien = gvMoi;

                            // Cập nhật cấu trúc tra cứu
                            if (lopTheoGv.TryGetValue(maGv, out var dsCu)) dsCu.Remove(maLop);
                            if (!lopTheoGv.TryGetValue(gvMoi, out var dsMoi))
                            {
                                dsMoi = [];
                                lopTheoGv[gvMoi] = dsMoi;
                            }
                            dsMoi.Add(maLop);

                            coThayDoi = true;
                        }
                    }
                }
            }

            // ========================================================
            // BƯỚC 2: SỬA CÁC LỚP BỊ TRÙNG GIỜ CỦA CÙNG MỘT GIẢNG VIÊN
            // ========================================================
            foreach (var (gvId, dsLop) in lopTheoGv)
            {
                if (dsLop.Count <= 1) continue;

                for (int i = 0; i < dsLop.Count; i++)
                {
                    for (int j = i + 1; j < dsLop.Count; j++)
                    {
                        int lop1 = dsLop[i];
                        int lop2 = dsLop[j];

                        if (thoiGianTheoLop.TryGetValue(lop1, out var tg1) &&
                            thoiGianTheoLop.TryGetValue(lop2, out var tg2))
                        {
                            bool isConflict = tg1.Any(t1 => tg2.Any(t2 => t1.TrungGioVoi(t2)));
                            if (!isConflict) continue;

                            // Xác định thứ tự ưu tiên thử đổi (thử cả 2 lớp nếu cần)
                            var danhSachLopThuDoi = new List<int>();

                            bool lop1Khoa = cacLopCoDinh != null && cacLopCoDinh.Contains(lop1);
                            bool lop2Khoa = cacLopCoDinh != null && cacLopCoDinh.Contains(lop2);

                            if (lop1Khoa && lop2Khoa) continue; // Cả 2 đều bị khóa -> không sửa được

                            if (!lop2Khoa) danhSachLopThuDoi.Add(lop2);
                            if (!lop1Khoa) danhSachLopThuDoi.Add(lop1);

                            bool daSuaCapNay = false;
                            foreach (var lopDoi in danhSachLopThuDoi)
                            {
                                if (genTheoLop.TryGetValue(lopDoi, out var genDoi) &&
                                    ungVienTheoLop.TryGetValue(lopDoi, out var dsUngVien) &&
                                    thoiGianTheoLop.TryGetValue(lopDoi, out var tgLopDoi))
                                {
                                    var ungVienTot = TimUngVienHopLe(
                                        gvId, tgLopDoi, dsUngVien, lopTheoGv, thoiGianTheoLop, lichBanTheoGv);

                                    if (ungVienTot.Count > 0)
                                    {
                                        int gvMoi = ungVienTot[_random.Next(ungVienTot.Count)];
                                        genDoi.MaGiangVien = gvMoi;

                                        dsLop.Remove(lopDoi);
                                        if (!lopTheoGv.TryGetValue(gvMoi, out var dsMoi))
                                        {
                                            dsMoi = [];
                                            lopTheoGv[gvMoi] = dsMoi;
                                        }
                                        dsMoi.Add(lopDoi);

                                        coThayDoi = true;
                                        daSuaCapNay = true;
                                        break;
                                    }
                                }
                            }

                            // Bổ sung Toán tử Hoán đổi (Swap Repair): Khi chuyển 1 chiều thất bại,
                            // thử hoán đổi 2 lớp giữa 2 giảng viên phù hợp chuyên môn
                            if (!daSuaCapNay)
                            {
                                foreach (var lopDoi in danhSachLopThuDoi)
                                {
                                    if (ungVienTheoLop.TryGetValue(lopDoi, out var dsUngVien) &&
                                        thoiGianTheoLop.TryGetValue(lopDoi, out var tgLopDoi))
                                    {
                                        if (ThuHoanDoiHaiGiangVien(
                                            gvId, lopDoi, tgLopDoi, dsUngVien,
                                            genTheoLop, lopTheoGv, thoiGianTheoLop, ungVienTheoLop,
                                            cacLopCoDinh, lichBanTheoGv))
                                        {
                                            coThayDoi = true;
                                            daSuaCapNay = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (daSuaCapNay) break;
                        }
                    }
                    if (coThayDoi) break;
                }
                if (coThayDoi) break;
            }

            // Nếu không còn xung đột nào sửa được, dừng sớm
            if (!coThayDoi) break;
        }

        return caThe;
    }

    /// <summary>
    /// Tìm danh sách ứng viên chuyên môn mà không bị trùng lịch dạy và không vướng lịch bận cố định.
    /// </summary>
    private static List<int> TimUngVienHopLe(
        int gvHienTai,
        List<ThoiGianLop> tgLopCanDoi,
        List<int> dsUngVien,
        Dictionary<int, List<int>> lopTheoGv,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop,
        Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv)
    {
        var ungVienTot = new List<int>();

        foreach (var ungVienId in dsUngVien)
        {
            if (ungVienId == gvHienTai) continue;

            // 1. Kiểm tra lịch bận cố định của ungVienId
            if (lichBanTheoGv != null && lichBanTheoGv.TryGetValue(ungVienId, out var dsBanCuaUngVien))
            {
                if (tgLopCanDoi.Any(tLop => dsBanCuaUngVien.Any(tBan => tBan.TrungGioVoi(tLop))))
                {
                    continue; // Bị vướng lịch bận -> bỏ qua
                }
            }

            // 2. Kiểm tra trùng lịch với các lớp sẵn có của ungVienId
            bool biTrungLopSanCo = false;
            if (lopTheoGv.TryGetValue(ungVienId, out var cacLopCuaUngVien))
            {
                foreach (var maLopSanCo in cacLopCuaUngVien)
                {
                    if (thoiGianTheoLop.TryGetValue(maLopSanCo, out var tgSanCo))
                    {
                        if (tgLopCanDoi.Any(tDoi => tgSanCo.Any(tSc => tDoi.TrungGioVoi(tSc))))
                        {
                            biTrungLopSanCo = true;
                            break;
                        }
                    }
                }
            }

            if (!biTrungLopSanCo)
            {
                ungVienTot.Add(ungVienId);
            }
        }

        return ungVienTot;
    }

    /// <summary>
    /// Toán tử hoán đổi (Swap Repair): Khi giảng viên gvA bị kẹt lịch ở lớp lopA,
    /// tìm giảng viên gvB và lớp lopB của gvB sao cho:
    /// 1. gvB có chuyên môn dạy lopA và không bị trùng lịch/vướng bận khi nhận lopA (sau khi nhả lopB).
    /// 2. gvA có chuyên môn dạy lopB và không bị trùng lịch/vướng bận khi nhận lopB (sau khi nhả lopA).
    /// 3. Cả 2 lớp đều không bị khóa ghim.
    /// </summary>
    private bool ThuHoanDoiHaiGiangVien(
        int gvA,
        int lopA,
        List<ThoiGianLop> tgLopA,
        List<int> dsUngVienCuaLopA,
        Dictionary<int, Gen> genTheoLop,
        Dictionary<int, List<int>> lopTheoGv,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop,
        Dictionary<int, List<int>> ungVienTheoLop,
        HashSet<int>? cacLopCoDinh,
        Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv)
    {
        // Shuffle danh sách ứng viên của lopA để đảm bảo đa dạng nghiệm
        var danhSachUngVien = dsUngVienCuaLopA.Where(x => x != gvA).OrderBy(_ => _random.Next()).ToList();

        foreach (var gvB in danhSachUngVien)
        {
            if (!lopTheoGv.TryGetValue(gvB, out var dsLopCuaB) || dsLopCuaB.Count == 0) continue;

            var dsLopBThucTe = dsLopCuaB.OrderBy(_ => _random.Next()).ToList();

            foreach (var lopB in dsLopBThucTe)
            {
                // Không hoán đổi lớp đã bị khóa ghim
                if (cacLopCoDinh != null && cacLopCoDinh.Contains(lopB)) continue;

                // Điều kiện 1: gvA phải có chuyên môn dạy lopB
                if (!ungVienTheoLop.TryGetValue(lopB, out var dsUvCuaLopB) || !dsUvCuaLopB.Contains(gvA))
                {
                    continue;
                }

                if (!thoiGianTheoLop.TryGetValue(lopB, out var tgLopB)) continue;

                // Điều kiện 2: gvB nhận lopA (sau khi nhả lopB) có hợp lệ không?
                if (CoVuongLichBan(gvB, tgLopA, lichBanTheoGv)) continue;
                if (CoTrungLopConLai(gvB, tgLopA, lopB, lopTheoGv, thoiGianTheoLop)) continue;

                // Điều kiện 3: gvA nhận lopB (sau khi nhả lopA) có hợp lệ không?
                if (CoVuongLichBan(gvA, tgLopB, lichBanTheoGv)) continue;
                if (CoTrungLopConLai(gvA, tgLopB, lopA, lopTheoGv, thoiGianTheoLop)) continue;

                // THỎA MÃN TOÀN DIỆN! TIẾN HÀNH HOÁN ĐỔI 2 CHIỀU:
                if (genTheoLop.TryGetValue(lopA, out var genA) && genTheoLop.TryGetValue(lopB, out var genB))
                {
                    genA.MaGiangVien = gvB;
                    genB.MaGiangVien = gvA;

                    // Cập nhật cấu trúc tra cứu lopTheoGv
                    if (lopTheoGv.TryGetValue(gvA, out var dsA))
                    {
                        dsA.Remove(lopA);
                        dsA.Add(lopB);
                    }
                    if (lopTheoGv.TryGetValue(gvB, out var dsB))
                    {
                        dsB.Remove(lopB);
                        dsB.Add(lopA);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private static bool CoVuongLichBan(int gvId, List<ThoiGianLop> tgLop, Dictionary<int, List<ThoiGianBan>>? lichBanTheoGv)
    {
        if (lichBanTheoGv != null && lichBanTheoGv.TryGetValue(gvId, out var dsBan))
        {
            return tgLop.Any(tLop => dsBan.Any(tBan => tBan.TrungGioVoi(tLop)));
        }
        return false;
    }

    private static bool CoTrungLopConLai(
        int gvId,
        List<ThoiGianLop> tgLopMoi,
        int lopBoQua,
        Dictionary<int, List<int>> lopTheoGv,
        Dictionary<int, List<ThoiGianLop>> thoiGianTheoLop)
    {
        if (!lopTheoGv.TryGetValue(gvId, out var dsLop)) return false;

        foreach (var maLop in dsLop)
        {
            if (maLop == lopBoQua) continue;
            if (thoiGianTheoLop.TryGetValue(maLop, out var tgHienCo))
            {
                if (tgLopMoi.Any(tMoi => tgHienCo.Any(tHc => tMoi.TrungGioVoi(tHc))))
                {
                    return true;
                }
            }
        }
        return false;
    }
}


