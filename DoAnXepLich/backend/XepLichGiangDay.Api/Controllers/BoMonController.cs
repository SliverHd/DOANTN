using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.BoMon;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/bo-mon")]
[Authorize]
public class BoMonController(BoMonService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await service.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(TaoBoMonDto dto)
    {
        var (success, error, created) = await service.CreateAsync(dto);
        if (!success)
        {
            return BadRequest(error);
        }
        return CreatedAtAction(nameof(GetById), new { id = created!.MaBoMon }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaoBoMonDto dto)
    {
        var (success, error) = await service.UpdateAsync(id, dto);
        if (!success)
        {
            if (error == "Khong tim thay bo mon.") return NotFound(error);
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
            if (error == "Khong tim thay bo mon.") return NotFound(error);
            return BadRequest(error);
        }
        return NoContent();
    }
}
