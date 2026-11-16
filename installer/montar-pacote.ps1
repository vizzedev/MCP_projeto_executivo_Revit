<#
 Monta instalar-skill-completo.ps1: o instalador com a skill embutida (ZIP em base64 de skill\,
 installer\puxar-pasta-base.ps1, fonts\ e docs\Guia_Skill_Executivo_Revit.pdf). Usado por COMPILAR_EXE.cmd.
 A versão vem do primeiro "## x.y.z" do CHANGELOG.md.
#>
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$versao = (Select-String -Path (Join-Path $repo "CHANGELOG.md") -Pattern '^##\s+(\d+\.\d+\.\d+)' | Select-Object -First 1).Matches[0].Groups[1].Value
$tmp = Join-Path $env:TEMP ("pacote-skill-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path (Join-Path $tmp "installer"), (Join-Path $tmp "docs") -Force | Out-Null
Copy-Item (Join-Path $repo "skill") (Join-Path $tmp "skill") -Recurse
Copy-Item (Join-Path $PSScriptRoot "puxar-pasta-base.ps1") (Join-Path $tmp "installer")
$guia = Join-Path $repo "docs\Guia_Skill_Executivo_Revit.pdf"; if (Test-Path $guia) { Copy-Item $guia (Join-Path $tmp "docs") }
Copy-Item (Join-Path $PSScriptRoot "fonts") (Join-Path $tmp "fonts") -Recurse   # Poppins (SIL OFL) para a janela do instalador
$zip = "$tmp.zip"; Compress-Archive -Path (Join-Path $tmp "*") -DestinationPath $zip -Force
$b64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($zip))
Remove-Item $tmp -Recurse -Force; Remove-Item $zip -Force
$fonte = Get-Content (Join-Path $PSScriptRoot "instalar-skill.ps1") -Raw -Encoding UTF8
# repositório padrão para avisos de versão: installer\repo.txt (opcional, uma linha "usuario/repositorio")
$arqRepo = Join-Path $PSScriptRoot "repo.txt"
if (Test-Path $arqRepo) { $r = (Get-Content $arqRepo -TotalCount 1).Trim(); if ($r) { $fonte = $fonte.Replace('[string]$Repo = ""', '[string]$Repo = "' + $r + '"') } }
$fonte = $fonte.Replace('$VersaoEmbutida = ""', '$VersaoEmbutida = "' + $versao + '"').Replace('$PacoteEmbutido = ""', '$PacoteEmbutido = "' + $b64 + '"')
$saida = Join-Path $PSScriptRoot "instalar-skill-completo.ps1"
[System.IO.File]::WriteAllText($saida, $fonte, (New-Object System.Text.UTF8Encoding($true)))
Write-Host ("Pacote $versao embutido: " + [math]::Round($b64.Length / 1KB) + " KB → $saida")
