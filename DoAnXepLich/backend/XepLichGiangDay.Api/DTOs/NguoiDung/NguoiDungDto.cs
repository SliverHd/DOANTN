namespace XepLichGiangDay.Api.DTOs.NguoiDung;

public class NguoiDungDto
{
    public int MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VaiTro { get; set; } = string.Empty;
    public int? MaGiangVien { get; set; }
    public string? TenGiangVien { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
