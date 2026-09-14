@echo off
title UniNest Auto Launcher
echo ===================================================
echo           Starting UniNest Web Application...
echo ===================================================
start http://localhost:5157
cd /d "%~dp0"
dotnet run --project src/UniNest.Api
pause
