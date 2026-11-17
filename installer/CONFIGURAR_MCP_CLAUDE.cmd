@echo off
chcp 65001 >nul
title Skill Executivo Revit - Ligar o MCP do Revit ao Claude Desktop
rem Use quando o suplemento aparece no Revit, mas o conector nao aparece no Claude Desktop.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0configurar-mcp-claude.ps1" %*
echo.
pause
