using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/thoi-khoa-bieu")]
public class ThoiKhoaBieuController(ThoiKhoaBieuService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? maHocKy, [FromQuery] int? maBoMon)
    {
        var list = await service.LayDanhSachLopHocPhanAsync(maHocKy, maBoMon);
        return Ok(list);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, loi) = await service.XoaLopHocPhanAsync(id);
        if (!ok)
        {
            if (loi == "Lop hoc phan khong ton tai.") return NotFound(loi);
            return BadRequest(loi);
        }
        return NoContent();
    }
}
