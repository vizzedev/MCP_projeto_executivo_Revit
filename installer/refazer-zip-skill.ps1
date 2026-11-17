<#
 Refaz o ZIP da skill na Área de Trabalho com "/" nos caminhos internos.
 Use se o Claude recusar o envio com "Zip file contains path with invalid characters"
 (ZIP gerado por instaladores anteriores à versão 1.3, que usavam o Compress-Archive do Windows PowerShell 5.1).
 O conteúdo (inclusive o perfil do escritório) não muda; o ZIP antigo fica como .antigo.zip.
#>
param([string]$Zip = "")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
$desk = [Environment]::GetFolderPath("Desktop")
if (-not $Zip) { $z = Get-ChildItem $desk -Filter "executivo-revit-*.zip" | Where-Object { $_.Name -notlike "*.antigo.zip" } | Sort-Object LastWriteTime -Descending | Select-Object -First 1; if ($z) { $Zip = $z.FullName } }
if (-not $Zip -or -not (Test-Path $Zip)) { Write-Host "[ERRO] Não encontrei executivo-revit-*.zip na Área de Trabalho." -ForegroundColor Red; exit 1 }
$antigo = $Zip -replace '\.zip$', '.antigo.zip'
if (Test-Path $antigo) { Remove-Item $antigo -Force }
Move-Item $Zip $antigo
$nome = "executivo-revit"
$out = [System.IO.Compression.ZipFile]::Open($Zip, [System.IO.Compression.ZipArchiveMode]::Create)
$entrada = [System.IO.Compression.ZipFile]::OpenRead($antigo)
try {
    foreach ($e in $entrada.Entries) {
        if (-not $e.Name) { continue }   # pasta
        $rel = $e.FullName.Replace('\', '/').TrimStart('/')
        if (-not $rel.StartsWith("$nome/")) { $rel = "$nome/" + ($rel -replace '^[^/]+/', '') }
        $novo = $out.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
        $s1 = $e.Open(); $s2 = $novo.Open(); $s1.CopyTo($s2); $s2.Dispose(); $s1.Dispose()
    }
} finally { $entrada.Dispose(); $out.Dispose() }
Write-Host "[OK] ZIP refeito: $Zip" -ForegroundColor Green
Write-Host "     Envie este arquivo no Claude (Personalização › Skills). O antigo ficou como $(Split-Path $antigo -Leaf)."
try { explorer.exe "/select,`"$Zip`"" } catch { }
