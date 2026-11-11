using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.Import;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

public class ImportThoiKhoaBieuRequest
{
    public int MaHocKy { get; set; }
    public int? MaBoMon { get; set; }
    public IFormFile File { get; set; } = null!;
}

[ApiController]
[Route("api/import-thoi-khoa-bieu")]
[Authorize(Roles = "Admin,TruongBoMon")]
public class ImportThoiKhoaBieuController(ImportThoiKhoaBieuService importService) : ControllerBase
{
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview([FromForm] ImportThoiKhoaBieuRequest request, CancellationToken cancellationToken)
    {
        if (request.MaHocKy <= 0)
        {
            return BadRequest("Vui long chon hoc ky hop le.");
        }

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Vui long chon file Excel hop le.");
        }

        try
        {
            var ketQua = await importService.PreviewAsync(request.File, request.MaHocKy, request.MaBoMon, cancellationToken);
            return Ok(ketQua);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Loi doc file Excel: {ex.Message}");
        }
    }

    [HttpPost("xac-nhan")]
    public async Task<IActionResult> XacNhan([FromBody] XacNhanImportRequest request, CancellationToken cancellationToken)
    {
        if (request.MaHocKy <= 0)
        {
            return BadRequest("Vui long chon hoc ky hop le.");
        }

        if (request.DanhSachDong == null || request.DanhSachDong.Count == 0)
        {
            return BadRequest("Danh sach import trong.");
        }

        try
        {
            var soDong = await importService.XacNhanImportAsync(request, cancellationToken);
            return Ok(new { SoDongThanhCong = soDong });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Loi luu du lieu import: {ex.Message}");
        }
    }

    [HttpGet("lich-su/{maHocKy:int}")]
    public async Task<IActionResult> GetLichSu(int maHocKy)
    {
        if (maHocKy <= 0)
        {
            return BadRequest("Ma hoc ky khong hop le.");
        }

        var list = await importService.LayLichSuImportAsync(maHocKy);
        return Ok(list);
    }

    [HttpGet("file-mau")]
    public IActionResult DownloadFileMau()
    {
        var bytes = importService.TaoFileMauExcel();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_ThoiKhoaBieu.xlsx");
    }
}
