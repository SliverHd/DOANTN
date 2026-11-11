using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.HocPhan;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/hoc-phan")]
[Authorize]
public class HocPhanController(HocPhanService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<HocPhanDto>>> GetAll([FromQuery] int? maBoMon, [FromQuery] int? maHocKy)
    {
        return Ok(await service.GetAllAsync(maBoMon, maHocKy));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HocPhanDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPost]
    public async Task<ActionResult<HocPhanDto>> Create(TaoHocPhanDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.MaHocPhan }, created);
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaoHocPhanDto dto)
    {
        var success = await service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
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
}
