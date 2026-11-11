using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.HocKy;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/hoc-ky")]
[Authorize]
public class HocKyController(HocKyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<HocKyDto>>> GetAll()
    {
        return Ok(await service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HocKyDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(TaoHocKyDto dto)
    {
        var (success, error, created) = await service.CreateAsync(dto);
        if (!success)
        {
            return BadRequest(error);
        }
        return CreatedAtAction(nameof(GetById), new { id = created!.MaHocKy }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaoHocKyDto dto)
    {
        var (success, error) = await service.UpdateAsync(id, dto);
        if (!success)
        {
            if (error == "Khong tim thay hoc ky.") return NotFound(error);
            return BadRequest(error);
        }
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await service.DeleteAsync(id);
        if (!success)
        {
            if (error == "Khong tim thay hoc ky.") return NotFound(error);
            return BadRequest(error);
        }
        return NoContent();
    }
}
