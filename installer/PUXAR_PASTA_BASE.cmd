@echo off
chcp 65001 >nul
title Skill Executivo Revit - Pasta-base
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0puxar-pasta-base.ps1" %*
echo.
pause
