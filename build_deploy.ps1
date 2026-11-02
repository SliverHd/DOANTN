# Script Dong bo tu dong 1-Click len SmarterASP qua FTP
$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " [1/4] DANG BUILD FRONTEND (REACT + VITE)...            " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

Set-Location "d:\doanxeptkb\DoAnXepLich\frontend"
npm run build

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " [2/4] DANG BIEN DICH BACKEND (.NET 8 RELEASE)...       " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

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

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " [3/4] DANG KET NOI FTP VA DONG BO LEN SMARTERASP...     " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$ftpBase = "ftp://win8082.site4now.net/site1"
$ftpUser = "hung130903-001"
$ftpPass = "Admin@123456"

function Upload-FtpBytes($remoteRelPath, [byte[]]$bytes) {
    $uri = "$ftpBase/$remoteRelPath"
    $req = [System.Net.FtpWebRequest]::Create($uri)
    $req.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true
    $req.KeepAlive = $false
    $stream = $req.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    $req.GetResponse().Close()
}

function Upload-FtpFileItem($localFile, $remoteRelPath) {
    $bytes = [System.IO.File]::ReadAllBytes($localFile)
    Upload-FtpBytes $remoteRelPath $bytes
    Write-Host "  -> Uploaded: $remoteRelPath" -ForegroundColor Gray
}

function Create-FtpDirectory($remoteRelPath) {
    try {
        $uri = "$ftpBase/$remoteRelPath"
        $req = [System.Net.FtpWebRequest]::Create($uri)
        $req.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.KeepAlive = $false
        $req.GetResponse().Close()
    } catch {
        # Thu muc da ton tai, bo qua
    }
}

function Delete-FtpFileItem($remoteRelPath) {
    try {
        $uri = "$ftpBase/$remoteRelPath"
        $req = [System.Net.FtpWebRequest]::Create($uri)
        $req.Credentials = New-Object System.Net.NetworkCredential($ftpUser, $ftpPass)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $req.KeepAlive = $false
        $req.GetResponse().Close()
    } catch {
        # File khong ton tai, bo qua
    }
}

# Tam dung app de mo khoa DLL
$offlineBytes = [System.Text.Encoding]::UTF8.GetBytes("<h1>Cap nhat he thong...</h1>")
Upload-FtpBytes "app_offline.htm" $offlineBytes
Write-Host "  [+] Tam khoa IIS de cap nhat DLL an toan..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Upload cac file cot loi Backend
Upload-FtpFileItem "$publishDir\XepLichGiangDay.Api.dll" "XepLichGiangDay.Api.dll"
Upload-FtpFileItem "$publishDir\XepLichGiangDay.Api.pdb" "XepLichGiangDay.Api.pdb"
Upload-FtpFileItem "$publishDir\appsettings.json" "appsettings.json"

# Upload Frontend wwwroot
Create-FtpDirectory "wwwroot"
Create-FtpDirectory "wwwroot/assets"

$distFiles = Get-ChildItem -Path "$publishDir\wwwroot" -Recurse -File
foreach ($file in $distFiles) {
    $rel = $file.FullName.Substring("$publishDir\wwwroot\".Length).Replace("\", "/")
    Upload-FtpFileItem $file.FullName "wwwroot/$rel"
}

# Khoi dong lai app
Delete-FtpFileItem "app_offline.htm"
Write-Host "  [+] Da mo lai ung dung thanh cong!" -ForegroundColor Yellow

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host " [4/4] DONG BO HOAN TAT 100%!                           " -ForegroundColor Green
Write-Host " Trang web online da cap nhat ban moi nhat:             " -ForegroundColor Green
Write-Host " http://hung130903-001-site1.ftempurl.com               " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
