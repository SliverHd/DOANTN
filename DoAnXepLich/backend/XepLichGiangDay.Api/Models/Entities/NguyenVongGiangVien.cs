namespace XepLichGiangDay.Api.Models.Entities;

public class NguyenVongGiangVien
{
    public int MaNguyenVong { get; set; }
    public int MaGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public string LoaiNguyenVong { get; set; } = string.Empty;
    public int MucDo { get; set; }
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public double TrongSoPhat { get; set; }
    public string? NoiDungGoc { get; set; }
    public string TrangThai { get; set; } = "ChoDuyet"; // ChoDuyet, DaDuyet, TuChoi
    public DateTime? NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayDuyet { get; set; }
    public string? GhiChuDuyet { get; set; }
}
