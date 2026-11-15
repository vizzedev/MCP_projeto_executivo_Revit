// SKILL EXECUTIVO REVIT — Fase 0: pré-voo (somente leitura)
// send_code_to_revit · transactionMode "none" · sem parâmetros
var sb = new System.Text.StringBuilder();
var app = document.Application;
var bloqueios = new List<string>(); var avisos = new List<string>();

sb.AppendLine("REVIT: " + app.VersionName + " (" + app.VersionBuild + ")");

// Arquivos abertos
sb.AppendLine("ARQUIVOS ABERTOS:");
foreach (Document d in app.Documents) {
    if (d.IsLinked) continue;
    var caminho = string.IsNullOrEmpty(d.PathName) ? "(sem arquivo)" : d.PathName;
    var rede = caminho.StartsWith(@"\\") || (caminho.Length > 2 && caminho[1] == ':' && Win.GetDriveType(caminho.Substring(0, 3)) == 4);
    sb.AppendLine("  - " + d.Title + (d.IsFamilyDocument ? " [família]" : "") + " | " + caminho + (rede ? " [REDE]" : "") + (d.IsModified ? " | ALTERAÇÕES NÃO SALVAS" : "") + (d.Title == document.Title ? " | ATIVO" : ""));
    if (string.IsNullOrEmpty(d.PathName)) avisos.Add("Há um projeto sem arquivo aberto (" + d.Title + ") — feche sem salvar se for resto de teste.");
}

// Configuração local criada pelo instalador
var cfgPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkillExecutivoRevit", "config.json");
string template = null;
if (System.IO.File.Exists(cfgPath)) {
    var cfg = System.IO.File.ReadAllText(cfgPath);
    sb.AppendLine("CONFIG LOCAL: " + cfgPath + " (" + System.IO.File.GetLastWriteTime(cfgPath).ToString("dd/MM/yyyy HH:mm") + ")");
    var m = System.Text.RegularExpressions.Regex.Match(cfg, "\"pastaBase\"\\s*:\\s*\"([^\"]*)\"");
    if (m.Success) {
        var pastaBase = m.Groups[1].Value.Replace("\\\\", "\\");
        sb.AppendLine("  pasta-base: " + pastaBase + (System.IO.Directory.Exists(pastaBase) ? " (acessível)" : " (INACESSÍVEL)"));
        if (!System.IO.Directory.Exists(pastaBase)) bloqueios.Add("Pasta-base inacessível: " + pastaBase);
        var t = System.IO.Path.Combine(pastaBase, "01_TEMPLATE", "TEMPLATE_MASTER_v1.rte");
        if (System.IO.File.Exists(t)) template = t;
    }
} else {
    avisos.Add("config.json local não encontrado — rode PUXAR_PASTA_BASE.cmd (ou o instalador) neste computador.");
}

// Template master no servidor e cópia local
if (template != null) {
    var fi = new System.IO.FileInfo(template);
    sb.AppendLine("TEMPLATE MASTER: " + template + " (" + (fi.Length / 1048576) + " MB, " + fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm") + ")");
    var local = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkillExecutivoRevit", "templates", fi.Name);
    if (System.IO.File.Exists(local)) {
        var li = new System.IO.FileInfo(local);
        var atual = li.LastWriteTime >= fi.LastWriteTime && li.Length == fi.Length;
        sb.AppendLine("CÓPIA LOCAL: " + local + (atual ? " (atualizada)" : " (DESATUALIZADA — será recopiada na fase 1)"));
    } else sb.AppendLine("CÓPIA LOCAL: não existe — será criada na fase 1");
} else {
    bloqueios.Add("Template master não encontrado na pasta-base (01_TEMPLATE\\TEMPLATE_MASTER_v1.rte).");
}

// Avisos do documento ativo
sb.AppendLine("DOCUMENTO ATIVO: " + document.Title + " | avisos do Revit: " + document.GetWarnings().Count);

sb.AppendLine(bloqueios.Count == 0 ? "RESULTADO: OK para seguir" : "RESULTADO: BLOQUEADO");
foreach (var b in bloqueios) sb.AppendLine("  BLOQUEIO: " + b);
foreach (var a in avisos) sb.AppendLine("  AVISO: " + a);
return sb.ToString();
}
// Tipo de unidade do Windows (4 = rede). Classe auxiliar: o MCP compila o código acima dentro de Execute().
class Win { [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] public static extern uint GetDriveType(string raiz);
