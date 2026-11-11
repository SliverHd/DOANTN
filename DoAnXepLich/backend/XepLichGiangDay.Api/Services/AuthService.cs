using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.Auth;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Security;

namespace XepLichGiangDay.Api.Services;

public class AuthService(AppDbContext context, JwtTokenService? jwtTokenService = null)
{
    private static readonly PasswordHasher<NguoiDung> PasswordHasher = new();
    private readonly JwtTokenService _jwtTokenService = jwtTokenService ?? new JwtTokenService();

    public async Task<ThongTinDangNhapDto?> DangNhapAsync(DangNhapDto dto)
    {
        await KhoiTaoAdminMacDinhNeuChuaCo();

        var user = await context.NguoiDung
            .FirstOrDefaultAsync(x => x.TenDangNhap == dto.TenDangNhap);

        if (user is null || user.TrangThai == "Khoa")
        {
            return null;
        }

<<<<<<< HEAD
        if (!XacThucMatKhau(user, dto.MatKhau, out var canCapNhatHash))
        {
            return null;
        }

        if (canCapNhatHash)
        {
            user.MatKhau = BamMatKhau(dto.MatKhau);
            await context.SaveChangesAsync();
        }

        int? maBoMon = user.MaBoMon;
        if (!maBoMon.HasValue && user.MaGiangVien.HasValue)
        {
            var gv = await context.GiangVien.FindAsync(user.MaGiangVien.Value);
            maBoMon = gv?.MaBoMon;
        }

        string? tenBoMon = null;
        if (maBoMon.HasValue)
        {
            var bm = await context.BoMon.FindAsync(maBoMon.Value);
            tenBoMon = bm?.TenBoMon;
        }

        var token = _jwtTokenService.TaoToken(user, maBoMon);

        return new ThongTinDangNhapDto
        {
            MaNguoiDung = user.MaNguoiDung,
            TenDangNhap = user.TenDangNhap,
            HoTen = user.HoTen,
            Email = user.Email,
            VaiTro = user.VaiTro,
            MaGiangVien = user.MaGiangVien,
            MaBoMon = maBoMon,
            TenBoMon = tenBoMon,
            Token = token
        };
    }

    public async Task<bool> DoiMatKhauAsync(DoiMatKhauDto dto)
    {
        if (dto.MaNguoiDung <= 0 || string.IsNullOrWhiteSpace(dto.MatKhauMoi)) return false;

        var user = await context.NguoiDung.FindAsync(dto.MaNguoiDung);
        if (user is null) return false;

        if (!XacThucMatKhau(user, dto.MatKhauCu, out _)) return false;

        user.MatKhau = BamMatKhau(dto.MatKhauMoi.Trim());
        await context.SaveChangesAsync();
        return true;
    }

    public static string BamMatKhau(string matKhau)
    {
        return PasswordHasher.HashPassword(new NguoiDung(), matKhau);
    }

    public static bool XacThucMatKhau(NguoiDung user, string matKhau, out bool canCapNhatHash)
    {
        canCapNhatHash = false;
        if (string.IsNullOrEmpty(user.MatKhau) || matKhau is null)
        {
            return false;
        }

        // Tuong thich nguoc voi hash SHA-256 cu (64 ky tu hex) va tu dong nang cap len PBKDF2
        if (LaHashSha256Cu(user.MatKhau))
        {
            var expectedHex = BamMatKhauSha256Cu(matKhau);
            var matched = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.MatKhau.ToUpperInvariant()),
                Encoding.UTF8.GetBytes(expectedHex));
            if (matched)
            {
                canCapNhatHash = true;
            }
            return matched;
        }

        try
        {
            var result = PasswordHasher.VerifyHashedPassword(user, user.MatKhau, matKhau);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                canCapNhatHash = true;
                return true;
            }
            return result == PasswordVerificationResult.Success;
        }
        catch
        {
            return false;
        }
    }

    public static string BamMatKhauSha256Cu(string matKhau)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(matKhau));
        return Convert.ToHexString(bytes);
    }

    private static bool LaHashSha256Cu(string hash)
    {
        if (hash.Length != 64) return false;
        for (int i = 0; i < hash.Length; i++)
        {
            var c = hash[i];
            var isHex = (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F');
            if (!isHex) return false;
        }
        return true;
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
