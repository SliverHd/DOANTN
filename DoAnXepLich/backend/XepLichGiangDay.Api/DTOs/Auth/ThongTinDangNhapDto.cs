namespace XepLichGiangDay.Api.DTOs.Auth;

public class ThongTinDangNhapDto
{
    public int MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VaiTro { get; set; } = string.Empty; // Admin, TruongBoMon, GiangVien
    public int? MaGiangVien { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public string Token { get; set; } = string.Empty;
}
