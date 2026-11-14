<#
=====================================================================
 SKILL EXECUTIVO REVIT — Preparação da pasta-base (por computador)
 Versão 0.2 · 13/11/2026
---------------------------------------------------------------------
 O que faz:
   1. Confere a unidade de rede da pasta-base (e mapeia, se faltar)
   2. Valida acesso de leitura e escrita à pasta-base
   3. Localiza o template (.rte) e o projeto de referência (.rvt)
   4. Detecta as versões do Revit e o suplemento do MCP instalados
   5. Grava a configuração local que a skill lê
      (%APPDATA%\SkillExecutivoRevit\config.json)
   6. Registra este computador na pasta-base (_config\maquinas)
   7. Orienta a conectar a pasta no app do Claude

 Uso:
   Clique duas vezes em PUXAR_PASTA_BASE.cmd
   ou:  powershell -ExecutionPolicy Bypass -File puxar-pasta-base.ps1
        [-PastaBase "P:\..."] [-Unc "\\servidor\compart"] [-CriarEstrutura]

 Não move, renomeia nem apaga nenhum arquivo existente.
=====================================================================
#>

param(
    [string]$PastaBase = "",
    [string]$Unc = "",
    [switch]$CriarEstrutura
)

$ErrorActionPreference = "Stop"
$VersaoScript = "0.2"
$PastaConfigLocal = Join-Path $env:APPDATA "SkillExecutivoRevit"
$ArquivoConfigLocal = Join-Path $PastaConfigLocal "config.json"
$Pendencias = New-Object System.Collections.Generic.List[string]

function Escrever-Titulo($texto) { Write-Host ""; Write-Host "== $texto" -ForegroundColor Cyan }
function Ok($texto)    { Write-Host "  [OK]    $texto" -ForegroundColor Green }
function Aviso($texto) { Write-Host "  [AVISO] $texto" -ForegroundColor Yellow; $script:Pendencias.Add($texto) }
function Erro($texto)  { Write-Host "  [ERRO]  $texto" -ForegroundColor Red }

Write-Host "Skill Executivo Revit — preparação da pasta-base (v$VersaoScript)" -ForegroundColor White
Write-Host "Computador: $env:COMPUTERNAME   Usuário: $env:USERNAME"

# ------------------------------------------------------------------
# 0. Reaproveita configuração anterior, se existir
# ------------------------------------------------------------------
$configAnterior = $null
if (Test-Path $ArquivoConfigLocal) {
    try {
        $configAnterior = Get-Content $ArquivoConfigLocal -Raw -Encoding UTF8 | ConvertFrom-Json
        if (-not $PSBoundParameters.ContainsKey("PastaBase") -and $configAnterior.pastaBase) { $PastaBase = $configAnterior.pastaBase }
        if (-not $Unc -and $configAnterior.unc) { $Unc = $configAnterior.unc }
    } catch { }
}

if (-not $PastaBase) {
    $PastaBase = Read-Host "Caminho da pasta-base no servidor (ex.: P:\Projetos\SkillExecutivo)"
    if (-not $PastaBase) { Write-Host "Pasta-base não informada." -ForegroundColor Red; exit 1 }
}

# ------------------------------------------------------------------
# 1. Unidade de rede
# ------------------------------------------------------------------
Escrever-Titulo "1. Unidade de rede"
$unidade = Split-Path $PastaBase -Qualifier   # ex.: "P:"
$uncDetectado = ""

if (-not (Test-Path "$unidade\")) {
    if ($Unc) {
        Write-Host "  Unidade $unidade não encontrada. Mapeando $Unc ..."
        cmd /c "net use $unidade `"$Unc`" /persistent:yes" | Out-Null
        if (Test-Path "$unidade\") { Ok "Unidade $unidade mapeada para $Unc" }
        else { Erro "Não foi possível mapear $unidade para $Unc. Verifique a rede e as credenciais."; exit 1 }
    } else {
        $Unc = Read-Host "Unidade $unidade não encontrada. Caminho de rede para mapear (ex.: \\SERVIDOR\COMPARTILHAMENTO), ou Enter para sair"
        if ($Unc) { cmd /c "net use $unidade `"$Unc`" /persistent:yes" | Out-Null }
        if (Test-Path "$unidade\") { Ok "Unidade $unidade mapeada para $Unc" } else {
        Erro "Unidade $unidade não encontrada neste computador."
        Write-Host "        Rode novamente informando o caminho de rede, por exemplo:"
        Write-Host "        PUXAR_PASTA_BASE.cmd -Unc \\SERVIDOR\COMPARTILHAMENTO"
        Write-Host "        (no computador onde a unidade já funciona, este script mostra o caminho correto)"
        exit 1
        }
    }
}

try {
    $disco = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$unidade'"
    if ($disco.ProviderName) { $uncDetectado = $disco.ProviderName; Ok "Unidade $unidade = $uncDetectado (rede)" }
    else { Ok "Unidade $unidade encontrada (disco local)" }
} catch { Ok "Unidade $unidade encontrada" }
if (-not $Unc -and $uncDetectado) { $Unc = $uncDetectado }

# ------------------------------------------------------------------
# 2. Acesso à pasta-base
# ------------------------------------------------------------------
Escrever-Titulo "2. Pasta-base"
if (-not (Test-Path $PastaBase)) { Erro "Pasta-base não encontrada: $PastaBase"; exit 1 }
Ok "Encontrada: $PastaBase"

$arquivoTeste = Join-Path $PastaBase (".teste_escrita_" + $env:COMPUTERNAME + ".tmp")
try {
    Set-Content -Path $arquivoTeste -Value "teste" -Encoding UTF8
    Remove-Item $arquivoTeste -Force
    Ok "Leitura e escrita permitidas"
    $podeEscrever = $true
} catch {
    Aviso "Sem permissão de escrita na pasta-base (somente leitura). A skill não conseguirá salvar registros lá."
    $podeEscrever = $false
}

if ($CriarEstrutura -and $podeEscrever) {
    foreach ($sub in @("01_TEMPLATE","02_PROJETO_REFERENCIA","03_DOCUMENTOS","04_BIBLIOTECA_DETALHES","05_FAMILIAS","_config")) {
        $p = Join-Path $PastaBase $sub
        if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null; Ok "Criada subpasta $sub" }
    }
}

# ------------------------------------------------------------------
# 3. Arquivos de referência
# ------------------------------------------------------------------
Escrever-Titulo "3. Arquivos de referência"
$todos = Get-ChildItem -Path $PastaBase -Recurse -File -ErrorAction SilentlyContinue
$templates = @($todos | Where-Object { $_.Extension -ieq ".rte" })
$modelos   = @($todos | Where-Object { $_.Extension -ieq ".rvt" -and $_.Name -notmatch "\.\d{4}\.rvt$" })   # ignora backups .0001.rvt
$pdfs      = @($todos | Where-Object { $_.Extension -ieq ".pdf" })
$specs     = @($todos | Where-Object { $_.Extension -ieq ".md" })

function Mais-Recente($lista) { $lista | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }

$template = Mais-Recente $templates
if ($template) {
    Ok ("Template: " + $template.FullName + "  (" + [math]::Round($template.Length/1MB) + " MB, " + $template.LastWriteTime.ToString("dd/MM/yyyy") + ")")
    if ($templates.Count -gt 1) { Aviso ("Há " + $templates.Count + " templates (.rte) na pasta-base; foi escolhido o mais recente.") }
} else { Aviso "Nenhum template (.rte) encontrado na pasta-base." }

$referencia = Mais-Recente $modelos
if ($referencia) {
    Ok ("Projeto de referência: " + $referencia.FullName + "  (" + [math]::Round($referencia.Length/1MB) + " MB)")
    if ($modelos.Count -gt 1) { Aviso ("Há " + $modelos.Count + " modelos (.rvt); foi escolhido o mais recente.") }
} else { Aviso "Nenhum projeto de referência (.rvt) encontrado." }

Ok ("PDFs de referência: " + $pdfs.Count + "   Documentos .md: " + $specs.Count)
if (-not ($specs | Where-Object { $_.Name -ieq "ESPECIFICACAO_SKILL.md" })) { Aviso "ESPECIFICACAO_SKILL.md não encontrado na pasta-base." }

# ------------------------------------------------------------------
# 4. Revit e suplemento do MCP
# ------------------------------------------------------------------
Escrever-Titulo "4. Revit e MCP"
$revitEncontrado = @{}   # ano -> caminho/origem

# (a) Programas instalados (registro do Windows, 64 e 32 bits)
$chavesDesinstalar = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
)
foreach ($chave in $chavesDesinstalar) {
    Get-ItemProperty $chave -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -match "^(Autodesk )?Revit (20\d\d)$" -or $_.DisplayName -match "^(Autodesk )?Revit (20\d\d)\b(?!.*(Content|Interoperability|Add-?in|Plug|Libraries|Help|Material))" } |
        ForEach-Object {
            if ($_.DisplayName -match "(20\d\d)") {
                $ano = $Matches[1]
                if (-not $revitEncontrado.ContainsKey($ano)) {
                    $local = $(if ($_.InstallLocation) { $_.InstallLocation } else { "registro" })
                    $revitEncontrado[$ano] = $local
                }
            }
        }
}

# (b) Chave própria da Autodesk
Get-ChildItem "HKLM:\SOFTWARE\Autodesk\Revit" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.PSChildName -match "(20\d\d)" -and -not $revitEncontrado.ContainsKey($Matches[1])) { $revitEncontrado[$Matches[1]] = "registro Autodesk" }
}

# (c) Revit aberto agora
Get-Process -Name "Revit" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $caminho = $_.Path
        if ($caminho -match "Revit (20\d\d)") { $revitEncontrado[$Matches[1]] = $caminho }
        elseif ($_.MainModule.FileVersionInfo.ProductVersion -match "^(20\d\d)") { $revitEncontrado[$Matches[1]] = $caminho }
    } catch { }
}

# (d) Pastas padrão em qualquer unidade local
Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue | Where-Object { $_.Root -match "^[A-Z]:\\$" } | ForEach-Object {
    foreach ($ano in 2020..2030) {
        $exe = Join-Path $_.Root "Program Files\Autodesk\Revit $ano\Revit.exe"
        if ((Test-Path $exe -ErrorAction SilentlyContinue) -and -not $revitEncontrado.ContainsKey("$ano")) { $revitEncontrado["$ano"] = $exe }
    }
}

$versoesRevit = @($revitEncontrado.Keys | Sort-Object)
if ($versoesRevit.Count -gt 0) { $versoesRevit | ForEach-Object { Ok ("Revit $_ : " + $revitEncontrado[$_]) } }
else { Aviso "Nenhuma instalação do Revit encontrada (registro, processo aberto e pastas padrão)." }
if ($versoesRevit.Count -gt 0 -and $versoesRevit -notcontains "2025") { Aviso "Revit 2025 não encontrado — o template e o projeto de referência são da versão 2025." }

# Suplemento do MCP: procura em todos os anos e nos pacotes de plugins
$suplementosMcp = @()
$pastasPlugins = @()
foreach ($ano in 2020..2030) {
    $pastasPlugins += "$env:APPDATA\Autodesk\Revit\Addins\$ano"
    $pastasPlugins += "$env:ProgramData\Autodesk\Revit\Addins\$ano"
}
$pastasPlugins += "$env:APPDATA\Autodesk\ApplicationPlugins"
$pastasPlugins += "$env:ProgramData\Autodesk\ApplicationPlugins"
foreach ($pasta in $pastasPlugins) {
    if (Test-Path $pasta) {
        Get-ChildItem $pasta -Recurse -Depth 3 -Include *.addin,PackageContents.xml -ErrorAction SilentlyContinue | ForEach-Object {
            $conteudo = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
            if ($_.FullName -match "(?i)mcp" -or $conteudo -match "(?i)mcp") { $suplementosMcp += $_.FullName }
        }
    }
}
$suplementosMcp = @($suplementosMcp | Select-Object -Unique)
if ($suplementosMcp.Count -gt 0) { $suplementosMcp | ForEach-Object { Ok "Suplemento MCP: $_" } }
else { Aviso "Suplemento do MCP do Revit não encontrado nas pastas de Addins. Instale-o antes de usar a skill." }

# ------------------------------------------------------------------
# 5. Configuração local (lida pela skill)
# ------------------------------------------------------------------
Escrever-Titulo "5. Configuração local"
if (-not (Test-Path $PastaConfigLocal)) { New-Item -ItemType Directory -Path $PastaConfigLocal | Out-Null }

$config = [ordered]@{
    versaoScript         = $VersaoScript
    atualizadoEm         = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    computador           = $env:COMPUTERNAME
    usuario              = $env:USERNAME
    pastaBase            = $PastaBase
    unidade              = $unidade
    unc                  = $Unc
    podeEscrever         = $podeEscrever
    template             = $(if ($template) { $template.FullName } else { "" })
    projetoReferencia    = $(if ($referencia) { $referencia.FullName } else { "" })
    pdfsReferencia       = @($pdfs | ForEach-Object { $_.FullName })
    revitVersoes         = $versoesRevit
    suplementosMcp       = $suplementosMcp
    pendencias           = @($Pendencias)
}
$json = $config | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($ArquivoConfigLocal, $json, (New-Object System.Text.UTF8Encoding($false)))
Ok "Gravada em $ArquivoConfigLocal"

# ------------------------------------------------------------------
# 6. Registro deste computador na pasta-base
# ------------------------------------------------------------------
if ($podeEscrever) {
    $pastaMaquinas = Join-Path $PastaBase "_config\maquinas"
    if (-not (Test-Path $pastaMaquinas)) { New-Item -ItemType Directory -Path $pastaMaquinas -Force | Out-Null }
    $arqMaquina = Join-Path $pastaMaquinas ($env:COMPUTERNAME + ".json")
    [System.IO.File]::WriteAllText($arqMaquina, $json, (New-Object System.Text.UTF8Encoding($false)))
    Ok "Computador registrado em $arqMaquina"
}

# ------------------------------------------------------------------
# 7. Resumo e próximo passo
# ------------------------------------------------------------------
Escrever-Titulo "Resumo"
if ($Pendencias.Count -eq 0) { Write-Host "  Tudo pronto neste computador." -ForegroundColor Green }
else {
    Write-Host ("  " + $Pendencias.Count + " pendência(s):") -ForegroundColor Yellow
    $Pendencias | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
}

Write-Host ""
Write-Host "PRÓXIMO PASSO — conectar a pasta no Claude:" -ForegroundColor White
Write-Host "  1. Abra o app do Claude (desktop) e inicie a conversa do projeto."
Write-Host "  2. Clique em 'Adicionar pasta' e selecione:"
Write-Host "       $PastaBase" -ForegroundColor White
Write-Host "     (o caminho já foi copiado para a área de transferência)"
Write-Host "  3. No Revit, ligue o servidor do MCP antes de pedir qualquer tarefa."
try { Set-Clipboard -Value $PastaBase } catch { }

exit 0
