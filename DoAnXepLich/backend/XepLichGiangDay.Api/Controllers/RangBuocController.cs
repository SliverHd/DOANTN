using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.RangBuoc;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/rang-buoc")]
[Authorize]
public class RangBuocController(RangBuocService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int maHocKy)
    {
        try
        {
            var list = await service.LayDanhSachRangBuocAsync(maHocKy);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> CapNhatTrongSo(int id, [FromBody] CapNhatRangBuocDto dto)
    {
        try
        {
            var ok = await service.CapNhatTrongSoAsync(id, dto.TrongSoPhat);
            if (!ok) return NotFound("Rang buoc khong ton tai.");
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("dinh-muc")]
    public async Task<IActionResult> GetDinhMuc([FromQuery] int maHocKy)
    {
        try
        {
            var list = await service.LayDanhSachDinhMucAsync(maHocKy);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPost("dinh-muc")]
    public async Task<IActionResult> CapNhatDinhMuc([FromBody] CapNhatDinhMucDto dto)
    {
        try
        {
            var ok = await service.CapNhatDinhMucAsync(dto);
            return Ok(new { ThanhCong = ok });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPost("dinh-muc/ap-dung-chung")]
    public async Task<IActionResult> ApDungChung([FromBody] ApDungDinhMucChungDto dto)
    {
        try
        {
            var ok = await service.ApDungDinhMucChungAsync(dto);
            return Ok(new { ThanhCong = ok });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("kiem-tra-cung/{maHocKy:int}")]
    public async Task<IActionResult> KiemTraRangBuocCung(int maHocKy)
    {
        try
        {
            var ketQua = await service.KiemTraRangBuocCungHocKyAsync(maHocKy);
            return Ok(ketQua);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ung-vien/{maHocKy:int}")]
    public async Task<IActionResult> LayDanhSachUngVien(int maHocKy)
    {
        try
        {
            var list = await service.LayDanhSachUngVienChiTietAsync(maHocKy);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("danh-gia-hien-tai/{maHocKy:int}")]
    public async Task<IActionResult> DanhGiaPhuongAnHienTai(int maHocKy)
    {
        try
        {
            var ketQua = await service.DanhGiaPhuongAnHienTaiAsync(maHocKy);
            return Ok(ketQua);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
