using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services.RangBuoc;

public class KetQuaKiemTraRangBuoc
{
    public bool HopLe => DanhSachViPham.Count == 0;
    public int TongSoLop { get; set; }
    public int SoLopDaPhanCong { get; set; }
    public int SoLopChuaPhanCong { get; set; }
    public int SoViPhamChuyenMon { get; set; }
    public int SoViPhamTrungLich { get; set; }
    public int SoViPhamLichBan { get; set; }
    public List<string> DanhSachViPham { get; set; } = new();
}

public class BoKiemTraRangBuoc
{
    /// <summary>
    /// Kiểm tra 2 khoảng thời gian học có bị trùng lịch nhau không (cùng thứ, giao nhau về tuần và tiết).
    /// </summary>
    public bool CoTrungLich(
        int thu1, int tbd1, int tkt1, int tuTuan1, int denTuan1,
        int thu2, int tbd2, int tkt2, int tuTuan2, int denTuan2)
    {
        if (thu1 != thu2) return false;

        // Kiểm tra giao khoảng tuần
        bool giaoTuan = Math.Max(tuTuan1, tuTuan2) <= Math.Min(denTuan1, denTuan2);
        if (!giaoTuan) return false;

        // Kiểm tra giao khoảng tiết
        bool giaoTiet = Math.Max(tbd1, tbd2) <= Math.Min(tkt1, tkt2);
        return giaoTiet;
    }

    /// <summary>
    /// Kiểm tra giảng viên có đủ chuyên môn để dạy học phần này không.
    /// </summary>
    public bool CoDungChuyenMon(int maGiangVien, int maHocPhan, IEnumerable<NangLucGiangDay> dsNangLuc)
    {
        return dsNangLuc.Any(x => x.MaGiangVien == maGiangVien && x.MaHocPhan == maHocPhan);
    }

    /// <summary>
    /// Kiểm tra lớp học phần có bị rơi vào lịch bận của giảng viên hay không.
    /// </summary>
    public bool CoVuongLichBan(int maGiangVien, int thu, int tbd, int tkt, IEnumerable<LichBanGiangVien> dsLichBan)
    {
        return dsLichBan.Any(lb =>
            lb.MaGiangVien == maGiangVien &&
            lb.Thu == thu &&
            Math.Max(lb.TietBatDau, tbd) <= Math.Min(lb.TietKetThuc, tkt));
    }

    /// <summary>
    /// Kiểm tra tất cả các lớp học phần trong kỳ đã được phân công giảng viên hay chưa.
    /// </summary>
    public bool DaPhuKinLop(IEnumerable<int> dsMaLopHocPhan, IDictionary<int, int?> phanCong)
    {
        foreach (var maLop in dsMaLopHocPhan)
        {
            if (!phanCong.TryGetValue(maLop, out var maGv) || !maGv.HasValue || maGv.Value <= 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Kiểm tra toàn diện tất cả các ràng buộc cứng cho một phương án phân công giảng dạy.
    /// </summary>
    public KetQuaKiemTraRangBuoc KiemTraToanBoPhuongAn(
        IDictionary<int, int?> phanCong, // Key: MaLopHocPhan, Value: MaGiangVien
        List<LopHocPhan> dsLop,
        List<ChiTietThoiKhoaBieu> dsChiTiet,
        List<NangLucGiangDay> dsNangLuc,
        List<LichBanGiangVien> dsLichBan,
        List<GiangVien>? dsGiangVien = null)
    {
        var ketQua = new KetQuaKiemTraRangBuoc
        {
            TongSoLop = dsLop.Count
        };

        // 1. Kiểm tra phủ kín lớp
        foreach (var lop in dsLop)
        {
            if (!phanCong.TryGetValue(lop.MaLopHocPhan, out var maGv) || !maGv.HasValue || maGv.Value <= 0)
            {
                ketQua.SoLopChuaPhanCong++;
                ketQua.DanhSachViPham.Add($"Lop [{lop.MaLopHocPhanTruong} - {lop.TenLop}] chua duoc phan cong giang vien.");
            }
            else
            {
                ketQua.SoLopDaPhanCong++;
            }
        }

        // 2. Kiểm tra đúng chuyên môn
        foreach (var (maLop, maGvOpt) in phanCong)
        {
            if (!maGvOpt.HasValue || maGvOpt.Value <= 0) continue;
            int maGv = maGvOpt.Value;

            var lop = dsLop.FirstOrDefault(x => x.MaLopHocPhan == maLop);
            if (lop == null) continue;

            if (!CoDungChuyenMon(maGv, lop.MaHocPhan, dsNangLuc))
            {
                ketQua.SoViPhamChuyenMon++;
                var gv = dsGiangVien?.FirstOrDefault(x => x.MaGiangVien == maGv);
                string tenGv = gv != null ? $"{gv.HoTen} (ID: {gv.MaGiangVien})" : $"Giang vien [Ma: {maGv}]";
                ketQua.DanhSachViPham.Add($"{tenGv} khong co nang luc chuyen mon day lop [{lop.MaLopHocPhanTruong} - {lop.TenLop}].");
            }
        }

        // 3. Kiểm tra trùng lịch và lịch bận theo từng giảng viên
        var phanCongTheoGv = phanCong
            .Where(x => x.Value.HasValue && x.Value.Value > 0)
            .GroupBy(x => x.Value!.Value);

        foreach (var gvGroup in phanCongTheoGv)
        {
            int maGv = gvGroup.Key;
            var gv = dsGiangVien?.FirstOrDefault(x => x.MaGiangVien == maGv);
            string tenGv = gv != null ? $"{gv.HoTen} (ID: {gv.MaGiangVien})" : $"Giang vien [Ma: {maGv}]";

            var dsLopCuaGv = gvGroup.Select(x => x.Key).ToList();
            var chiTietCuaGv = dsChiTiet.Where(x => dsLopCuaGv.Contains(x.MaLopHocPhan)).ToList();

            // 3.1 Kiểm tra trùng lịch giữa các lớp KHÁC NHAU của cùng một giảng viên
            for (int i = 0; i < chiTietCuaGv.Count; i++)
            {
                for (int j = i + 1; j < chiTietCuaGv.Count; j++)
                {
                    var ct1 = chiTietCuaGv[i];
                    var ct2 = chiTietCuaGv[j];

                    // Chỉ xét trùng lịch nếu thuộc 2 lớp khác nhau
                    if (ct1.MaLopHocPhan != ct2.MaLopHocPhan && CoTrungLich(
                        ct1.Thu, ct1.TietBatDau, ct1.TietKetThuc, ct1.TuTuan, ct1.DenTuan,
                        ct2.Thu, ct2.TietBatDau, ct2.TietKetThuc, ct2.TuTuan, ct2.DenTuan))
                    {
                        var lop1 = dsLop.FirstOrDefault(x => x.MaLopHocPhan == ct1.MaLopHocPhan);
                        var lop2 = dsLop.FirstOrDefault(x => x.MaLopHocPhan == ct2.MaLopHocPhan);

                        // Miá»…n trá»« náº¿u lĂ  lá»›p ghĂ©p há»c chung cĂ¹ng mĂ´n vĂ  cĂ¹ng ca/phĂ²ng
                        bool laLopGhep = lop1 != null && lop2 != null && lop1.MaHocPhan == lop2.MaHocPhan &&
                            ct1.Thu == ct2.Thu && ct1.TietBatDau == ct2.TietBatDau && ct1.TietKetThuc == ct2.TietKetThuc &&
                            ct1.TuTuan == ct2.TuTuan && ct1.DenTuan == ct2.DenTuan &&
                            !string.IsNullOrWhiteSpace(ct1.PhongHoc) &&
                            string.Equals(ct1.PhongHoc?.Trim(), ct2.PhongHoc?.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (!laLopGhep)
                        {
                            ketQua.SoViPhamTrungLich++;
                            string maLop1 = lop1 != null ? lop1.MaLopHocPhanTruong : $"Lop #{ct1.MaLopHocPhan}";
                            string maLop2 = lop2 != null ? lop2.MaLopHocPhanTruong : $"Lop #{ct2.MaLopHocPhan}";
                            ketQua.DanhSachViPham.Add($"{tenGv} bi trung lich day giua lop [{maLop1}] va [{maLop2}] vao Thu {ct1.Thu}, tiet {Math.Max(ct1.TietBatDau, ct2.TietBatDau)}-{Math.Min(ct1.TietKetThuc, ct2.TietKetThuc)}.");
                        }
                    }
                }

                // 3.2 Kiểm tra vướng lịch bận cá nhân
                var ct = chiTietCuaGv[i];
                if (CoVuongLichBan(maGv, ct.Thu, ct.TietBatDau, ct.TietKetThuc, dsLichBan))
                {
                    ketQua.SoViPhamLichBan++;
                    var lop = dsLop.FirstOrDefault(x => x.MaLopHocPhan == ct.MaLopHocPhan);
                    string maLop = lop != null ? lop.MaLopHocPhanTruong : $"Lop #{ct.MaLopHocPhan}";

                    ketQua.DanhSachViPham.Add($"{tenGv} bi vuong lich ban ca nhan khi day lop [{maLop}] vao Thu {ct.Thu}, tiet {ct.TietBatDau}-{ct.TietKetThuc}.");
                }
            }
        }

        return ketQua;
    }
}
