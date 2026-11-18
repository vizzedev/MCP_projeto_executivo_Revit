<#
 Atualiza a skill para o time a partir da pasta-base (skill\):
   1. confere a skill (SKILL.md, versão e rotinas novas);
   2. gera na Área de Trabalho o ZIP executivo-revit-<versão>.zip, com "/" nos caminhos internos, pronto para enviar
      no Claude (Configurações › Personalização › Skills › substituir a skill "executivo-revit");
   3. gera o instalador InstaladorSkillExecutivoRevit.exe com a skill nova embutida (COMPILAR_EXE.cmd), na pasta-base,
      para o time instalar/atualizar em cada computador.
 Use depois de mudar qualquer arquivo em skill\ (SKILL.md, scripts, references). Parâmetro -SemExe pula o passo 3.
#>
param([switch]$SemExe)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
$base = Split-Path $PSScriptRoot -Parent
$skill = Join-Path $base "skill"
Write-Host "Skill Executivo Revit - atualizar para o time" -ForegroundColor Cyan
Write-Host "Pasta-base: $base"

# 1. conferência
if (-not (Test-Path (Join-Path $skill "SKILL.md"))) { throw "Não encontrei skill\SKILL.md em $base" }
$versao = (Get-Content (Join-Path $skill "VERSAO.txt") -TotalCount 1).Trim()
if (-not $versao) { throw "skill\VERSAO.txt vazio" }
$obrig = @("02_cotas_lote.cs", "02_areas_legenda.cs", "00_preflight.cs", "02_proposta.cs")
$falta = $obrig | Where-Object { -not (Test-Path (Join-Path $skill "scripts\$_")) }
if ($falta) { throw ("Faltam scripts em skill\scripts: " + ($falta -join ", ")) }
$cab = Get-Content (Join-Path $skill "SKILL.md") -TotalCount 4 -Encoding UTF8
if (-not ($cab -match '^name:\s*"?executivo-revit"?')) { throw "O SKILL.md não começa com o cabeçalho name: executivo-revit" }
$nArq = (Get-ChildItem $skill -Recurse -File).Count
Write-Host "[OK] skill $versao conferida ($nArq arquivos)" -ForegroundColor Green

# 2. ZIP para enviar no Claude
$desk = [Environment]::GetFolderPath("Desktop")
$zip = Join-Path $desk "executivo-revit-$versao.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
$out = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $skill -Recurse -File | Where-Object { $_.Name -notlike "~*" -and $_.Name -ne "Thumbs.db" } | ForEach-Object {
        $rel = "executivo-revit/" + $_.FullName.Substring($skill.Length).TrimStart('\').Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($out, $_.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $out.Dispose() }
# confere: SKILL.md na raiz executivo-revit/ e nenhum "\" nos caminhos
$z = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $ruins = $z.Entries | Where-Object { $_.FullName.Contains('\') }
    $temSkill = $z.Entries | Where-Object { $_.FullName -eq "executivo-revit/SKILL.md" }
    if ($ruins -or -not $temSkill) { throw "ZIP gerado com estrutura errada" }
    Write-Host ("[OK] ZIP gerado: $zip (" + $z.Entries.Count + " arquivos)") -ForegroundColor Green
} finally { $z.Dispose() }

# 3. instalador .exe do time
if (-not $SemExe) {
    $cmp = Join-Path $PSScriptRoot "COMPILAR_EXE.cmd"
    if (Test-Path $cmp) {
        Write-Host "Gerando o instalador .exe (precisa de internet na primeira vez, para o módulo ps2exe)..."
        & cmd.exe /c "`"$cmp`" /silencioso"
        if ($LASTEXITCODE -eq 0) { Write-Host "[OK] Instalador atualizado: $(Join-Path $base 'InstaladorSkillExecutivoRevit.exe')" -ForegroundColor Green }
        else { Write-Host "[AVISO] O .exe não foi gerado (veja as mensagens acima). O ZIP continua valendo." -ForegroundColor Yellow }
    } else { Write-Host "[AVISO] COMPILAR_EXE.cmd não encontrado; pulei o .exe." -ForegroundColor Yellow }
}

Write-Host ""
Write-Host "PRÓXIMOS PASSOS" -ForegroundColor Cyan
Write-Host " Você: no Claude, Configurações › Personalização › Skills › executivo-revit › substituir pelo ZIP"
Write-Host "       executivo-revit-$versao.zip (está na Área de Trabalho)."
Write-Host " Time: cada um roda InstaladorSkillExecutivoRevit.exe da pasta-base (atualiza a skill e os scripts) e envia o"
Write-Host "       ZIP que ele deixa na Área de Trabalho no Claude, do mesmo jeito. Se a skill for da organização,"
Write-Host "       basta o administrador enviar o ZIP uma vez."
try { explorer.exe "/select,`"$zip`"" } catch { }
