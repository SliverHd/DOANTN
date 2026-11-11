namespace XepLichGiangDay.Api.DTOs.NguyenVong;

public class LichBanDto
{
    public int MaLichBan { get; set; }
    public int MaGiangVien { get; set; }
    public string? TenGiangVien { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public int MaHocKy { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string? LyDo { get; set; }
    public bool LaSeminar { get; set; }
}

public class TaoLichSeminarRequest
{
    public int MaHocKy { get; set; }
    public string TenSeminar { get; set; } = string.Empty;
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public List<int> DanhSachMaGiangVien { get; set; } = [];
}

public class KetQuaTaoLichSeminarDto
{
    public bool ThanhCong { get; set; }
    public string ThongDiep { get; set; } = string.Empty;
    public int SoGiangVienDaKhoa { get; set; }
    public int SoGiangVienDaTonTai { get; set; }
    public int SoLopBiGo { get; set; }
    public List<string> DanhSachLopBiGo { get; set; } = [];
}

public class XoaNhomSeminarRequest
{
    public int MaHocKy { get; set; }
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string? LyDo { get; set; }
    public int? MaBoMon { get; set; }
}

public class TaoLichBanDto
{
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string? LyDo { get; set; }
}

public class NguyenVongDto
{
    public int MaNguyenVong { get; set; }
    public int MaGiangVien { get; set; }
    public string? TenGiangVien { get; set; }
    public int? MaBoMon { get; set; }
    public string? TenBoMon { get; set; }
    public int MaHocKy { get; set; }
    public string MaRangBuocMem { get; set; } = "SC01";
    public string TenRangBuocMem { get; set; } = string.Empty;
    public string LoaiNguyenVong { get; set; } = "TranhDay"; // ThichDay, TranhDay
    public int MucDo { get; set; } = 1;
    public bool KhoaCungLichBan { get; set; }
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public double TrongSoPhat { get; set; }
    public string? NoiDungGoc { get; set; }
    public string? LyDo { get; set; }
    public string TrangThai { get; set; } = "ChoDuyet"; // ChoDuyet, DaDuyet, TuChoi
    public DateTime? NgayTao { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public string? GhiChuDuyet { get; set; }
}

public class ThemNguyenVongDto
{
    public int MaHocKy { get; set; }
    public int MaGiangVien { get; set; }
    public List<int>? DanhSachMaGiangVien { get; set; }
    public string MaRangBuocMem { get; set; } = "SC01";
    public string? TenRangBuocMem { get; set; }
    public int? Thu { get; set; }
    public int? DenThu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public int? TuTuan { get; set; } = 1;
    public int? DenTuan { get; set; } = 20;
    public double TrongSoPhat { get; set; } = 10.0;
    public string? LyDo { get; set; }
    public bool KhoaCungLichBan { get; set; } = false;
}

public class TaoNguyenVongDto
{
    public string LoaiNguyenVong { get; set; } = "ThichDay";
    public int MucDo { get; set; } = 1;
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public string? GhiChu { get; set; }
}

public class LuuMaTranNguyenVongRequest
{
    public int MaGiangVien { get; set; }
    public int MaHocKy { get; set; }
    public List<TaoLichBanDto> DanhSachLichBan { get; set; } = [];
    public List<TaoNguyenVongDto> DanhSachNguyenVong { get; set; } = [];
}

public class XuLyNguyenVongRequest
{
    public string? GhiChu { get; set; }
}

public class KetQuaDuyetNguyenVongDto
{
    public bool ThanhCong { get; set; }
    public string ThongDiep { get; set; } = string.Empty;
    public int SoLopBiHuy { get; set; }
    public List<string> DanhSachLopBiHuy { get; set; } = [];
    public NguyenVongDto? NguyenVong { get; set; }
}
