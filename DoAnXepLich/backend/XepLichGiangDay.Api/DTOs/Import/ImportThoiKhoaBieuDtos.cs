namespace XepLichGiangDay.Api.DTOs.Import;

public class DongThoiKhoaBieuImportDto
{
    public int SoThuTu { get; set; }
    public string MaLopHocPhanTruong { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string MaHocPhanTruong { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoLuongSinhVien { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string PhongHoc { get; set; } = string.Empty;
    public int TuTuan { get; set; }
    public int DenTuan { get; set; }
    public bool HopLe { get; set; } = true;
    public List<string> DanhSachLoi { get; set; } = new();
}

public class KetQuaPreviewImportDto
{
    public int TongSoDong { get; set; }
    public int SoDongHopLe { get; set; }
    public int SoDongLoi { get; set; }
    public List<DongThoiKhoaBieuImportDto> ChiTiet { get; set; } = new();
}

public class XacNhanImportRequest
{
    public int MaHocKy { get; set; }
    public int? MaBoMon { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public List<DongThoiKhoaBieuImportDto> DanhSachDong { get; set; } = new();
}

public class LichSuImportDto
{
    public int MaLichSu { get; set; }
    public int MaHocKy { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public DateTime NgayImport { get; set; }
    public int TongSoDong { get; set; }
    public int SoDongThanhCong { get; set; }
    public int SoDongLoi { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}
