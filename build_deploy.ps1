# Script dong bo va dong goi tu dong cho SmarterASP
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  DANG BUILD FRONTEND (REACT + VITE)...      " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Set-Location "d:\doanxeptkb\DoAnXepLich\frontend"
npm run build

Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "  DANG COPY VAO BACKEND VA DONG GOI...       " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Set-Location "d:\doanxeptkb"
$wwwroot = "d:\doanxeptkb\DoAnXepLich\backend\XepLichGiangDay.Api\wwwroot"
if (Test-Path $wwwroot) {
    Remove-Item -Path $wwwroot -Recurse -Force
}
New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
Copy-Item -Path "d:\doanxeptkb\DoAnXepLich\frontend\dist\*" -Destination $wwwroot -Recurse -Force

$publishDir = "d:\doanxeptkb\publish_site"
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}
dotnet publish "d:\doanxeptkb\DoAnXepLich\backend\XepLichGiangDay.Api\XepLichGiangDay.Api.csproj" -c Release -o $publishDir

$appsettingsPath = "$publishDir\appsettings.json"
$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$appsettings.ConnectionStrings.DefaultConnection = "Server=sql8005.site4now.net;Database=db_ad0add_xeptkb;User Id=db_ad0add_xeptkb_admin;Password=Admin@123456;TrustServerCertificate=True;Encrypt=False;"
$appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath

$zipPath = "d:\doanxeptkb\deploy.zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "  DA TAO XONG FILE MOI: D:\doanxeptkb\deploy.zip        " -ForegroundColor Green
Write-Host "  Ban chi can vao SmarterASP -> Tep -> Upload va Unzip! " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
