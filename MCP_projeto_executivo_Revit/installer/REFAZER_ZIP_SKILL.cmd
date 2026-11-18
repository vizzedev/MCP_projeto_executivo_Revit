@echo off
chcp 65001 >nul
title Skill Executivo Revit - Refazer o ZIP da skill
rem Use se o Claude recusar o ZIP com "Zip file contains path with invalid characters".
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0refazer-zip-skill.ps1" %*
echo.
pause
