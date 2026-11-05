using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.GiangVien;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class GiangVienService(AppDbContext context)
{
    public async Task<List<GiangVienDto>> GetAllAsync(int? maBoMon = null)
    {
        var query = context.GiangVien.AsQueryable();
        if (maBoMon.HasValue && maBoMon.Value > 0)
        {
            query = query.Where(x => x.MaBoMon == maBoMon.Value);
        }
        var dsGiangVien = await query.ToListAsync();
        var dsBoMon = await context.BoMon.ToDictionaryAsync(b => b.MaBoMon, b => b.TenBoMon);
        
        var dsNangLuc = await (from nl in context.NangLucGiangDay
                               join hp in context.HocPhan on nl.MaHocPhan equals hp.MaHocPhan
                               select new
                               {
                                   nl.MaNangLuc,
                                   nl.MaGiangVien,
                                   nl.MaHocPhan,
                                   hp.TenHocPhan,
                                   hp.SoTinChi,
                                   nl.MucDoChuyenMon,
                                   nl.TrangThai
                               }).ToListAsync();

        var nangLucTheoGv = dsNangLuc
            .GroupBy(x => x.MaGiangVien)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new NangLucGiangDayDto
                {
                    MaNangLuc = x.MaNangLuc,
                    MaGiangVien = x.MaGiangVien,
                    MaHocPhan = x.MaHocPhan,
                    TenHocPhan = x.TenHocPhan,
                    SoTinChi = x.SoTinChi,
                    MucDoChuyenMon = x.MucDoChuyenMon,
                    TrangThai = x.TrangThai
                }).ToList()
            );

        return dsGiangVien.Select(x => new GiangVienDto
        {
            MaGiangVien = x.MaGiangVien,
            HoTen = x.HoTen,
            Email = x.Email,
            SoDienThoai = x.SoDienThoai,
            ChucDanh = x.ChucDanh,
            MaBoMon = x.MaBoMon,
            TenBoMon = x.MaBoMon.HasValue && dsBoMon.TryGetValue(x.MaBoMon.Value, out var tenBm) ? tenBm : null,
            TrangThai = x.TrangThai,
            DanhSachNangLuc = nangLucTheoGv.GetValueOrDefault(x.MaGiangVien, [])
        }).ToList();
    }

    public async Task<GiangVienDto?> GetByIdAsync(int id)
    {
        var item = await context.GiangVien.FindAsync(id);
        if (item is null) return null;

        string? tenBoMon = null;
        if (item.MaBoMon.HasValue)
        {
            var bm = await context.BoMon.FindAsync(item.MaBoMon.Value);
            tenBoMon = bm?.TenBoMon;
        }

        var dsNangLuc = await (from nl in context.NangLucGiangDay
                               where nl.MaGiangVien == id
                               join hp in context.HocPhan on nl.MaHocPhan equals hp.MaHocPhan
                               select new NangLucGiangDayDto
                               {
                                   MaNangLuc = nl.MaNangLuc,
                                   MaGiangVien = nl.MaGiangVien,
                                   MaHocPhan = nl.MaHocPhan,
                                   TenHocPhan = hp.TenHocPhan,
                                   SoTinChi = hp.SoTinChi,
                                   MucDoChuyenMon = nl.MucDoChuyenMon,
                                   TrangThai = nl.TrangThai
                               }).ToListAsync();

        return new GiangVienDto
        {
            MaGiangVien = item.MaGiangVien,
            HoTen = item.HoTen,
            Email = item.Email,
            SoDienThoai = item.SoDienThoai,
            ChucDanh = item.ChucDanh,
            MaBoMon = item.MaBoMon,
            TenBoMon = tenBoMon,
            TrangThai = item.TrangThai,
            DanhSachNangLuc = dsNangLuc
        };
    }

    public async Task<GiangVienDto> CreateAsync(TaoGiangVienDto dto)
    {
        var item = new GiangVien
        {
            HoTen = dto.HoTen,
            Email = dto.Email,
            SoDienThoai = dto.SoDienThoai,
            ChucDanh = dto.ChucDanh,
            MaBoMon = dto.MaBoMon,
            TrangThai = dto.TrangThai
        };

        context.GiangVien.Add(item);
        await context.SaveChangesAsync();

        if (dto.DanhSachMaHocPhan is { Count: > 0 })
        {
            foreach (var maHp in dto.DanhSachMaHocPhan.Distinct())
            {
                context.NangLucGiangDay.Add(new NangLucGiangDay
                {
                    MaGiangVien = item.MaGiangVien,
                    MaHocPhan = maHp,
                    MucDoChuyenMon = 1,
                    TrangThai = "DangApDung"
                });
            }
            await context.SaveChangesAsync();
        }

        return (await GetByIdAsync(item.MaGiangVien))!;
    }

    public async Task<bool> UpdateAsync(int id, TaoGiangVienDto dto)
    {
        var item = await context.GiangVien.FindAsync(id);
        if (item is null) return false;

        item.HoTen = dto.HoTen;
        item.Email = dto.Email;
        item.SoDienThoai = dto.SoDienThoai;
        item.ChucDanh = dto.ChucDanh;
        item.MaBoMon = dto.MaBoMon;
        item.TrangThai = dto.TrangThai;

        if (dto.DanhSachMaHocPhan != null)
        {
            var oldNl = await context.NangLucGiangDay.Where(x => x.MaGiangVien == id).ToListAsync();
            context.NangLucGiangDay.RemoveRange(oldNl);

            foreach (var maHp in dto.DanhSachMaHocPhan.Distinct())
            {
                context.NangLucGiangDay.Add(new NangLucGiangDay
                {
                    MaGiangVien = id,
                    MaHocPhan = maHp,
                    MucDoChuyenMon = 1,
                    TrangThai = "DangApDung"
                });
            }
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CapNhatNangLucAsync(int maGiangVien, List<int> danhSachMaHocPhan)
    {
        var gvTonTai = await context.GiangVien.AnyAsync(x => x.MaGiangVien == maGiangVien);
        if (!gvTonTai) return false;

        var oldNl = await context.NangLucGiangDay.Where(x => x.MaGiangVien == maGiangVien).ToListAsync();
        context.NangLucGiangDay.RemoveRange(oldNl);

        foreach (var maHp in danhSachMaHocPhan.Distinct())
        {
            context.NangLucGiangDay.Add(new NangLucGiangDay
            {
                MaGiangVien = maGiangVien,
                MaHocPhan = maHp,
                MucDoChuyenMon = 1,
                TrangThai = "DangApDung"
            });
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<NangLucGiangDayDto>> LayNangLucAsync(int maGiangVien)
    {
        return await (from nl in context.NangLucGiangDay
                      where nl.MaGiangVien == maGiangVien
                      join hp in context.HocPhan on nl.MaHocPhan equals hp.MaHocPhan
                      select new NangLucGiangDayDto
                      {
                          MaNangLuc = nl.MaNangLuc,
                          MaGiangVien = nl.MaGiangVien,
                          MaHocPhan = nl.MaHocPhan,
                          TenHocPhan = hp.TenHocPhan,
                          SoTinChi = hp.SoTinChi,
                          MucDoChuyenMon = nl.MucDoChuyenMon,
                          TrangThai = nl.TrangThai
                      }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var item = await context.GiangVien.FindAsync(id);
        if (item is null) return (false, "Không tìm thấy giảng viên.");

        var daPhanCong = await context.PhanCongGiangDay.AnyAsync(x => x.MaGiangVien == id);
        if (daPhanCong)
        {
            return (false, "Không thể xóa giảng viên đã có lịch phân công giảng dạy trong hệ thống.");
        }

        var coKetQua = await context.KetQuaXepLich.AnyAsync(x => x.MaGiangVien == id);
        if (coKetQua)
        {
            return (false, "Không thể xóa giảng viên đã có dữ liệu xếp lịch.");
        }

        context.GiangVien.Remove(item);
        await context.SaveChangesAsync();
        return (true, null);
    }

}
