namespace XepLichGiangDay.Api.DTOs.NguoiDung;

public class TaoNguoiDungDto
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VaiTro { get; set; } = "GiangVien"; // Admin, TruongBoMon, GiangVien
    public int? MaGiangVien { get; set; }
    public int? MaBoMon { get; set; }
}
