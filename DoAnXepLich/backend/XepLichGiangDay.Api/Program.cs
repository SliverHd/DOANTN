using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using XepLichGiangDay.Api.Data;
using XepLichGiangDay.Api.Security;
using XepLichGiangDay.Api.Services;
using XepLichGiangDay.Api.Services.RangBuoc;
using XepLichGiangDay.Api.Services.ThuatToan;
using XepLichGiangDay.Api.Services.ThuatToan.CpSat;
using XepLichGiangDay.Api.Services.ThuatToan.Genetic;

// Thiet lap InvariantCulture mac dinh toan he thong tranh loi dau phay thap phan vi-VN khi parse/format so thuc
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicy = "ReactFrontend";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhap JWT token theo dinh dang: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication(JwtAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(JwtAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// CSDL SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Dang ky cac Services nghiep vu
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NguoiDungService>();
builder.Services.AddScoped<GiangVienService>();
builder.Services.AddScoped<HocPhanService>();
builder.Services.AddScoped<HocKyService>();
builder.Services.AddScoped<NamHocService>();
builder.Services.AddScoped<BoMonService>();
builder.Services.AddScoped<ImportThoiKhoaBieuService>();
builder.Services.AddScoped<ThoiKhoaBieuService>();
builder.Services.AddScoped<RangBuocService>();
builder.Services.AddScoped<NguyenVongService>();
builder.Services.AddScoped<XepLichService>();

// Dang ky rang buoc & thuat toan xep lich (Strategy Pattern)
builder.Services.AddSingleton<HocKyLockService>();
builder.Services.AddScoped<LuuKetQuaXepLichService>();
builder.Services.AddScoped<BoKiemTraRangBuoc>();
builder.Services.AddScoped<BoDanhGiaPhuongAn>();
builder.Services.AddScoped<TaoDanhSachUngVien>();
builder.Services.AddScoped<ISchedulingAlgorithm, GeneticAlgorithmService>();
builder.Services.AddScoped<ISchedulingAlgorithm, CpSatAlgorithmService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(corsPolicy);

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
