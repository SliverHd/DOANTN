using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.Auth;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ThongTinDangNhapDto>> Login(DangNhapDto dto)
    {
        var result = await authService.DangNhapAsync(dto);
        if (result is null)
        {
            return Unauthorized("Ten dang nhap hoac mat khau khong dung, hoac tai khoan da bi khoa.");
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("doi-mat-khau")]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauDto dto)
    {
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
        {
            var claimUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimUserId, out var currentUserId) || currentUserId != dto.MaNguoiDung)
            {
                return Forbid();
            }
        }

        var success = await authService.DoiMatKhauAsync(dto);
        return success ? Ok(new { message = "Doi mat khau thanh cong." }) : BadRequest("Mat khau cu khong dung hoac thong tin khong hop le.");
    }
}
