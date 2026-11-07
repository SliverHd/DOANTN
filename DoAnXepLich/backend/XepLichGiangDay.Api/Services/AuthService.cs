using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.Auth;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class AuthService(AppDbContext context)
{
    public async Task<ThongTinDangNhapDto?> DangNhapAsync(DangNhapDto dto)
    {
        await KhoiTaoAdminMacDinhNeuChuaCo();

        var hash = BamMatKhau(dto.MatKhau);
        var user = await context.NguoiDung
            .FirstOrDefaultAsync(x => x.TenDangNhap == dto.TenDangNhap && x.MatKhau == hash);

        if (user is null || user.TrangThai == "Khoa")
        {
            return null;
        }

        int? maBoMon = null;
        if (user.MaGiangVien.HasValue)
        {
            var gv = await context.GiangVien.FindAsync(user.MaGiangVien.Value);
            maBoMon = gv?.MaBoMon;
        }

        return new ThongTinDangNhapDto
        {
            MaNguoiDung = user.MaNguoiDung,
            TenDangNhap = user.TenDangNhap,
            HoTen = user.HoTen,
            Email = user.Email,
            VaiTro = user.VaiTro,
            MaGiangVien = user.MaGiangVien,
            MaBoMon = maBoMon,
            Token = Guid.NewGuid().ToString("N")
        };
    }

    public async Task<bool> DoiMatKhauAsync(DoiMatKhauDto dto)
    {
        if (dto.MaNguoiDung <= 0 || string.IsNullOrWhiteSpace(dto.MatKhauMoi)) return false;

        var user = await context.NguoiDung.FindAsync(dto.MaNguoiDung);
        if (user is null) return false;

        var hashCu = BamMatKhau(dto.MatKhauCu);
        if (user.MatKhau != hashCu) return false;

        user.MatKhau = BamMatKhau(dto.MatKhauMoi.Trim());
        await context.SaveChangesAsync();
        return true;
    }

    public static string BamMatKhau(string matKhau)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(matKhau));
        return Convert.ToHexString(bytes);
    }

    public async Task KhoiTaoAdminMacDinhNeuChuaCo()
    {
        if (!await context.NguoiDung.AnyAsync())
        {
            context.NguoiDung.Add(new NguoiDung
            {
                TenDangNhap = "admin",
                MatKhau = BamMatKhau("admin123"),
                HoTen = "Quan tri vien he thong",
                Email = "admin@truong.edu.vn",
                VaiTro = "Admin",
                TrangThai = "KichHoat"
            });
            await context.SaveChangesAsync();
        }
    }
}
