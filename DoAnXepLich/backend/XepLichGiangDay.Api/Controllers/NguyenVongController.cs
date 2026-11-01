using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/nguyen-vong")]
public class NguyenVongController(NguyenVongService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NguyenVongDto>>> GetNguyenVong([FromQuery] int maHocKy, [FromQuery] int? maGiangVien)
    {
        var result = await service.LayNguyenVongAsync(maHocKy, maGiangVien);
        return Ok(result);
    }

    [HttpGet("lich-ban")]
    public async Task<ActionResult<List<LichBanDto>>> GetLichBan([FromQuery] int maHocKy, [FromQuery] int? maGiangVien)
    {
        var result = await service.LayLichBanAsync(maHocKy, maGiangVien);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<NguyenVongDto>> ThemNguyenVong([FromBody] ThemNguyenVongDto dto)
    {
        if (dto.MaHocKy <= 0 || dto.MaGiangVien <= 0)
        {
            return BadRequest("Mã học kỳ và mã giảng viên không hợp lệ.");
        }

        var result = await service.ThemNguyenVongAsync(dto);
        if (result is null)
        {
            return BadRequest("Không thể thêm nguyện vọng.");
        }
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> XoaNguyenVong(int id)
    {
        var success = await service.XoaNguyenVongAsync(id);
        if (!success)
        {
            return NotFound("Không tìm thấy nguyện vọng cần xóa.");
        }
        return Ok(new { Message = "Đã xóa nguyện vọng thành công." });
    }

    [HttpPost("luu-ma-tran")]
    public async Task<IActionResult> LuuMaTran([FromBody] LuuMaTranNguyenVongRequest request)
    {
        if (request.MaGiangVien <= 0 || request.MaHocKy <= 0)
        {
            return BadRequest("Mã giảng viên hoặc mã học kỳ không hợp lệ.");
        }

        var success = await service.LuuMaTranAsync(request);
        return success ? Ok(new { Message = "Lưu lịch bận và nguyện vọng thành công." }) : BadRequest("Không thể lưu thông tin.");
    }

    /// <summary>
    /// Trưởng bộ môn DUYỆT yêu cầu xin nghỉ / lịch bận của giảng viên:
    /// Tự động kick giáo viên ra khỏi các lớp học phần trùng ca nghỉ
    /// </summary>
    [HttpPost("{id:int}/duyet")]
    public async Task<ActionResult<KetQuaDuyetNguyenVongDto>> DuyetNguyenVong(int id, [FromBody] XuLyNguyenVongRequest? req, CancellationToken cancellationToken)
    {
        var result = await service.DuyetNguyenVongAsync(id, req?.GhiChu, cancellationToken);
        if (!result.ThanhCong)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Trưởng bộ môn TỪ CHỐI yêu cầu xin nghỉ / lịch bận của giảng viên
    /// </summary>
    [HttpPost("{id:int}/tu-choi")]
    public async Task<ActionResult<KetQuaDuyetNguyenVongDto>> TuChoiNguyenVong(int id, [FromBody] XuLyNguyenVongRequest? req, CancellationToken cancellationToken)
    {
        var result = await service.TuChoiNguyenVongAsync(id, req?.GhiChu, cancellationToken);
        if (!result.ThanhCong)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
