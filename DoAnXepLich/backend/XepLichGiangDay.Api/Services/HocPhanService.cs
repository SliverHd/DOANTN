using Microsoft.EntityFrameworkCore;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.DTOs.HocPhan;
using XepLichGiangDay.Api.Models.Entities;

namespace XepLichGiangDay.Api.Services;

public class HocPhanService(AppDbContext context)
{
    public async Task<List<HocPhanDto>> GetAllAsync()
    {
        return await (from hp in context.HocPhan
                      join bm in context.BoMon on hp.MaBoMon equals bm.MaBoMon into bmGroup
                      from bm in bmGroup.DefaultIfEmpty()
                      select new HocPhanDto
                      {
                          MaHocPhan = hp.MaHocPhan,
                          TenHocPhan = hp.TenHocPhan,
                          SoTinChi = hp.SoTinChi,
                          MaBoMon = hp.MaBoMon,
                          TenBoMon = bm != null ? bm.TenBoMon : null,
                          TrangThai = hp.TrangThai
                      }).ToListAsync();
    }

    public async Task<HocPhanDto?> GetByIdAsync(int id)
    {
        var item = await (from hp in context.HocPhan
                          where hp.MaHocPhan == id
                          join bm in context.BoMon on hp.MaBoMon equals bm.MaBoMon into bmGroup
                          from bm in bmGroup.DefaultIfEmpty()
                          select new HocPhanDto
                          {
                              MaHocPhan = hp.MaHocPhan,
                              TenHocPhan = hp.TenHocPhan,
                              SoTinChi = hp.SoTinChi,
                              MaBoMon = hp.MaBoMon,
                              TenBoMon = bm != null ? bm.TenBoMon : null,
                              TrangThai = hp.TrangThai
                          }).FirstOrDefaultAsync();
        return item;
    }

    public async Task<HocPhanDto> CreateAsync(TaoHocPhanDto dto)
    {
        var item = new HocPhan
        {
            TenHocPhan = dto.TenHocPhan,
            SoTinChi = dto.SoTinChi,
            MaBoMon = dto.MaBoMon,
            TrangThai = dto.TrangThai
        };

        context.HocPhan.Add(item);
        await context.SaveChangesAsync();

        string? tenBoMon = null;
        if (item.MaBoMon.HasValue)
        {
            var bm = await context.BoMon.FindAsync(item.MaBoMon.Value);
            tenBoMon = bm?.TenBoMon;
        }

        return new HocPhanDto
        {
            MaHocPhan = item.MaHocPhan,
            TenHocPhan = item.TenHocPhan,
            SoTinChi = item.SoTinChi,
            MaBoMon = item.MaBoMon,
            TenBoMon = tenBoMon,
            TrangThai = item.TrangThai
        };
    }

    public async Task<bool> UpdateAsync(int id, TaoHocPhanDto dto)
    {
        var item = await context.HocPhan.FindAsync(id);
        if (item is null) return false;

        item.TenHocPhan = dto.TenHocPhan;
        item.SoTinChi = dto.SoTinChi;
        item.MaBoMon = dto.MaBoMon;
        item.TrangThai = dto.TrangThai;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var item = await context.HocPhan.FindAsync(id);
        if (item is null) return (false, "Không tìm thấy học phần.");

        var coLop = await context.LopHocPhan.AnyAsync(x => x.MaHocPhan == id);
        if (coLop)
        {
            return (false, "Không thể xóa học phần đã có lớp học phần mở trong hệ thống.");
        }

        context.HocPhan.Remove(item);
        await context.SaveChangesAsync();
        return (true, null);
    }
}
