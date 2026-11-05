using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.GiangVien;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/giang-vien")]
public class GiangVienController(GiangVienService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GiangVienDto>>> GetAll([FromQuery] int? maBoMon)
    {
        return Ok(await service.GetAllAsync(maBoMon));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GiangVienDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<GiangVienDto>> Create(TaoGiangVienDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.MaGiangVien }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaoGiangVienDto dto)
    {
        var success = await service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await service.DeleteAsync(id);
        if (!success)
        {
            return BadRequest(new { thongBao = error });
        }
        return NoContent();
    }


    [HttpGet("{id:int}/nang-luc")]
    public async Task<ActionResult<List<NangLucGiangDayDto>>> GetNangLuc(int id)
    {
        return Ok(await service.LayNangLucAsync(id));
    }

    [HttpPut("{id:int}/nang-luc")]
    public async Task<IActionResult> CapNhatNangLuc(int id, [FromBody] CapNhatNangLucDto dto)
    {
        var success = await service.CapNhatNangLucAsync(id, dto.DanhSachMaHocPhan);
        return success ? NoContent() : NotFound();
    }
}
