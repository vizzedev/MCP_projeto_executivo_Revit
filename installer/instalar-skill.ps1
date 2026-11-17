<#
=====================================================================
 SKILL EXECUTIVO REVIT — Instalador / atualizador (janela)
 Versão 1.3
---------------------------------------------------------------------
 Assistente em janela (Boas-vindas › Configuração › Perfil › Instalação › Concluído):
   - todas as perguntas nas duas primeiras telas; depois roda sozinho
   - arquivos da skill embutidos no .exe (montar-pacote.ps1); sem pacote, usa os arquivos ao lado ou o GitHub
   - instala o MCP do Revit (instalador oficial do projeto do MCP) se faltar
   - confere pasta-base, template, Revit e o servidor do MCP (puxar-pasta-base.ps1)
   - gera o ZIP da skill (pasta com o nome da skill) na Área de Trabalho, instala guia e atalhos
   - ao final mostra os passos que só o usuário pode fazer e abre a pasta do ZIP
 Uso:   InstaladorSkillExecutivoRevit.exe  (ou INSTALAR.cmd)
        opcionais: -PastaBase X:\...  -Repo usuario/repositorio  -RevitAno 2025  -McpRepo usuario/repositorio
                   -McpTag v1.2.0  -AtualizarMcp  -SemMcp  -Previa <pasta>  (salva imagens das telas e sai)
 Não altera o template master nem projetos.
=====================================================================
#>
param(
    [string]$PastaBase = "",
    [string]$Repo = "",
    [string]$RevitAno = "2025",
    [string]$McpRepo = "LuDattilo/revit-mcp-server",
    [string]$McpTag = "latest",
    [switch]$AtualizarMcp,
    [switch]$SemMcp,
    [string]$Previa = ""
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$VersaoInstalador = "1.3"
# ---- pacote embutido: preenchido por montar-pacote.ps1 (base64 de um ZIP com skill\, installer\, docs\ e fonts\) ----
$VersaoEmbutida = ""
$PacoteEmbutido = ""
# ---- fim do pacote ----

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
try { [System.Windows.Forms.Application]::EnableVisualStyles() } catch { }

$Raiz      = Join-Path $env:APPDATA "SkillExecutivoRevit"
$Local     = Join-Path $env:LOCALAPPDATA "SkillExecutivoRevit"
$Cache     = Join-Path $Local "versoes"
$ArqVersao = Join-Path $Raiz "versao.json"
$ArqPerfil = Join-Path $Raiz "escritorio.json"
$ArqInst   = Join-Path $Raiz "instalador.json"
foreach ($p in @($Raiz, $Local, $Cache)) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null } }
$global:Avisos = New-Object System.Collections.Generic.List[string]

function Gravar-Json($obj, $arq) { [System.IO.File]::WriteAllText($arq, ($obj | ConvertTo-Json -Depth 6), (New-Object System.Text.UTF8Encoding($false))) }
function Ler-Json($arq) { if ($arq -and (Test-Path $arq)) { try { return Get-Content $arq -Raw -Encoding UTF8 | ConvertFrom-Json } catch { } } return $null }
$DirScript = if ($PSScriptRoot) { $PSScriptRoot } else { "" }

# ------------------------------------------------------------------
# Arquivos: pacote embutido > arquivos ao lado do instalador > GitHub (na instalação)
# ------------------------------------------------------------------
$global:versao = $null; $global:pastaVersao = $null
if ($PacoteEmbutido) {
    $global:versao = $VersaoEmbutida
    $global:pastaVersao = Join-Path $Cache ($VersaoEmbutida -replace '[^\w\.-]', '_')
    $zipTmp = Join-Path $env:TEMP ("skill-pacote-" + [guid]::NewGuid().ToString("N") + ".zip")
    [System.IO.File]::WriteAllBytes($zipTmp, [Convert]::FromBase64String($PacoteEmbutido))
    if (Test-Path $global:pastaVersao) { try { Remove-Item $global:pastaVersao -Recurse -Force } catch { } }
    Expand-Archive -Path $zipTmp -DestinationPath $global:pastaVersao -Force
    Remove-Item $zipTmp -Force -ErrorAction SilentlyContinue
} elseif ($DirScript -and (Test-Path (Join-Path (Split-Path $DirScript -Parent) "skill\SKILL.md"))) {
    $global:pastaVersao = Split-Path $DirScript -Parent; $global:versao = "local"
}

# ------------------------------------------------------------------
# Visual: cores e fontes do guia (Poppins; Segoe UI se faltar)
# ------------------------------------------------------------------
function Cor($h) { [System.Drawing.ColorTranslator]::FromHtml($h) }
$global:Paleta = @{ Escuro = Cor "#1E1E1E"; Acento = Cor "#A51C1C"; AcentoEsc = Cor "#861616"; Texto = Cor "#2B2B2B"; Cinza = Cor "#6B6B6B"
        Claro = Cor "#F2F2F0"; Linha = Cor "#D9D9D6"; Branco = [System.Drawing.Color]::White; CinzaLat = Cor "#9A9A9A"
        CinzaEsc = Cor "#5A5A5A"; ClaroLat = Cor "#CFCFCF"; Verde = Cor "#3A8A3A"; VerdeClaro = Cor "#EEF6EE"; Ambar = Cor "#C98A00"; AmbarClaro = Cor "#FFF7E6" }
$global:pfc = New-Object System.Drawing.Text.PrivateFontCollection
$dirsFonte = @()
if ($global:pastaVersao) { $dirsFonte += (Join-Path $global:pastaVersao "fonts"); $dirsFonte += (Join-Path $global:pastaVersao "installer\fonts") }
if ($DirScript) { $dirsFonte += (Join-Path $DirScript "fonts") }
foreach ($d in $dirsFonte) { if (Test-Path $d) { Get-ChildItem $d -Filter "Poppins-*.ttf" | ForEach-Object { try { $global:pfc.AddFontFile($_.FullName) } catch { } }; break } }
function Fonte([string]$peso, [single]$tam) {
    $nome = if ($peso -eq "Regular") { "Poppins" } else { "Poppins $peso" }
    $fam = $global:pfc.Families | Where-Object { $_.Name -eq $nome } | Select-Object -First 1
    if ($fam) { return New-Object System.Drawing.Font($fam, $tam, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Point) }
    $alt = @{ Regular = "Segoe UI"; Light = "Segoe UI Light"; Medium = "Segoe UI Semibold"; SemiBold = "Segoe UI Semibold" }[$peso]
    return New-Object System.Drawing.Font($alt, $tam)
}
$g0 = [System.Drawing.Graphics]::FromHwnd([IntPtr]::Zero); $global:esc = $g0.DpiX / 96.0; $g0.Dispose()
function S($v) { [int][math]::Round($v * $global:esc) }
function Pt($x, $y) { New-Object System.Drawing.Point((S $x), (S $y)) }
function Tam($w, $h) { New-Object System.Drawing.Size((S $w), (S $h)) }

function Rotulo($pai, [string]$texto, $x, $y, $w, $h, $fonte, $cor) {
    $l = New-Object System.Windows.Forms.Label
    $l.AutoSize = $false; $l.UseMnemonic = $false; $l.UseCompatibleTextRendering = $true
    $l.Text = $texto; $l.Location = Pt $x $y; $l.Size = Tam $w $h; $l.Font = $fonte; $l.ForeColor = $cor
    $l.BackColor = [System.Drawing.Color]::Transparent
    $pai.Controls.Add($l); return $l
}
function Bloco($pai, $x, $y, $w, $h, $cor) {
    $p = New-Object System.Windows.Forms.Panel
    $p.Location = Pt $x $y; $p.Size = Tam $w $h; $p.BackColor = $cor
    $pai.Controls.Add($p); return $p
}
function Botao($pai, [string]$texto, $x, $y, $w, $h, [bool]$primario) {
    $b = New-Object System.Windows.Forms.Button
    $b.FlatStyle = "Flat"; $b.UseCompatibleTextRendering = $true; $b.UseMnemonic = $false
    $b.Text = $texto; $b.Location = Pt $x $y; $b.Size = Tam $w $h; $b.Font = Fonte "Medium" 9; $b.Cursor = [System.Windows.Forms.Cursors]::Hand
    if ($primario) {
        $b.BackColor = $global:Paleta.Escuro; $b.ForeColor = $global:Paleta.Branco; $b.FlatAppearance.BorderSize = 0
        $b.FlatAppearance.MouseOverBackColor = $global:Paleta.Acento; $b.FlatAppearance.MouseDownBackColor = $global:Paleta.AcentoEsc
    } else {
        $b.BackColor = $global:Paleta.Branco; $b.ForeColor = $global:Paleta.Texto; $b.FlatAppearance.BorderSize = 1; $b.FlatAppearance.BorderColor = $global:Paleta.Linha
        $b.FlatAppearance.MouseOverBackColor = $global:Paleta.Claro; $b.FlatAppearance.MouseDownBackColor = $global:Paleta.Linha
    }
    $pai.Controls.Add($b); return $b
}
function Campo($pai, [string]$rotulo, $x, $y, $w, [string]$valor) {
    Rotulo $pai $rotulo $x $y $w 20 (Fonte "Medium" 8.5) $global:Paleta.Texto | Out-Null
    $moldura = Bloco $pai $x ($y + 22) $w 34 $global:Paleta.Linha
    $fundo = New-Object System.Windows.Forms.Panel
    $fundo.Location = New-Object System.Drawing.Point(1, 1); $fundo.Size = New-Object System.Drawing.Size(($moldura.Width - 2), ($moldura.Height - 2)); $fundo.BackColor = $global:Paleta.Branco
    $moldura.Controls.Add($fundo)
    $tb = New-Object System.Windows.Forms.TextBox
    $tb.BorderStyle = "None"; $tb.Font = New-Object System.Drawing.Font("Segoe UI", 10); $tb.ForeColor = $global:Paleta.Texto; $tb.BackColor = $global:Paleta.Branco
    $tb.Width = $fundo.Width - (S 20); $tb.Location = New-Object System.Drawing.Point((S 10), [int](($fundo.Height - $tb.Height) / 2))
    if ($valor) { $tb.Text = $valor }
    $tb.Tag = $moldura
    $tb.Add_Enter({ $this.Tag.BackColor = $global:Paleta.Acento })
    $tb.Add_Leave({ $this.Tag.BackColor = $global:Paleta.Linha })
    $fundo.Tag = $tb; $fundo.Add_Click({ $this.Tag.Focus() })
    $fundo.Controls.Add($tb); return $tb
}
# caixa de marcação desenhada (Tag = marcada)
function Caixa($pai, [string]$texto, $x, $y, $w, [bool]$marcada) {
    $cx = New-Object System.Windows.Forms.Panel
    $cx.Location = Pt $x ($y + 2); $cx.Size = Tam 18 18; $cx.Tag = $marcada; $cx.Cursor = [System.Windows.Forms.Cursors]::Hand
    $cx.BackColor = [System.Drawing.Color]::Transparent
    $cx.Add_Paint({ try {
        $g = $_.Graphics; $g.SmoothingMode = "AntiAlias"
        $r = New-Object System.Drawing.Rectangle(0, 0, ($this.Width - 1), ($this.Height - 1))
        if ($this.Tag) {
            $g.FillRectangle((New-Object System.Drawing.SolidBrush($global:Paleta.Acento)), $r)
            $pen = New-Object System.Drawing.Pen($global:Paleta.Branco, [single](S 2))
            $w = $this.Width
            $g.DrawLines($pen, [System.Drawing.Point[]]@((New-Object System.Drawing.Point([int]($w * 0.24), [int]($w * 0.52))), (New-Object System.Drawing.Point([int]($w * 0.42), [int]($w * 0.70))), (New-Object System.Drawing.Point([int]($w * 0.77), [int]($w * 0.32)))))
        } else {
            $g.FillRectangle((New-Object System.Drawing.SolidBrush($global:Paleta.Branco)), $r)
            $g.DrawRectangle((New-Object System.Drawing.Pen($global:Paleta.CinzaLat, [single](S 1))), $r)
        }
    } catch { } })
    $cx.Add_Click({ $this.Tag = -not $this.Tag; $this.Invalidate() })
    $pai.Controls.Add($cx)
    $l = Rotulo $pai $texto ($x + 28) $y $w 22 (Fonte "Regular" 9.5) $global:Paleta.Texto
    $l.Cursor = [System.Windows.Forms.Cursors]::Hand; $l.Tag = $cx
    $l.Add_Click({ $this.Tag.Tag = -not $this.Tag.Tag; $this.Tag.Invalidate() })
    return $cx
}
# ícone de estado de etapa (Tag = pendente | andamento | ok | aviso | erro)
$global:giro = 0
function IconeEstado($pai, $x, $y) {
    $ic = New-Object System.Windows.Forms.Panel
    $ic.Location = Pt $x $y; $ic.Size = Tam 22 22; $ic.Tag = "pendente"; $ic.BackColor = [System.Drawing.Color]::Transparent
    $ic.Add_Paint({ try {
        $g = $_.Graphics; $g.SmoothingMode = "AntiAlias"
        $s = $this.Width; $m = [single](S 1.5); $r = New-Object System.Drawing.RectangleF($m, $m, ($s - 2 * $m - 1), ($s - 2 * $m - 1))
        $pen2 = [single](S 2)
        switch ($this.Tag) {
            "pendente"  { $g.DrawEllipse((New-Object System.Drawing.Pen($global:Paleta.Linha, $pen2)), $r) }
            "andamento" { $g.DrawEllipse((New-Object System.Drawing.Pen($global:Paleta.Claro, $pen2)), $r)
                          $g.DrawArc((New-Object System.Drawing.Pen($global:Paleta.Acento, $pen2)), $r, [single]$global:giro, [single]100) }
            "ok"        { $g.FillEllipse((New-Object System.Drawing.SolidBrush($global:Paleta.Verde)), $r)
                          $p = New-Object System.Drawing.Pen($global:Paleta.Branco, $pen2)
                          $g.DrawLines($p, [System.Drawing.PointF[]]@((New-Object System.Drawing.PointF(($s * 0.30), ($s * 0.52))), (New-Object System.Drawing.PointF(($s * 0.45), ($s * 0.66))), (New-Object System.Drawing.PointF(($s * 0.71), ($s * 0.37))))) }
            "aviso"     { $g.FillEllipse((New-Object System.Drawing.SolidBrush($global:Paleta.Ambar)), $r)
                          $p = New-Object System.Drawing.Pen($global:Paleta.Branco, $pen2)
                          $g.DrawLine($p, ($s / 2), ($s * 0.27), ($s / 2), ($s * 0.58)); $g.FillEllipse((New-Object System.Drawing.SolidBrush($global:Paleta.Branco)), ($s / 2 - $pen2 / 2 - 0.5), ($s * 0.68), ($pen2 + 1), ($pen2 + 1)) }
            "erro"      { $g.FillEllipse((New-Object System.Drawing.SolidBrush($global:Paleta.Acento)), $r)
                          $p = New-Object System.Drawing.Pen($global:Paleta.Branco, $pen2); $a = $s * 0.34; $b = $s * 0.66
                          $g.DrawLine($p, $a, $a, $b, $b); $g.DrawLine($p, $b, $a, $a, $b) }
        }
    } catch { } })
    $pai.Controls.Add($ic); return $ic
}

# ------------------------------------------------------------------
# Estado atual do computador
# ------------------------------------------------------------------
$inst = Ler-Json $ArqInst
$cfgAnt = Ler-Json (Join-Path $Raiz "config.json")
if (-not $PastaBase -and $cfgAnt -and $cfgAnt.pastaBase) { $PastaBase = $cfgAnt.pastaBase }
$uncAnt = if ($cfgAnt -and $cfgAnt.unc) { $cfgAnt.unc } else { "" }
if (-not $Repo -and $inst -and $inst.repo) { $Repo = $inst.repo }
$pastaAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitAno"
$pastaPlugin = Join-Path $pastaAddins "revit_mcp_plugin"
function Tem-Plugin { (Test-Path (Join-Path $pastaAddins "mcp-servers-for-revit.addin")) -and (Test-Path (Join-Path $pastaPlugin "RevitMCPPlugin.dll")) }
function Tem-Claude {
    $cfgs = @(Join-Path $env:APPDATA "Claude\claude_desktop_config.json")
    Get-ChildItem (Join-Path $env:LOCALAPPDATA "Packages") -Filter "Claude_*" -Directory -ErrorAction SilentlyContinue | ForEach-Object { $cfgs += Join-Path $_.FullName "LocalCache\Roaming\Claude\claude_desktop_config.json" }
    Get-ChildItem (Join-Path $env:LOCALAPPDATA "Claude-Data") -Filter "claude_*" -Directory -ErrorAction SilentlyContinue | ForEach-Object { $cfgs += Join-Path $_.FullName "claude_desktop_config.json" }
    foreach ($cc in $cfgs) { $j = Ler-Json $cc; if ($j -and $j.mcpServers -and (@($j.mcpServers.PSObject.Properties.Name) -match '(?i)revit')) { return $true } }
    return $false
}
# Claude Desktop instalado? (o instalador do MCP só configura o Claude se ele já existir e tiver sido aberto uma vez)
function Tem-ClaudeDesktop {
    if (Test-Path (Join-Path $env:LOCALAPPDATA "AnthropicClaude")) { return $true }
    if (Get-ChildItem (Join-Path $env:LOCALAPPDATA "Packages") -Filter "Claude_*" -Directory -ErrorAction SilentlyContinue) { return $true }
    if (Test-Path (Join-Path $env:APPDATA "Claude")) { return $true }
    if (Test-Path (Join-Path $env:LOCALAPPDATA "Claude-Data")) { return $true }
    try { if (Get-AppxPackage -Name "*Claude*" -ErrorAction SilentlyContinue) { return $true } } catch { }
    return $false
}
$temPlugin = Tem-Plugin; $temClaude = Tem-Claude; $temDesktop = Tem-ClaudeDesktop
$mcpOk = $temPlugin -and $temClaude
function Perfil-Modelo {
    $m = $null
    if ($global:pastaVersao) { $m = Ler-Json (Join-Path $global:pastaVersao "skill\config\escritorio.json") }
    if (-not $m) { $m = [pscustomobject]@{ nome = ""; endereco_contato = ""; prefixo = ""; responsavel_padrao = [pscustomobject]@{ autor = ""; cau = "" }; executor_padrao = [pscustomobject]@{ nome = ""; crea = "" }; caminhos = [pscustomobject]@{ pasta_projetos = ""; pasta_base = ""; template_master = "" } } }
    return $m
}
function Limpo($v) { if ($v -and ("$v" -notmatch '<<')) { return "$v" } else { return "" } }

# ------------------------------------------------------------------
# Janela
# ------------------------------------------------------------------
$LARG = 840; $ALT = 620; $LAT = 248
$form = New-Object System.Windows.Forms.Form
$form.FormBorderStyle = "None"; $form.Size = Tam $LARG $ALT; $form.StartPosition = "CenterScreen"
$form.BackColor = $global:Paleta.Linha; $form.Text = "Skill Executivo Revit — Instalador"; $form.KeyPreview = $true
$form.AutoScaleMode = "None"
try {
    $exeAtual = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    if ($exeAtual -notmatch '(?i)\\powershell(_ise)?\.exe$|\\pwsh\.exe$') { $form.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($exeAtual) }
    elseif ($DirScript -and (Test-Path (Join-Path $DirScript "icone.ico"))) { $form.Icon = New-Object System.Drawing.Icon((Join-Path $DirScript "icone.ico")) }
} catch { }

# lateral escura
$lateral = Bloco $form 0 0 $LAT $ALT $global:Paleta.Escuro
Bloco $lateral 36 48 28 4 $global:Paleta.Acento | Out-Null
Rotulo $lateral ("INSTALADOR  ·  v" + $VersaoInstalador) 36 62 200 18 (Fonte "Medium" 7.5) $global:Paleta.CinzaLat | Out-Null
Rotulo $lateral "Skill Executivo`nRevit" 36 80 200 72 (Fonte "SemiBold" 17) $global:Paleta.Branco | Out-Null
Rotulo $lateral "Projeto executivo automatizado`nno Revit, com o Claude." 36 160 200 44 (Fonte "Light" 8.5) $global:Paleta.ClaroLat | Out-Null
$nomesEtapas = @("Boas-vindas", "Configuração", "Perfil do escritório", "Instalação", "Concluído")
$global:latNum = @(); $global:latNome = @()
$marcador = Bloco $lateral 0 0 3 22 $global:Paleta.Acento
for ($i = 0; $i -lt $nomesEtapas.Count; $i++) {
    $yy = 248 + $i * 38
    $global:latNum += Rotulo $lateral ("0" + ($i + 1)) 36 $yy 28 22 (Fonte "Medium" 8.5) $global:Paleta.CinzaEsc
    $global:latNome += Rotulo $lateral $nomesEtapas[$i] 68 $yy 170 22 (Fonte "Regular" 9.5) $global:Paleta.CinzaLat
}
Rotulo $lateral ($env:COMPUTERNAME + "  ·  " + $env:USERNAME) 36 ($ALT - 52) 190 18 (Fonte "Regular" 7.5) $global:Paleta.CinzaEsc | Out-Null
# arrastar a janela pela lateral
$global:arrasto = $null
$md = { if ($_.Button -eq "Left") { $global:arrasto = [System.Windows.Forms.Cursor]::Position; $global:origem = $form.Location } }
$mm = { if ($global:arrasto) { $p = [System.Windows.Forms.Cursor]::Position; $form.Location = New-Object System.Drawing.Point(($global:origem.X + $p.X - $global:arrasto.X), ($global:origem.Y + $p.Y - $global:arrasto.Y)) } }
$mu = { $global:arrasto = $null }
foreach ($ctl in @($lateral) + @($lateral.Controls)) { $ctl.Add_MouseDown($md); $ctl.Add_MouseMove($mm); $ctl.Add_MouseUp($mu) }

# conteúdo branco (1 px de borda cinza à direita, em cima e embaixo)
$conteudo = New-Object System.Windows.Forms.Panel
$conteudo.Location = New-Object System.Drawing.Point((S $LAT), 1); $conteudo.Size = New-Object System.Drawing.Size(((S $LARG) - (S $LAT) - 1), ((S $ALT) - 2)); $conteudo.BackColor = $global:Paleta.Branco
$form.Controls.Add($conteudo)
$LC = $LARG - $LAT   # largura útil do conteúdo (unidades lógicas)
$X0 = 44; $WC = $LC - 2 * $X0
# fechar (×)
$fechar = New-Object System.Windows.Forms.Panel
$fechar.Location = Pt ($LC - 46) 14; $fechar.Size = Tam 30 30; $fechar.Cursor = [System.Windows.Forms.Cursors]::Hand; $fechar.Tag = $false
$fechar.Add_Paint({ try { $g = $_.Graphics; $g.SmoothingMode = "AntiAlias"; $cor = if ($this.Tag) { $global:Paleta.Acento } else { $global:Paleta.CinzaLat }
    $p = New-Object System.Drawing.Pen($cor, [single](S 1.6)); $a = $this.Width * 0.34; $b = $this.Width * 0.66
    $g.DrawLine($p, $a, $a, $b, $b); $g.DrawLine($p, $b, $a, $a, $b) } catch { } })
$fechar.Add_MouseEnter({ $this.Tag = $true; $this.Invalidate() }); $fechar.Add_MouseLeave({ $this.Tag = $false; $this.Invalidate() })
$fechar.Add_Click({ if ($global:pag -ne 3) { $form.Close() } })
$conteudo.Controls.Add($fechar)

# rodapé
$YR = $ALT - 2 - 84
Bloco $conteudo $X0 $YR $WC 1 $global:Paleta.Linha | Out-Null
$btPrim = Botao $conteudo "Continuar" ($LC - $X0 - 150) ($YR + 22) 150 40 $true
$btVoltar = Botao $conteudo "Voltar" ($LC - $X0 - 150 - 12 - 110) ($YR + 22) 110 40 $false
$btAux1 = Botao $conteudo "Abrir pasta do ZIP" $X0 ($YR + 22) 160 40 $false
$btAux2 = Botao $conteudo "Abrir o guia" ($X0 + 172) ($YR + 22) 120 40 $false
$rodapeTxt = Rotulo $conteudo "" $X0 ($YR + 32) 360 22 (Fonte "Regular" 8.5) $global:Paleta.Cinza
$form.AcceptButton = $btPrim

function Pagina { $p = New-Object System.Windows.Forms.Panel; $p.Location = New-Object System.Drawing.Point(0, (S 0)); $p.Size = New-Object System.Drawing.Size($conteudo.Width, (S $YR)); $p.BackColor = $global:Paleta.Branco; $p.Visible = $false; $conteudo.Controls.Add($p); return $p }
function Cabecalho($pg, [string]$kicker, [string]$titulo, [string]$texto) {
    Rotulo $pg $kicker.ToUpper() $X0 48 $WC 18 (Fonte "Medium" 8) $global:Paleta.Acento | Out-Null
    $t = Rotulo $pg $titulo $X0 68 $WC 40 (Fonte "SemiBold" 19) $global:Paleta.Texto
    $d = Rotulo $pg $texto $X0 112 $WC 46 (Fonte "Regular" 9.5) $global:Paleta.Cinza
    return @($t, $d)
}

# --- página 0: boas-vindas
$pg0 = Pagina
Cabecalho $pg0 "Bem-vindo" "Instalar a skill do projeto executivo" "Este assistente prepara o computador para usar a skill do Claude que monta proposta, executivo e detalhamento no Revit." | Out-Null
$caixa0 = Bloco $pg0 $X0 180 $WC 236 $global:Paleta.Claro
$itens0 = @(
    @("Arquivos da skill", "Extraídos deste instalador, com o perfil do escritório preenchido."),
    @("MCP do Revit", "O suplemento que conecta o Revit ao Claude, se ainda não estiver instalado."),
    @("Conferência", "Pasta-base, template master, Revit e servidor do MCP."),
    @("Próximos passos", "O que só você pode fazer no Claude e no Revit, explicado no final."))
for ($i = 0; $i -lt 4; $i++) {
    $yy = 20 + $i * 52
    Rotulo $caixa0 ([string]($i + 1)) 22 ($yy - 2) 24 28 (Fonte "SemiBold" 13) $global:Paleta.Acento | Out-Null
    Rotulo $caixa0 $itens0[$i][0] 54 $yy ($WC - 70) 20 (Fonte "Medium" 9.5) $global:Paleta.Texto | Out-Null
    Rotulo $caixa0 $itens0[$i][1] 54 ($yy + 20) ($WC - 70) 20 (Fonte "Regular" 8.5) $global:Paleta.Cinza | Out-Null
}
Rotulo $pg0 "Antes de continuar, feche o Revit e salve o que estiver aberto." $X0 436 $WC 20 (Fonte "Regular" 8.5) $global:Paleta.Cinza | Out-Null

# --- página 1: configuração
$pg1 = Pagina
Cabecalho $pg1 "Configuração" "Onde fica a pasta-base?" "A pasta-base no servidor guarda o template master e os registros dos projetos. As perguntas ficam nestas telas; depois a instalação roda sozinha." | Out-Null
$tbBase = Campo $pg1 "Pasta-base do escritório" $X0 168 ($WC - 122) $PastaBase
$btProcurar = Botao $pg1 "Procurar…" ($X0 + $WC - 110) 190 110 34 $false
$btProcurar.Font = Fonte "Medium" 8.5
$tbUnc = Campo $pg1 "Caminho de rede (só se a unidade não existir neste computador)" $X0 234 $WC $uncAnt
$yMcp = 300
$tbRepo = $null
if (-not $inst) { $tbRepo = Campo $pg1 "Repositório no GitHub para avisos de versão (opcional)" $X0 300 $WC $Repo; $yMcp = 368 }
$txtMcp = if ($mcpOk) { "Reinstalar o MCP do Revit" } else { "Instalar o MCP do Revit" }
$cxMcp = Caixa $pg1 $txtMcp $X0 $yMcp 220 ((-not $mcpOk -or $AtualizarMcp) -and -not $SemMcp)
$stMcp = if ($mcpOk) { "Já instalado neste computador" } elseif (-not $temDesktop) { "Claude Desktop não instalado" } elseif ($temPlugin) { "Falta configurar no Claude Desktop" } else { "Não encontrado neste computador" }
$lbStMcp = Rotulo $pg1 $stMcp ($X0 + 256) $yMcp ($WC - 256) 22 (Fonte "Medium" 8.5) $(if ($mcpOk) { $global:Paleta.Verde } else { $global:Paleta.Acento })
$lbStMcp.TextAlign = "TopRight"
$txtNota = if ($temDesktop) { "Durante a instalação do MCP, o Revit e o Claude Desktop precisam estar fechados." } else { "Instale o Claude Desktop, abra e entre na sua conta uma vez, feche e só então continue." }
$lbNota = Rotulo $pg1 $txtNota ($X0 + 28) ($yMcp + 24) ($WC - 28) 20 (Fonte "Regular" 8) $(if ($temDesktop) { $global:Paleta.Cinza } else { $global:Paleta.Acento })
if (-not $temDesktop) {
    $lnkClaude = Rotulo $pg1 "Baixar o Claude Desktop" ($X0 + 28) ($yMcp + 42) 220 20 (Fonte "Medium" 8.5) $global:Paleta.Acento
    $lnkClaude.Cursor = [System.Windows.Forms.Cursors]::Hand
    $lnkClaude.Add_Click({ try { Start-Process "https://claude.ai/download" } catch { } })
}
$erro1 = Rotulo $pg1 "" $X0 ($yMcp + 66) $WC 22 (Fonte "Medium" 8.5) $global:Paleta.Acento
$btProcurar.Add_Click({
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.Description = "Escolha a pasta-base do escritório (onde ficam _template e _config)"
    if ($tbBase.Text -and (Test-Path $tbBase.Text)) { $dlg.SelectedPath = $tbBase.Text }
    if ($dlg.ShowDialog($form) -eq "OK") { $tbBase.Text = $dlg.SelectedPath }
})

# --- página 2: perfil
$pg2 = Pagina
Cabecalho $pg2 "Perfil do escritório" "Dados que vão no carimbo" "Preenchidos uma única vez. Ficam neste computador e, se você quiser, na pasta-base para os outros computadores." | Out-Null
$WM = [int](($WC - 16) / 2); $X1 = $X0 + $WM + 16
$tbNome    = Campo $pg2 "Nome do escritório" $X0 160 $WM ""
$tbPrefixo = Campo $pg2 "Prefixo dos tipos e famílias" $X1 160 $WM ""
$tbContato = Campo $pg2 "Endereço, telefone e rede social (rodapé do carimbo)" $X0 222 $WC ""
$tbResp    = Campo $pg2 "Responsável técnico" $X0 284 $WM ""
$tbCau     = Campo $pg2 "Registro CAU" $X1 284 $WM ""
$tbExec    = Campo $pg2 "Responsável pela execução" $X0 346 $WM ""
$tbCrea    = Campo $pg2 "Registro CREA / CAU" $X1 346 $WM ""
$cxGravarBase = Caixa $pg2 "Gravar este perfil na pasta-base, para os outros computadores" $X0 418 ($WC - 30) $true
$erro2 = Rotulo $pg2 "" $X0 446 $WC 22 (Fonte "Medium" 8.5) $global:Paleta.Acento

# --- página 3: instalação
$pg3 = Pagina
Cabecalho $pg3 "Instalação" "Preparando o computador" "Leva alguns minutos. Se o instalador do MCP abrir uma janela, siga as instruções dela; esta janela continua sozinha depois." | Out-Null
$etapas = @("Arquivos da skill", "MCP do Revit", "Conferência do computador", "ZIP da skill", "Guia e atalhos")
$global:ic = @(); $global:det = @()
$listaEt = New-Object System.Windows.Forms.Panel; $listaEt.Location = Pt 0 170; $listaEt.Size = Tam $LC 260; $listaEt.BackColor = $global:Paleta.Branco; $pg3.Controls.Add($listaEt)
for ($i = 0; $i -lt $etapas.Count; $i++) {
    $yy = $i * 52
    $global:ic += IconeEstado $listaEt $X0 ($yy + 1)
    Rotulo $listaEt $etapas[$i] ($X0 + 36) $yy 300 20 (Fonte "Medium" 9.5) $global:Paleta.Texto | Out-Null
    $global:det += Rotulo $listaEt "Aguardando" ($X0 + 36) ($yy + 20) ($WC - 36) 20 (Fonte "Regular" 8) $global:Paleta.Cinza
}
$log = New-Object System.Windows.Forms.TextBox
$log.Multiline = $true; $log.ReadOnly = $true; $log.ScrollBars = "Vertical"; $log.BorderStyle = "None"; $log.BackColor = $global:Paleta.Claro; $log.ForeColor = $global:Paleta.Texto
$log.Font = New-Object System.Drawing.Font("Consolas", 8.5); $log.Location = Pt $X0 170; $log.Size = Tam $WC 256; $log.Visible = $false; $pg3.Controls.Add($log)
$barraFundo = Bloco $pg3 $X0 448 $WC 4 $global:Paleta.Claro
$global:frac = 0
$barraFundo.Add_Paint({ try { $_.Graphics.FillRectangle((New-Object System.Drawing.SolidBrush($global:Paleta.Acento)), 0, 0, [int]($this.Width * $global:frac), $this.Height) } catch { } })
$lnkLog = Rotulo $pg3 "Ver detalhes" $X0 464 200 20 (Fonte "Medium" 8.5) $global:Paleta.Acento
$lnkLog.Cursor = [System.Windows.Forms.Cursors]::Hand
$lnkLog.Add_Click({ $log.Visible = -not $log.Visible; $listaEt.Visible = -not $log.Visible; $lnkLog.Text = if ($log.Visible) { "Ocultar detalhes" } else { "Ver detalhes" } })
$tmGiro = New-Object System.Windows.Forms.Timer; $tmGiro.Interval = 60
$tmGiro.Add_Tick({ $global:giro = ($global:giro + 24) % 360; foreach ($x in $global:ic) { if ($x.Tag -eq "andamento") { $x.Invalidate() } } })

# --- página 4: concluído
$pg4 = Pagina
$cab4 = Cabecalho $pg4 "Concluído" "Quase pronto" "O computador está preparado. Faltam os passos que só você pode fazer:"
$global:fimTit = @(); $global:fimTxt = @()
for ($i = 0; $i -lt 5; $i++) {
    $yy = 150 + $i * 60
    Rotulo $pg4 ([string]($i + 1)) $X0 ($yy - 2) 24 28 (Fonte "SemiBold" 13) $global:Paleta.Acento | Out-Null
    $global:fimTit += Rotulo $pg4 "" ($X0 + 30) $yy ($WC - 30) 20 (Fonte "Medium" 9.5) $global:Paleta.Texto
    $global:fimTxt += Rotulo $pg4 "" ($X0 + 30) ($yy + 20) ($WC - 30) 34 (Fonte "Regular" 8) $global:Paleta.Cinza
}
$resumo = Bloco $pg4 $X0 454 $WC 58 $global:Paleta.VerdeClaro
$resumoBarra = Bloco $resumo 0 0 3 58 $global:Paleta.Verde
$resumoTxt = Rotulo $resumo "" 18 10 ($WC - 120) 40 (Fonte "Regular" 8.5) $global:Paleta.Texto
$resumoLnk = Rotulo $resumo "Ver avisos" ($WC - 100) 10 86 20 (Fonte "Medium" 8.5) $global:Paleta.Acento
$resumoLnk.TextAlign = "TopRight"; $resumoLnk.Cursor = [System.Windows.Forms.Cursors]::Hand
$resumoLnk.Add_Click({ [System.Windows.Forms.MessageBox]::Show($form, ($global:Avisos -join "`n`n"), "Avisos da instalação", "OK", "Warning") | Out-Null })

$paginas = @($pg0, $pg1, $pg2, $pg3, $pg4)

# ------------------------------------------------------------------
# Navegação
# ------------------------------------------------------------------
$global:pag = 0; $global:perfilNovo = $false; $global:perfil = $null; $global:saida = $null; $global:guiaLocal = $null; $global:falha = $null
function Atualizar-Lateral($atual) {
    for ($i = 0; $i -lt 5; $i++) {
        if ($i -lt $atual) { $global:latNum[$i].ForeColor = Cor "#7DB07D"; $global:latNome[$i].ForeColor = $global:Paleta.ClaroLat; $global:latNome[$i].Font = Fonte "Regular" 9.5 }
        elseif ($i -eq $atual) { $global:latNum[$i].ForeColor = $global:Paleta.Acento; $global:latNome[$i].ForeColor = $global:Paleta.Branco; $global:latNome[$i].Font = Fonte "SemiBold" 9.5 }
        else { $global:latNum[$i].ForeColor = $global:Paleta.CinzaEsc; $global:latNome[$i].ForeColor = $global:Paleta.CinzaLat; $global:latNome[$i].Font = Fonte "Regular" 9.5 }
    }
    if ($atual -eq 2 -and -not $global:perfilNovo) { }
    $marcador.Location = New-Object System.Drawing.Point(0, ($global:latNum[$atual].Top))
}
function Mostrar($i) {
    $global:pag = $i
    for ($k = 0; $k -lt $paginas.Count; $k++) { $paginas[$k].Visible = ($k -eq $i) }
    Atualizar-Lateral $i
    $btVoltar.Visible = ($i -eq 1 -or $i -eq 2); $btPrim.Visible = ($i -ne 3)
    $btAux1.Visible = ($i -eq 4 -and $global:saida); $btAux2.Visible = ($i -eq 4 -and $global:guiaLocal)
    $rodapeTxt.Visible = ($i -eq 3); $rodapeTxt.Text = "Não feche esta janela durante a instalação."
    $fechar.Visible = ($i -ne 3)
    $btPrim.Text = switch ($i) { 0 { "Começar" } 1 { "Continuar" } 2 { "Instalar" } 4 { "Concluir" } }
    $btPrim.Focus() | Out-Null
}
function DoEv { [System.Windows.Forms.Application]::DoEvents() }
function Log([string]$t) { $log.AppendText($t + "`r`n"); DoEv }
function Aviso([string]$t) { $global:Avisos.Add($t); Log ("  [AVISO] " + $t) }
function Etapa($i, [string]$estado, [string]$texto) {
    $global:ic[$i].Tag = $estado; $global:ic[$i].Invalidate()
    if ($texto) { $global:det[$i].Text = $texto }
    $feitas = @($global:ic | Where-Object { $_.Tag -in @("ok", "aviso", "erro") }).Count
    $frac = ($feitas + $(if ($estado -eq "andamento") { 0.4 } else { 0 })) / $global:ic.Count
    $global:frac = [math]::Min([double]1, [double]$frac); $barraFundo.Invalidate()
    if ($texto) { Log ("[" + $etapas[$i] + "] " + $texto) }
    DoEv
}
# roda um .ps1 do pacote num processo oculto e copia a saída para o registro; devolve o código de saída
function Rodar-Oculto([string]$arq, [string]$argsTxt) {
    $cmdTxt = "[Console]::OutputEncoding=[Text.Encoding]::UTF8; & '" + $arq.Replace("'", "''") + "' " + $argsTxt
    $psi = New-Object System.Diagnostics.ProcessStartInfo("powershell.exe", ("-NoProfile -ExecutionPolicy Bypass -Command `"" + $cmdTxt.Replace('"', '\"') + "`""))
    $psi.UseShellExecute = $false; $psi.CreateNoWindow = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.RedirectStandardInput = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $pr = [System.Diagnostics.Process]::Start($psi); $pr.StandardInput.Close()
    $to = $pr.StandardOutput.ReadToEndAsync(); $te = $pr.StandardError.ReadToEndAsync()
    Esperar $pr
    foreach ($ln in ($to.Result -split "`r?`n")) { if ($ln.Trim()) { Log ("  " + $ln.TrimEnd()) } }
    if ($te.Result.Trim()) { Log $te.Result.Trim() }
    return $pr.ExitCode
}
# grava o servidor revit-mcp na configuração do Claude Desktop (todas as pastas, UTF-8 sem BOM)
function Ligar-Claude {
    $cfgMcp = Join-Path $global:pastaVersao "installer\configurar-mcp-claude.ps1"
    if (-not (Test-Path $cfgMcp)) { return 99 }
    return (Rodar-Oculto $cfgMcp ("-RevitAno " + $RevitAno))
}
function Esperar($proc) { while (-not $proc.HasExited) { DoEv; Start-Sleep -Milliseconds 80 } }

$btVoltar.Add_Click({ if ($global:pag -eq 2) { Mostrar 1 } else { Mostrar 0 } })
$btAux1.Add_Click({ if ($global:saida) { Start-Process explorer.exe "/select,`"$($global:saida)`"" } })
$btAux2.Add_Click({ if ($global:guiaLocal) { Start-Process $global:guiaLocal } })
$btPrim.Add_Click({
    try {
    switch ($global:pag) {
        0 { Mostrar 1 }
        1 {
            $erro1.Text = ""
            $b = $tbBase.Text.Trim().TrimEnd('\')
            if (-not $b) { $erro1.Text = "Informe a pasta-base."; return }
            $unc = $tbUnc.Text.Trim()
            $q = Split-Path $b -Qualifier -ErrorAction SilentlyContinue
            if ($q -and -not (Test-Path "$q\") -and $unc) { $erro1.Text = "Mapeando $q..."; DoEv; cmd /c "net use $q `"$unc`" /persistent:yes" | Out-Null; $erro1.Text = "" }
            if (-not (Test-Path $b)) { $erro1.Text = "Não encontrei esta pasta. Confira o caminho e a conexão com o servidor."; return }
            $global:PastaBase = $b; $global:Unc = $unc
            if ($tbRepo) { $global:Repo = $tbRepo.Text.Trim() }
            $global:perfil = Ler-Json $ArqPerfil
            if (-not $global:perfil) { $global:perfil = Ler-Json (Join-Path $b "_config\escritorio.json") }
            $global:perfilNovo = -not $global:perfil
            if ($global:perfilNovo) {
                $m = Perfil-Modelo
                $tbNome.Text = Limpo $m.nome; $tbPrefixo.Text = Limpo $m.prefixo; $tbContato.Text = Limpo $m.endereco_contato
                $tbResp.Text = Limpo $m.responsavel_padrao.autor; $tbCau.Text = Limpo $m.responsavel_padrao.cau
                $tbExec.Text = Limpo $m.executor_padrao.nome; $tbCrea.Text = Limpo $m.executor_padrao.crea
                $global:perfil = $m; Mostrar 2
            } else { Iniciar-Instalacao }
        }
        2 {
            if (-not $tbNome.Text.Trim()) { $erro2.Text = "Informe pelo menos o nome do escritório."; return }
            $p = $global:perfil
            $p.nome = $tbNome.Text.Trim(); $p.prefixo = $tbPrefixo.Text.Trim(); $p.endereco_contato = $tbContato.Text.Trim()
            $p.responsavel_padrao.autor = $tbResp.Text.Trim(); $p.responsavel_padrao.cau = $tbCau.Text.Trim()
            $p.executor_padrao.nome = $tbExec.Text.Trim(); $p.executor_padrao.crea = $tbCrea.Text.Trim()
            Iniciar-Instalacao
        }
        4 { $form.Close() }
    }
    } catch { [System.Windows.Forms.MessageBox]::Show($form, ("Erro inesperado: " + $_.Exception.Message), "Skill Executivo Revit", "OK", "Error") | Out-Null }
})
$form.Add_FormClosing({ if ($global:pag -eq 3 -and -not $global:terminou) { $_.Cancel = $true } })

function Iniciar-Instalacao {
    Mostrar 3; $tmGiro.Start()
    $t = New-Object System.Windows.Forms.Timer; $t.Interval = 400
    $t.Add_Tick({ $this.Stop(); try { Executar-Instalacao } catch { $global:falha = $_.Exception.Message; Log ("[ERRO] " + $global:falha) }
                  $tmGiro.Stop(); $global:terminou = $true; Preencher-Final; Mostrar 4 })
    $t.Start()
}

# ------------------------------------------------------------------
# Instalação
# ------------------------------------------------------------------
function Executar-Instalacao {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $hdr = @{ "User-Agent" = "SkillExecutivoRevit-Instalador"; "Accept" = "application/vnd.github+json" }
    $PastaBase = $global:PastaBase; $Repo = $global:Repo
    Log ("Instalador v$VersaoInstalador  ·  " + (Get-Date).ToString("dd/MM/yyyy HH:mm") + "  ·  pasta-base: " + $PastaBase)

    # 1. arquivos
    Etapa 0 "andamento" "Preparando os arquivos..."
    if (-not $global:pastaVersao) {
        if (-not $Repo) { Etapa 0 "erro" "Este instalador não traz os arquivos da skill e não há repositório informado."; throw "Sem arquivos da skill." }
        try { $rel = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest" -Headers $hdr -TimeoutSec 30; $global:versao = $rel.tag_name; $url = $rel.zipball_url }
        catch { $info = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo" -Headers $hdr -TimeoutSec 30; $ramo = $info.default_branch
                $commit = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/commits/$ramo" -Headers $hdr -TimeoutSec 30
                $global:versao = $ramo + "-" + $commit.sha.Substring(0, 7); $url = "https://api.github.com/repos/$Repo/zipball/$ramo" }
        Etapa 0 "andamento" ("Baixando a versão " + $global:versao + "...")
        $zip = Join-Path $env:TEMP ("skill-executivo-" + [guid]::NewGuid().ToString("N") + ".zip")
        Invoke-WebRequest -Uri $url -Headers $hdr -OutFile $zip -TimeoutSec 300 -UseBasicParsing
        $tmp = Join-Path $env:TEMP ("skill-executivo-" + [guid]::NewGuid().ToString("N")); Expand-Archive -Path $zip -DestinationPath $tmp -Force
        $global:pastaVersao = Join-Path $Cache ($global:versao -replace '[^\w\.-]', '_')
        if (Test-Path $global:pastaVersao) { Remove-Item $global:pastaVersao -Recurse -Force }
        Move-Item (Get-ChildItem $tmp -Directory | Select-Object -First 1).FullName $global:pastaVersao
        Remove-Item $zip -Force; Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
    $estado0 = "ok"; $txt0 = "Versão " + $global:versao + " pronta."
    if ($PacoteEmbutido -and $Repo) {
        try { $rel = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest" -Headers $hdr -TimeoutSec 15
              if ($rel.tag_name -and ($rel.tag_name.TrimStart('v') -ne $global:versao.TrimStart('v'))) { Aviso ("Há outra versão no GitHub (" + $rel.tag_name + "); este instalador traz a " + $global:versao + ". Peça o instalador novo ao responsável."); $estado0 = "aviso"; $txt0 += " Há outra versão no GitHub." } } catch { }
    }
    Etapa 0 $estado0 $txt0

    # 2. MCP
    if (-not $cxMcp.Tag) {
        if ($mcpOk) { Etapa 1 "andamento" "Conferindo a ligação com o Claude Desktop..."; [void](Ligar-Claude); Etapa 1 "ok" "Já instalado neste computador." }
        elseif ($temPlugin -and (Tem-ClaudeDesktop)) {
            Etapa 1 "andamento" "Ligando o MCP ao Claude Desktop..."; [void](Ligar-Claude)
            if (Tem-Claude) { Etapa 1 "ok" "Ligado ao Claude Desktop (reinicie o Claude Desktop)." } else { Aviso "Não foi possível ligar o MCP ao Claude Desktop. Veja 'Ver detalhes'."; Etapa 1 "aviso" "Suplemento instalado; falta a ligação com o Claude Desktop." } }
        else { Aviso "MCP do Revit não instalado: a skill não funciona sem ele. Rode o instalador de novo marcando a instalação do MCP."; Etapa 1 "aviso" "Não instalado (opção desmarcada)." }
    } else {
        if (-not (Tem-ClaudeDesktop)) { Aviso "Claude Desktop não encontrado: o MCP fica no Revit, mas não aparece no Claude. Instale o Claude Desktop (claude.ai/download), abra e entre na conta uma vez, feche e rode este instalador de novo." }
        Etapa 1 "andamento" "Baixando o instalador oficial do MCP..."
        $instMcp = Join-Path $env:TEMP "instalar-mcp-revit.ps1"
        try {
            Invoke-WebRequest -Uri "https://raw.githubusercontent.com/$McpRepo/main/scripts/install.ps1" -OutFile $instMcp -UseBasicParsing -TimeoutSec 120
            $argMcp = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$instMcp`"", "-RevitVersion", $RevitAno)
            if ($McpTag -and $McpTag -ne "latest") { $argMcp += @("-Tag", $McpTag) }
            $argMcp += "-Force"
            Etapa 1 "andamento" "Instalando: siga a janela do instalador do MCP que abriu."
            $proc = Start-Process powershell.exe -ArgumentList $argMcp -PassThru
            Esperar $proc
            if (Tem-Plugin) { Etapa 1 "andamento" "Ligando o MCP ao Claude Desktop..."; [void](Ligar-Claude) }
            if ((Tem-Plugin) -and (Tem-Claude)) { Etapa 1 "ok" "Instalado e ligado ao Claude Desktop (reinicie o Claude Desktop)." }
            elseif (Tem-Plugin) { Aviso "O suplemento do MCP está no Revit, mas o Claude Desktop ainda não tem o servidor do Revit. Com o Claude Desktop instalado e aberto uma vez, feche-o e rode este instalador de novo com 'Instalar o MCP do Revit' marcado."; Etapa 1 "aviso" "Suplemento instalado; falta a configuração no Claude Desktop." }
            else { Aviso ("O instalador do MCP não terminou a instalação (código " + $proc.ExitCode + ")."); Etapa 1 "erro" "Não foi possível instalar. Veja os avisos." }
        } catch { Aviso ("Não foi possível baixar o instalador do MCP: " + $_.Exception.Message); Etapa 1 "erro" "Falha ao baixar o instalador do MCP." }
        finally { Remove-Item $instMcp -Force -ErrorAction SilentlyContinue }
    }

    # 3. conferência (processo separado, saída capturada)
    Etapa 2 "andamento" "Conferindo pasta-base, template e Revit..."
    $puxar = Join-Path $global:pastaVersao "installer\puxar-pasta-base.ps1"
    $estado2 = "ok"; $txt2 = "Pasta-base, template e Revit conferidos."
    if (Test-Path $puxar) {
        $cmdTxt = "[Console]::OutputEncoding=[Text.Encoding]::UTF8; & '" + $puxar.Replace("'", "''") + "' -PastaBase '" + $PastaBase.Replace("'", "''") + "'"
        if ($global:Unc) { $cmdTxt += " -Unc '" + $global:Unc.Replace("'", "''") + "'" }
        $psi = New-Object System.Diagnostics.ProcessStartInfo("powershell.exe", ("-NoProfile -ExecutionPolicy Bypass -Command `"" + $cmdTxt.Replace('"', '\"') + "`""))
        $psi.UseShellExecute = $false; $psi.CreateNoWindow = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.RedirectStandardInput = $true
        $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $proc = [System.Diagnostics.Process]::Start($psi); $proc.StandardInput.Close()
        $tOut = $proc.StandardOutput.ReadToEndAsync(); $tErr = $proc.StandardError.ReadToEndAsync()
        Esperar $proc
        foreach ($ln in ($tOut.Result -split "`r?`n")) { if ($ln.Trim()) { Log ("  " + $ln.TrimEnd()); if ($ln -match '\[AVISO\]\s*(.+)$') { $global:Avisos.Add($Matches[1].Trim()); $estado2 = "aviso" } } }
        if ($tErr.Result.Trim()) { Log $tErr.Result.Trim() }
        if ($proc.ExitCode -ne 0) { $estado2 = "erro"; $txt2 = "A conferência encontrou um erro. Veja os detalhes."; $global:Avisos.Add("A conferência da pasta-base terminou com erro. Abra 'Ver detalhes' ou rode PUXAR_PASTA_BASE.cmd.") }
        elseif ($estado2 -eq "aviso") { $txt2 = "Conferido, com avisos." }
    } else { $estado2 = "aviso"; $txt2 = "Script de conferência não encontrado no pacote."; $global:Avisos.Add($txt2) }
    $porta = 8080; $arqPorta = Join-Path $pastaPlugin "mcp-port.txt"
    if (Test-Path $arqPorta) { $pt = (Get-Content $arqPorta -Raw).Trim(); if ($pt -match '^\d+$') { $porta = [int]$pt } }
    $resp = $false
    try { $tcp = New-Object System.Net.Sockets.TcpClient; $ar = $tcp.BeginConnect("127.0.0.1", $porta, $null, $null); if ($ar.AsyncWaitHandle.WaitOne(1500) -and $tcp.Connected) { $resp = $true }; $tcp.Close() } catch { }
    Log ("  Servidor do MCP na porta " + $porta + ": " + $(if ($resp) { "respondendo" } else { "desligado (normal com o Revit fechado)" }))
    Etapa 2 $estado2 $txt2

    # 4. perfil e ZIP
    Etapa 3 "andamento" "Gerando o ZIP da skill..."
    $cfg = Ler-Json (Join-Path $Raiz "config.json")
    $perfil = $global:perfil
    $perfil.caminhos.pasta_base = $PastaBase
    if ($cfg -and $cfg.template) { $perfil.caminhos.template_master = $cfg.template }
    Gravar-Json $perfil $ArqPerfil
    $perfilBase = Join-Path $PastaBase "_config\escritorio.json"
    if ($global:perfilNovo -and $cxGravarBase.Tag -and -not (Test-Path $perfilBase)) {
        try { New-Item -ItemType Directory -Path (Split-Path $perfilBase) -Force | Out-Null; Gravar-Json $perfil $perfilBase; Log "  Perfil gravado na pasta-base." }
        catch { Aviso "Não foi possível gravar o perfil na pasta-base (sem permissão de escrita)." }
    }
    $nomeSkill = "executivo-revit"
    $fm = Get-Content (Join-Path $global:pastaVersao "skill\SKILL.md") -TotalCount 6 -Encoding UTF8 | Where-Object { $_ -match '^name:\s*(\S+)' } | Select-Object -First 1
    if ($fm -match '^name:\s*(\S+)') { $nomeSkill = $Matches[1] }
    $emb = Join-Path $env:TEMP ("skill-zip-" + [guid]::NewGuid().ToString("N")); New-Item -ItemType Directory -Path $emb | Out-Null
    Copy-Item (Join-Path $global:pastaVersao "skill") (Join-Path $emb $nomeSkill) -Recurse
    Gravar-Json $perfil (Join-Path $emb "$nomeSkill\config\escritorio.json")
    $global:saida = Join-Path ([Environment]::GetFolderPath("Desktop")) ("$nomeSkill-" + ($global:versao -replace '[^\w\.-]', '_') + ".zip")
    if (Test-Path $global:saida) { Remove-Item $global:saida -Force }
    # ZIP com "/" nos caminhos: o Compress-Archive do Windows PowerShell 5.1 grava "\" e o Claude recusa o envio
    # ("Zip file contains path with invalid characters")
    Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem
    $raizZip = Join-Path $emb $nomeSkill
    $zipArq = [System.IO.Compression.ZipFile]::Open($global:saida, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($arqZ in (Get-ChildItem $raizZip -Recurse -File)) {
            $rel = $nomeSkill + "/" + $arqZ.FullName.Substring($raizZip.Length).TrimStart('\', '/').Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zipArq, $arqZ.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    } finally { $zipArq.Dispose() }
    Remove-Item $emb -Recurse -Force
    Etapa 3 "ok" ("Na Área de Trabalho: " + (Split-Path $global:saida -Leaf))

    # 5. guia, atalhos e registro
    Etapa 4 "andamento" "Instalando o guia e os atalhos..."
    $guia = Get-ChildItem $global:pastaVersao -Recurse -Filter "Guia_Skill_Executivo_Revit.pdf" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($guia) { $global:guiaLocal = Join-Path $Local "Guia_Skill_Executivo_Revit.pdf"; Copy-Item $guia.FullName $global:guiaLocal -Force }
    $exeLocal = $null
    try { $exeAtual = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
          if ($exeAtual -notmatch '(?i)\\powershell(_ise)?\.exe$|\\pwsh\.exe$') { $exeLocal = Join-Path $Local "InstaladorSkillExecutivoRevit.exe"; if ($exeAtual -ne $exeLocal) { Copy-Item $exeAtual $exeLocal -Force } } } catch { }
    $estado4 = "ok"; $txt4 = "Guia na Área de Trabalho; atalhos no menu Iniciar › Skill Executivo Revit."
    try {
        $sh = New-Object -ComObject WScript.Shell
        $menu = Join-Path ([Environment]::GetFolderPath("Programs")) "Skill Executivo Revit"; New-Item -ItemType Directory -Path $menu -Force | Out-Null
        if ($global:guiaLocal) { foreach ($dest in @((Join-Path ([Environment]::GetFolderPath("Desktop")) "Skill Executivo Revit - Guia.lnk"), (Join-Path $menu "Guia de uso.lnk"))) { $lk = $sh.CreateShortcut($dest); $lk.TargetPath = $global:guiaLocal; $lk.Save() } }
        if ($exeLocal) { $lk = $sh.CreateShortcut((Join-Path $menu "Atualizar a skill.lnk")); $lk.TargetPath = $exeLocal; $lk.WorkingDirectory = $Local; $lk.IconLocation = "$exeLocal,0"; $lk.Save() }
    } catch { Aviso ("Não foi possível criar os atalhos: " + $_.Exception.Message); $estado4 = "aviso"; $txt4 = "Guia instalado; atalhos não criados." }
    Gravar-Json ([ordered]@{ versao = $global:versao; instaladoEm = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss"); repo = $Repo; zip = $global:saida; instalador = $VersaoInstalador; pastaBase = $PastaBase }) $ArqVersao
    Gravar-Json ([ordered]@{ repo = $Repo; mcpRepo = $McpRepo; revitAno = $RevitAno }) $ArqInst
    Etapa 4 $estado4 $txt4
    try { Set-Clipboard -Value $global:saida } catch { try { [System.Windows.Forms.Clipboard]::SetText($global:saida) } catch { } }
    try { Start-Process explorer.exe "/select,`"$($global:saida)`"" } catch { }
}

function Preencher-Final {
    $base = $global:PastaBase
    $tit = @("Ative a execução de código no Claude", "Envie a skill", "Reinicie o Claude Desktop", "Ligue o servidor no Revit", "Comece uma conversa com a pasta-base")
    $txt = @("Configurações › Capacidades › Execução de código e criação de arquivos.",
             "Personalizar › Skills › + › Criar skill › Enviar uma skill. O ZIP já está selecionado no Explorer e o caminho foi copiado.",
             "Feche e abra o app para ele carregar o MCP do Revit.",
             "Revit $RevitAno › Suplementos › Revit MCP Switch (o indicador fica verde). Faça isso sempre que abrir o Revit.",
             "Nova conversa › Adicionar pasta › $base. Depois peça: 'Vamos iniciar o executivo da Família Silva'.")
    for ($i = 0; $i -lt 5; $i++) { $global:fimTit[$i].Text = $tit[$i]; $global:fimTxt[$i].Text = $txt[$i] }
    $n = $global:Avisos.Count
    if ($global:falha) {
        $cab4[0].Text = "Instalação interrompida"; $cab4[1].Text = "Não foi possível concluir: " + $global:falha
        foreach ($l in $global:fimTit + $global:fimTxt) { $l.Text = "" }
        $resumo.BackColor = Cor "#FBEDED"; $resumoBarra.BackColor = $global:Paleta.Acento
        $resumoTxt.Text = "Abra 'Ver avisos', corrija o problema e rode o instalador de novo."
        $global:Avisos.Add($global:falha)
    } elseif ($n -gt 0) {
        $resumo.BackColor = $global:Paleta.AmbarClaro; $resumoBarra.BackColor = $global:Paleta.Ambar
        $resumoTxt.Text = "Skill " + $global:versao + " preparada, com " + $n + $(if ($n -eq 1) { " aviso" } else { " avisos" }) + ".`n" + $global:Avisos[0]
    } else {
        $resumoTxt.Text = "Tudo certo: skill " + $global:versao + " preparada e MCP do Revit " + $(if (Tem-Plugin) { "instalado." } else { "pendente." })
    }
    $resumoLnk.Visible = ($global:Avisos.Count -gt 0)
}

# ------------------------------------------------------------------
# Prévia (imagens das telas, para revisar o desenho) ou execução normal
# ------------------------------------------------------------------
if ($Previa) {
    New-Item -ItemType Directory -Path $Previa -Force | Out-Null
    $form.StartPosition = "Manual"; $form.Location = New-Object System.Drawing.Point(-4000, -4000); $form.Show(); DoEv
    function Foto($nome) { DoEv; $bmp = New-Object System.Drawing.Bitmap($form.Width, $form.Height); $form.DrawToBitmap($bmp, (New-Object System.Drawing.Rectangle(0, 0, $form.Width, $form.Height))); $bmp.Save((Join-Path $Previa "$nome.png")); $bmp.Dispose() }
    Mostrar 0; Foto "0-boas-vindas"
    Mostrar 1; if (-not $tbBase.Text) { $tbBase.Text = "P:\Projetos\SkillExecutivo" }; Foto "1-configuracao"
    $global:perfilNovo = $true; $tbNome.Text = "Escritório Exemplo"; $tbPrefixo.Text = "EX-"; $tbContato.Text = "Rua Exemplo, 100 · (00) 0000-0000 · @escritorio"; $tbResp.Text = "Arq. Fulano de Tal"; $tbCau.Text = "A000000-0"; Mostrar 2; Foto "2-perfil"
    Mostrar 3; Etapa 0 "ok" "Versão 0.5.0 pronta."; Etapa 1 "ok" "Já instalado neste computador."; $global:giro = 200; Etapa 2 "andamento" "Conferindo pasta-base, template e Revit..."; Foto "3-instalacao"
    $global:PastaBase = "P:\Projetos\SkillExecutivo"; $global:versao = "0.5.0"; $global:saida = "C:\Users\exemplo\Desktop\executivo-revit-0.5.0.zip"; $global:guiaLocal = "x"
    $global:Avisos.Add("Sem permissão de escrita na pasta-base (somente leitura)."); Preencher-Final; Mostrar 4; Foto "4-concluido"
    $form.Close(); return
}
Mostrar 0
[void]$form.ShowDialog()
