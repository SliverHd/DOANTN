namespace XepLichGiangDay.Api.DTOs.HocPhan;

public class TaoHocPhanDto
{
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public int? MaBoMon { get; set; }
    public string TrangThai { get; set; } = "DangApDung";
}
