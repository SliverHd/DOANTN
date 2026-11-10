namespace XepLichGiangDay.Api.DTOs.XepLich;

public class BangPhanCongHocKyDto
{
    public int MaHocKy { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public int TongSoLop { get; set; }
    public int SoLopDaPhanCong { get; set; }
    public int SoLopChuaPhanCong { get; set; }
    public int SoLopDaKhoa { get; set; }
    public List<DongPhanCongLopDto> DanhSachLop { get; set; } = [];
}

public class DongPhanCongLopDto
{
    public int MaLopHocPhan { get; set; }
    public string MaLopHocPhanTruong { get; set; } = string.Empty;
    public int MaHocPhan { get; set; }
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public string TenLop { get; set; } = string.Empty;
    public int SoLuongSinhVien { get; set; }

    // Chi tiết thời khóa biểu
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public int SoTiet => TietKetThuc >= TietBatDau ? (TietKetThuc - TietBatDau + 1) : 0;
    public string PhongHoc { get; set; } = string.Empty;
    public int TuTuan { get; set; }
    public int DenTuan { get; set; }
    public string ThoiGianHocTongHop { get; set; } = string.Empty;
    public List<ChiTietBuoiHocDto> DanhSachBuoiHoc { get; set; } = [];


    // Thông tin phân công hiện tại
    public int? MaPhanCong { get; set; }
    public int? MaGiangVien { get; set; }
    public string? TenGiangVien { get; set; }
    public string? EmailGiangVien { get; set; }
    public string NguonPhanCong { get; set; } = string.Empty; // "ThuCong", "GA", "CP_SAT", ""
    public string TrangThai { get; set; } = "ChuaPhanCong"; // "DuKien", "DaKhoa", "ChuaPhanCong"
    public bool DaKhoa => TrangThai == "DaKhoa";
    public double DiemPhat { get; set; }

    // Thông tin trạng thái tạm hoãn / tạm nghỉ tuần
    public bool CoLichTamNghi { get; set; }
    public string? TuanTamNghi { get; set; }
    public string? GhiChuTamNghi { get; set; }

    // Danh sách gợi ý giảng viên ứng viên cho lớp này
    public List<UngVienLopDto> DanhSachUngVien { get; set; } = [];
}

public class UngVienLopDto
{
    public int MaGiangVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public bool CoNangLuc { get; set; }
    public bool BiVuongLichBan { get; set; }
    public bool BiKhoaCungSeminar { get; set; }
    public bool BiTrungLichDay { get; set; }
    public bool HopLe => CoNangLuc && !BiVuongLichBan && !BiTrungLichDay;
    public string LyDoKhongHopLe { get; set; } = string.Empty;
}

public class YeuCauPhanCongThuCongDto
{
    public int MaLopHocPhan { get; set; }
    public int MaGiangVien { get; set; }
    public bool KhoaNgay { get; set; } = false;
    public bool BoQuaCanhBao { get; set; } = false;
}

public class KetQuaPhanCongThuCongDto
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = string.Empty;
    public List<string> DanhSachViPham { get; set; } = [];
    public bool CanXacNhan { get; set; }
}

public class ThongKeTaiGvDto
{
    public int MaGiangVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public int SoLopPhanCong { get; set; }
    public int TongSoTinChi { get; set; }
    public int TongSoTietTuan { get; set; }
    public int SoTietToiThieu { get; set; }
    public int SoTietToiDa { get; set; }
    public string TrangThaiTai { get; set; } = "BinhThuong"; // "ThieuTai", "BinhThuong", "QuaTai"
}

public class ChiTietBuoiHocDto
{
    public int Thu { get; set; }
    public int TietBatDau { get; set; }
    public int TietKetThuc { get; set; }
    public string PhongHoc { get; set; } = string.Empty;
    public int TuTuan { get; set; }
    public int DenTuan { get; set; }
}

