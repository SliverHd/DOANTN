@echo off
title Push code to SliverHd/DOANTN
echo ====================================================
echo   DANG TIEN HANH DAY CODE LEN GITHUB SliverHd/DOANTN
echo ====================================================
cd /d D:\doanxeptkb
git add .
git commit -m "Cap nhat ma nguon Do An Xep Lich Giang Day"
git push -u origin main
echo.
pause
