using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/nguyen-vong")]
[Authorize]
public class NguyenVongController(NguyenVongService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NguyenVongDto>>> GetNguyenVong(
        [FromQuery] int maHocKy,
        [FromQuery] int? maGiangVien,
        [FromQuery] int? maBoMon)
    {
        var result = await service.LayNguyenVongAsync(maHocKy, maGiangVien, maBoMon);
        return Ok(result);
    }

    [HttpGet("lich-ban")]
    public async Task<ActionResult<List<LichBanDto>>> GetLichBan(
        [FromQuery] int maHocKy,
        [FromQuery] int? maGiangVien,
        [FromQuery] int? maBoMon,
        [FromQuery] bool? chiSeminar)
    {
        var result = await service.LayLichBanAsync(maHocKy, maGiangVien, maBoMon, chiSeminar);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPost("seminar")]
    public async Task<ActionResult<KetQuaTaoLichSeminarDto>> TaoLichSeminar(
        [FromBody] TaoLichSeminarRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.TaoLichSeminarAsync(request, cancellationToken);
        if (!result.ThanhCong)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpDelete("lich-ban/{id:int}")]
    public async Task<IActionResult> XoaLichBan(int id, CancellationToken cancellationToken)
    {
        var success = await service.XoaLichBanAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound("Không tìm thấy lịch bận / Seminar cần xóa.");
        }
        return Ok(new { Message = "Đã xóa lịch thành công." });
    }

    [Authorize(Roles = "Admin,TruongBoMon")]
    [HttpPost("seminar/xoa-nhom")]
    public async Task<IActionResult> XoaNhomSeminar(
        [FromBody] XoaNhomSeminarRequest request,
        CancellationToken cancellationToken)
    {
        var count = await service.XoaNhomSeminarAsync(request, cancellationToken);
        return Ok(new { SoBanGhiDaXoa = count, Message = $"Đã gỡ lịch Seminar cho {count} giảng viên." });
    }

    [HttpPost]
    public async Task<ActionResult<NguyenVongDto>> ThemNguyenVong([FromBody] ThemNguyenVongDto dto)
    {
        if (dto.MaHocKy <= 0 || dto.MaGiangVien <= 0)
        {
            return BadRequest("Mã học kỳ và mã giảng viên không hợp lệ.");
        }

        if (!DuocPhepThaoTacGiangVien(dto.MaGiangVien))
        {
            return Forbid();
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
        var maGiangVien = await service.LayMaGiangVienCuaNguyenVongAsync(id);
        if (maGiangVien is null)
        {
            return NotFound("Không tìm thấy nguyện vọng cần xóa.");
        }

        if (!DuocPhepThaoTacGiangVien(maGiangVien.Value))
        {
            return Forbid();
        }

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

        if (!DuocPhepThaoTacGiangVien(request.MaGiangVien))
        {
            return Forbid();
        }

        var success = await service.LuuMaTranAsync(request);
        return success ? Ok(new { Message = "Lưu lịch bận và nguyện vọng thành công." }) : BadRequest("Không thể lưu thông tin.");
    }

    /// <summary>
    /// Trưởng bộ môn DUYỆT yêu cầu xin nghỉ / lịch bận của giảng viên:
    /// Tự động kick giáo viên ra khỏi các lớp học phần trùng ca nghỉ
    /// </summary>
    [Authorize(Roles = "Admin,TruongBoMon")]
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
    [Authorize(Roles = "Admin,TruongBoMon")]
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

    private bool DuocPhepThaoTacGiangVien(int maGiangVien)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        if (User.IsInRole("Admin") || User.IsInRole("TruongBoMon"))
        {
            return true;
        }

        var claimMaGv = User.FindFirst("ma_giang_vien")?.Value;
        return int.TryParse(claimMaGv, out var currentMaGv) && currentMaGv == maGiangVien;
    }
}
