using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.NguoiDung;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/nguoi-dung")]
[Authorize(Roles = "Admin")]
public class NguoiDungController(NguoiDungService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NguoiDungDto>>> GetAll()
    {
        return Ok(await service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NguoiDungDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<NguoiDungDto>> Create(TaoNguoiDungDto dto)
    {
        var item = await service.CreateAsync(dto);
        if (item is null)
        {
            return BadRequest("Ten dang nhap da ton tai hoac thong tin khong hop le.");
        }
        return CreatedAtAction(nameof(GetById), new { id = item.MaNguoiDung }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CapNhatNguoiDungDto dto)
    {
        var success = await service.UpdateAsync(id, dto);
        return success ? NoContent() : BadRequest("Khong the cap nhat nguoi dung. Khong ton tai hoac khong duoc phep khoa/ha quyen tai khoan admin.");
    }

    [HttpPost("{id:int}/reset-mat-khau")]
    public async Task<IActionResult> ResetMatKhau(int id, ResetMatKhauDto dto)
    {
        var success = await service.ResetMatKhauAsync(id, dto.MatKhauMoi);
        return success ? NoContent() : BadRequest("Khong the dat lai mat khau.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await service.DeleteAsync(id);
        return success ? NoContent() : BadRequest("Khong the xoa nguoi dung. Khong ton tai hoac khong duoc phep xoa tai khoan admin goc.");
    }
}
