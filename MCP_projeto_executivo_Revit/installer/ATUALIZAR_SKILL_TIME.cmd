@echo off
chcp 65001 >nul
title Skill Executivo Revit - Atualizar a skill para o time
rem Gera o ZIP da skill (Area de Trabalho) e o instalador .exe com a versao da pasta skill\.
rem Use: ATUALIZAR_SKILL_TIME.cmd            (ZIP + .exe)
rem      ATUALIZAR_SKILL_TIME.cmd -SemExe    (so o ZIP)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0atualizar-skill-time.ps1" %*
echo.
pause
