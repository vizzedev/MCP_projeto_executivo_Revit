@echo off
chcp 65001 >nul
title Skill Executivo Revit - Instalador
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0instalar-skill.ps1" %*
echo.
pause
