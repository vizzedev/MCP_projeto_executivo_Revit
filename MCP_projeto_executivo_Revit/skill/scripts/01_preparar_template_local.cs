// SKILL EXECUTIVO REVIT — Fase 1: cópia local do template master
// send_code_to_revit · transactionMode "none"
// parameters[0] = caminho do template master no servidor
// Seguro para repetir: só copia se a cópia local não existir ou estiver desatualizada.
// Copia para um arquivo temporário e renomeia no fim (nunca deixa cópia pela metade).
// Arquivo de 200 MB pela rede pode levar 1–3 min: se o MCP passar de 80 s, aguarde e rode de novo.
var origem = (parameters != null && parameters.Length > 0) ? parameters[0] as string : null;
if (string.IsNullOrWhiteSpace(origem) || !System.IO.File.Exists(origem)) return "ERRO: template não encontrado: " + origem;
var pasta = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkillExecutivoRevit", "templates");
System.IO.Directory.CreateDirectory(pasta);
var destino = System.IO.Path.Combine(pasta, System.IO.Path.GetFileName(origem));
var fo = new System.IO.FileInfo(origem);
if (System.IO.File.Exists(destino)) {
    var fd = new System.IO.FileInfo(destino);
    if (fd.Length == fo.Length && fd.LastWriteTime >= fo.LastWriteTime) return "OK: cópia local já atualizada → " + destino;
}
var tmp = destino + ".copiando";
var ini = DateTime.Now;
System.IO.File.Copy(origem, tmp, true);
if (System.IO.File.Exists(destino)) System.IO.File.Delete(destino);
System.IO.File.Move(tmp, destino);
System.IO.File.SetLastWriteTime(destino, fo.LastWriteTime);
return "OK: copiado em " + (DateTime.Now - ini).TotalSeconds.ToString("0") + " s → " + destino + " (" + (fo.Length / 1048576) + " MB)";
