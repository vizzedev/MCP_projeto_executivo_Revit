// SKILL EXECUTIVO REVIT — Fase 1: criar o projeto a partir da cópia local do template
// send_code_to_revit · transactionMode "none"
// parameters[0] = template LOCAL (.rte) — use 01_preparar_template_local.cs antes
// parameters[1] = caminho completo do .rvt de destino (a pasta é criada se não existir)
// Nunca sobrescreve: se o destino existir, para com erro.
var tpl = (parameters != null && parameters.Length > 0) ? parameters[0] as string : null;
var dest = (parameters != null && parameters.Length > 1) ? parameters[1] as string : null;
if (string.IsNullOrWhiteSpace(tpl) || !System.IO.File.Exists(tpl)) return "ERRO: template local não encontrado: " + tpl;
if (string.IsNullOrWhiteSpace(dest) || !dest.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase)) return "ERRO: destino inválido (precisa terminar em .rvt): " + dest;
if (System.IO.File.Exists(dest)) return "ERRO: já existe um arquivo em " + dest + " — escolha outro nome. Nada foi feito.";
if (tpl.StartsWith(@"\\") ) return "ERRO: use a cópia LOCAL do template (rede é lenta e trava o Revit).";
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));
var app = document.Application;
var novo = app.NewProjectDocument(tpl);
var o = new SaveAsOptions(); o.OverwriteExistingFile = false;
novo.SaveAs(dest, o);
novo.Close(false);
new UIApplication(app).OpenAndActivateDocument(dest);
return "OK: projeto criado e aberto → " + dest;
