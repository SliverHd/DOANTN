namespace XepLichGiangDay.Api.DTOs.NguoiDung;

public class CapNhatNguoiDungDto
{
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VaiTro { get; set; } = "GiangVien";
    public int? MaGiangVien { get; set; }
    public int? MaBoMon { get; set; }
    public string TrangThai { get; set; } = "KichHoat"; // KichHoat, Khoa
}
