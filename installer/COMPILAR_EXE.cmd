@echo off
chcp 65001 >nul
title Skill Executivo Revit - Gerar o .exe do instalador
rem Passo 1: monta instalar-skill-completo.ps1 com a skill embutida. Passo 2: gera o .exe com o modulo ps2exe.
rem Rode numa maquina Windows com internet, de dentro da pasta installer do repositorio.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0montar-pacote.ps1" || goto :erro
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "if (-not (Get-Module -ListAvailable ps2exe)) { if (-not (Get-PackageProvider -ListAvailable NuGet -ErrorAction SilentlyContinue)) { Install-PackageProvider NuGet -MinimumVersion 2.8.5.201 -Scope CurrentUser -Force | Out-Null }; Install-Module ps2exe -Scope CurrentUser -Force -AllowClobber };" ^
  "Import-Module ps2exe;" ^
  "Invoke-ps2exe -inputFile '%~dp0instalar-skill-completo.ps1' -outputFile '%~dp0InstaladorSkillExecutivoRevit.exe' -title 'Skill Executivo Revit - Instalador' -description 'Instala e atualiza a skill do projeto executivo no Revit' -product 'Skill Executivo Revit' -version '1.2.0.0' -noConfigFile -noConsole -STA -DPIAware -iconFile '%~dp0icone.ico'" || goto :erro
echo.
echo Gerado: %~dp0InstaladorSkillExecutivoRevit.exe
if not "%1"=="/silencioso" pause
exit /b 0
:erro
echo Falhou - veja as mensagens acima.
if not "%1"=="/silencioso" pause
exit /b 1
