<#
 Liga o MCP do Revit ao Claude Desktop (servidor "revit-mcp" no claude_desktop_config.json).
 Use quando o suplemento já aparece no Revit, mas o conector não aparece no Claude Desktop.
 - Acha o servidor instalado com o suplemento (…\Addins\<ano>\revit_mcp_plugin\Commands\RevitMCPCommandSet\server\build\index.js)
   e o Node.js (do sistema ou o que vem com o suplemento).
 - Grava em TODOS os lugares onde o Claude Desktop guarda a configuração: %APPDATA%\Claude, a pasta de pacote
   %LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude e a pasta de dados das versões novas
   %LOCALAPPDATA%\Claude-Data\claude_* (é a que "Configurações › Desenvolvedor › Editar configuração" abre).
 - Mantém o que já existe no arquivo (outros conectores e preferências), faz cópia .bak e grava em UTF-8 SEM BOM
   (com BOM o Claude Desktop não lê o arquivo e nenhum conector aparece).
 Depois: feche o Claude Desktop por completo (inclusive o ícone perto do relógio) e abra de novo.
#>
param([string]$RevitAno = "")
$ErrorActionPreference = "Stop"
$res = New-Object System.Collections.Generic.List[string]
function Msg($t) { $res.Add($t); Write-Host $t }

# servidor e Node.js
$anos = if ($RevitAno) { @($RevitAno) } else { 2027..2023 }
$server = $null; $nodeBundled = $null
foreach ($a in $anos) {
    $base = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$a\revit_mcp_plugin\Commands\RevitMCPCommandSet\server"
    if (-not $server -and (Test-Path (Join-Path $base "build\index.js"))) { $server = Join-Path $base "build\index.js"; if (Test-Path (Join-Path $base "runtime\node.exe")) { $nodeBundled = Join-Path $base "runtime\node.exe" } }
}
if (-not $server) { Msg "[ERRO] Servidor do MCP não encontrado. Instale o MCP do Revit primeiro (suplemento no Revit)."; exit 2 }
$node = $null
$sys = Get-Command node -ErrorAction SilentlyContinue
if ($sys) { $node = $sys.Source } elseif ($nodeBundled) { $node = $nodeBundled }
if (-not $node) { Msg "[ERRO] Node.js não encontrado (nem o do sistema nem o do suplemento). Instale o Node.js LTS (nodejs.org) e rode de novo."; exit 3 }
Msg "[OK] Servidor: $server"
Msg "[OK] Node.js: $node"

# onde o Claude Desktop guarda a configuração
$dirs = @()
$dirs += Join-Path $env:APPDATA "Claude"
Get-ChildItem (Join-Path $env:LOCALAPPDATA "Packages") -Filter "Claude_*" -Directory -ErrorAction SilentlyContinue | ForEach-Object { $dirs += Join-Path $_.FullName "LocalCache\Roaming\Claude" }
Get-ChildItem (Join-Path $env:LOCALAPPDATA "Claude-Data") -Filter "claude_*" -Directory -ErrorAction SilentlyContinue | ForEach-Object { $dirs += $_.FullName }
$temPacote = @($dirs).Count -gt 1
$temApp = (Test-Path (Join-Path $env:LOCALAPPDATA "AnthropicClaude")) -or $temPacote -or (Test-Path (Join-Path $env:APPDATA "Claude")) -or (Test-Path (Join-Path $env:LOCALAPPDATA "Claude-Data"))
if (-not $temApp) { Msg "[ERRO] Claude Desktop não encontrado. Instale (claude.ai/download), abra e entre na conta uma vez, feche e rode de novo."; exit 4 }

$gravados = 0
foreach ($d in $dirs) {
    # a pasta %APPDATA%\Claude só é usada pela versão clássica: cria se não houver a versão de pacote
    if (-not (Test-Path $d)) { if ($temPacote -and $d -eq (Join-Path $env:APPDATA "Claude")) { continue }; New-Item -ItemType Directory -Path $d -Force | Out-Null }
    $arq = Join-Path $d "claude_desktop_config.json"
    $cfg = $null
    if (Test-Path $arq) {
        Copy-Item $arq "$arq.bak" -Force
        try { $txt = [System.IO.File]::ReadAllText($arq).TrimStart([char]0xFEFF); if ($txt.Trim()) { $cfg = $txt | ConvertFrom-Json } }
        catch { Msg "[AVISO] $arq estava inválido (cópia em .bak); será recriado." }
    }
    if (-not $cfg) { $cfg = New-Object PSObject }
    if (-not ($cfg.PSObject.Properties.Name -contains "mcpServers") -or -not $cfg.mcpServers) { $cfg | Add-Member -NotePropertyName "mcpServers" -NotePropertyValue (New-Object PSObject) -Force }
    $entrada = New-Object PSObject
    $entrada | Add-Member -NotePropertyName "command" -NotePropertyValue $node
    $entrada | Add-Member -NotePropertyName "args" -NotePropertyValue @($server)
    $cfg.mcpServers | Add-Member -NotePropertyName "revit-mcp" -NotePropertyValue $entrada -Force
    $json = $cfg | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($arq, $json, (New-Object System.Text.UTF8Encoding($false)))
    Msg "[OK] Configurado: $arq"; $gravados++
}
if ($gravados -eq 0) { Msg "[ERRO] Nenhum arquivo de configuração gravado."; exit 5 }
$aberto = Get-Process -Name "claude" -ErrorAction SilentlyContinue
if ($aberto) { Msg "[AVISO] O Claude Desktop está aberto: feche por completo (inclusive o ícone perto do relógio) e abra de novo para o conector aparecer." }
else { Msg "[OK] Abra o Claude Desktop: o conector revit-mcp deve aparecer." }
exit 0
