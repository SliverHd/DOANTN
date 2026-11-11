using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.Auth;
using XepLichGiangDay.Api.Models.Entities;
using XepLichGiangDay.Api.Security;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Tests;

public class AuthAndSecurityTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuthTestDb_{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void BamMatKhau_PBKDF2_SinhMuoiNgauNhienVaXacThucChinhXac()
    {
        var hash1 = AuthService.BamMatKhau("MatKhauManh@2026");
        var hash2 = AuthService.BamMatKhau("MatKhauManh@2026");

        // Hai lan bam cung mat khau phai khac nhau do co Random Salt
        Assert.NotEqual(hash1, hash2);
        Assert.True(hash1.Length > 64);

        var user = new NguoiDung { MatKhau = hash1 };
        Assert.True(AuthService.XacThucMatKhau(user, "MatKhauManh@2026", out var canCapNhat));
        Assert.False(canCapNhat);

        Assert.False(AuthService.XacThucMatKhau(user, "SaiMatKhau", out _));
    }

    [Fact]
    public async Task DangNhapAsync_TuDongNangCapHashSha256CuLenPBKDF2()
    {
        await using var context = CreateInMemoryContext();
        var legacySha256 = AuthService.BamMatKhauSha256Cu("gv123456");
        Assert.Equal(64, legacySha256.Length);

        context.NguoiDung.Add(new NguoiDung
        {
            TenDangNhap = "gv_legacy",
            MatKhau = legacySha256,
            HoTen = "Giang vien Legacy",
            VaiTro = "GiangVien",
            TrangThai = "KichHoat"
        });
        await context.SaveChangesAsync();

        var jwtService = new JwtTokenService();
        var authService = new AuthService(context, jwtService);

        var loginResult = await authService.DangNhapAsync(new DangNhapDto
        {
            TenDangNhap = "gv_legacy",
            MatKhau = "gv123456"
        });

        Assert.NotNull(loginResult);
        Assert.NotEmpty(loginResult.Token);

        // Kiem tra DB da tu dong nang cap hash tu SHA-256 (64 ky tu hex) len PBKDF2
        var updatedUser = await context.NguoiDung.SingleAsync(x => x.TenDangNhap == "gv_legacy");
        Assert.NotEqual(legacySha256, updatedUser.MatKhau);
        Assert.True(updatedUser.MatKhau.Length > 64);
        Assert.True(AuthService.XacThucMatKhau(updatedUser, "gv123456", out var canCapNhatSauNangCap));
        Assert.False(canCapNhatSauNangCap);
    }

    [Fact]
    public void JwtTokenService_TaoVaXacThucToken_HopLeVaChongGiaMao()
    {
        var jwtService = new JwtTokenService();
        var user = new NguoiDung
        {
            MaNguoiDung = 42,
            TenDangNhap = "tbm_cntt",
            VaiTro = "TruongBoMon",
            MaGiangVien = 15
        };

        var token = jwtService.TaoToken(user, maBoMon: 3);
        Assert.Equal(3, token.Split('.').Length);

        var principal = jwtService.XacThucToken(token);
        Assert.NotNull(principal);
        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("tbm_cntt", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("TruongBoMon", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("15", principal.FindFirstValue("ma_giang_vien"));
        Assert.Equal("3", principal.FindFirstValue("ma_bo_mon"));

        // Gia mao chu ky hoac payload phai bi tu choi
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.{parts[1]}.chukygia123456";
        Assert.Null(jwtService.XacThucToken(tamperedToken));

        // Token het han phai bi tu choi
        var expiredToken = jwtService.TaoToken(user, maBoMon: 3, customLifetime: TimeSpan.FromMinutes(-5));
        Assert.Null(jwtService.XacThucToken(expiredToken));
    }
}
