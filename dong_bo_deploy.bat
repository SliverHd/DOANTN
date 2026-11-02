@echo off
title Dong bo va Dong goi Deploy SmarterASP
echo ====================================================
echo   TIEN HANH DONG BO VA DONG GOI DEPLOY SMARTERASP
echo ====================================================
cd /d D:\doanxeptkb
powershell -ExecutionPolicy Bypass -File "D:\doanxeptkb\build_deploy.ps1"
echo.
pause
