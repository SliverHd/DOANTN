namespace XepLichGiangDay.Api.DTOs.HocPhan;

public class HocPhanDto
{
    public int MaHocPhan { get; set; }
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
