// SKILL EXECUTIVO REVIT — Fase 11: emissão (PDF por caderno; opcional PDF por folha e DWG)
// send_code_to_revit · transactionMode "none"
// parameters[0] = caderno: "ARQ", "PROPOSTA", "DET" ou "TODOS" (padrão "TODOS")
// parameters[1] = pasta de destino (vazio = "<pasta do projeto>\EMISSÕES\<aaaa-mm-dd> <ETAPA>")
// parameters[2] = prefixo do número das folhas (teste: "SKILL "; vazio = todas)
// parameters[3] = formatos separados por ";": "pdf" (caderno único), "pdf_folhas" (um PDF por folha), "dwg" — padrão "pdf"
//
// Só roda depois da fase 10 sem bloqueios e com o ok do arquiteto. Não altera o modelo.
// PDF: tamanho do papel de cada folha, cor, vetorial, 100%. Nome: "<NOME DO PROJETO> - <CADERNO> - <aaaa-mm-dd>.pdf".
// Grava "EMISSAO.txt" na pasta com data, etapa, cadernos e folhas emitidas (registro da emissão).
// Exportar muitas folhas A1 pode passar de 80 s: espere e confira os arquivos antes de repetir.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var qual = P(0) == "" ? "TODOS" : P(0).ToUpper(); var prefixo = parameters.Length > 2 ? ((parameters[2] as string) ?? "") : "";
var formatos = (P(3) == "" ? "pdf" : P(3).ToLower()).Split(';').Select(s => s.Trim()).ToList();
Func<string, string> Limpo = s => string.Join("", (s ?? "").Select(ch => System.IO.Path.GetInvalidFileNameChars().Contains(ch) || ch == '/' ? '-' : ch)).Trim();
var pi = document.ProjectInformation;
var etapa = (pi.LookupParameter("ETAPA DO PROJETO")?.AsString() ?? "").Trim();
var nomeProj = (pi.LookupParameter("Nome do cliente")?.AsString() ?? "").Trim(); if (nomeProj == "") nomeProj = document.Title;
var hoje = DateTime.Now.ToString("yyyy-MM-dd");
var pasta = P(1);
if (pasta == "") {
    if (string.IsNullOrEmpty(document.PathName)) return "ERRO: projeto nunca foi salvo — informe a pasta de destino.";
    pasta = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(document.PathName), "EMISSÕES", hoje + (etapa != "" ? " " + Limpo(etapa) : ""));
}
System.IO.Directory.CreateDirectory(pasta);

// folhas por caderno, em ordem de número
var cad = new Dictionary<string, List<ViewSheet>>();
foreach (var s in new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(x => !x.IsPlaceholder && x.SheetNumber.StartsWith(prefixo))) {
    var tb = new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
    var fam = tb == null ? "" : tb.Symbol.FamilyName.ToUpper();
    var c = fam.Contains("PROPOSTA") ? "PROPOSTA" : fam.Contains("DETALHAMENTO") || fam.Contains("INTERIORES") ? "DET" : fam.Contains("EXECUTIVO") ? "ARQ" : "";
    if (c == "" || (qual != "TODOS" && qual != c)) continue;
    if (!cad.ContainsKey(c)) cad[c] = new List<ViewSheet>(); cad[c].Add(s);
}
if (cad.Count == 0) return "Nenhuma folha encontrada para o caderno " + qual + (prefixo != "" ? " com prefixo \"" + prefixo + "\"" : "") + ".";
Func<ViewSheet, int> Ordem = s => { int a; var n = s.SheetNumber.Substring(prefixo.Length).Split('/')[0].Trim(); return int.TryParse(n, out a) ? a : 999; };

var log = new System.Text.StringBuilder(); var registro = new System.Text.StringBuilder();
registro.AppendLine("EMISSÃO — " + nomeProj); registro.AppendLine("Data: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " | Etapa: " + (etapa == "" ? "(não informada)" : etapa)); registro.AppendLine("Arquivo: " + document.PathName);
foreach (var kv in cad) {
    var folhas = kv.Value.OrderBy(Ordem).ToList(); var ids = folhas.Select(f => f.Id).ToList();
    registro.AppendLine(""); registro.AppendLine("Caderno " + kv.Key + " — " + folhas.Count + " folhas:");
    foreach (var f in folhas) registro.AppendLine("  " + f.SheetNumber + "  " + f.Name + (f.LookupParameter("CONTEÚDO DA PRANCHA") != null ? "  — " + f.LookupParameter("CONTEÚDO DA PRANCHA").AsString() : ""));
    if (formatos.Contains("pdf")) {
        var op = new PDFExportOptions { Combine = true, FileName = Limpo(nomeProj + " - " + kv.Key + " - " + hoje), PaperFormat = ExportPaperFormat.Default, ColorDepth = ColorDepthType.Color, ZoomType = ZoomType.Zoom, ZoomPercentage = 100, HideCropBoundaries = true, HideScopeBoxes = true, HideReferencePlane = true, HideUnreferencedViewTags = true, MaskCoincidentLines = true };
        bool okPdf = document.Export(pasta, ids, op);
        var arq = System.IO.Path.Combine(pasta, op.FileName + ".pdf");
        log.AppendLine("PDF " + kv.Key + ": " + (okPdf && System.IO.File.Exists(arq) ? System.IO.Path.GetFileName(arq) + " (" + (new System.IO.FileInfo(arq).Length / 1024) + " KB, " + folhas.Count + " folhas)" : "FALHOU"));
    }
    if (formatos.Contains("pdf_folhas")) {
        int n = 0;
        foreach (var f in folhas) {
            var op = new PDFExportOptions { Combine = true, FileName = Limpo(kv.Key + " " + f.SheetNumber.Substring(prefixo.Length) + " - " + f.Name), PaperFormat = ExportPaperFormat.Default, ColorDepth = ColorDepthType.Color, ZoomType = ZoomType.Zoom, ZoomPercentage = 100, HideCropBoundaries = true, HideScopeBoxes = true, HideReferencePlane = true, HideUnreferencedViewTags = true };
            if (document.Export(pasta, new List<ElementId> { f.Id }, op)) n++;
        }
        log.AppendLine("PDF por folha " + kv.Key + ": " + n + " de " + folhas.Count);
    }
    if (formatos.Contains("dwg")) {
        var sub = System.IO.Path.Combine(pasta, "DWG " + kv.Key); System.IO.Directory.CreateDirectory(sub);
        var od = new DWGExportOptions { MergedViews = true };
        bool okDwg = document.Export(sub, Limpo(nomeProj + " - " + kv.Key), ids, od);
        log.AppendLine("DWG " + kv.Key + ": " + (okDwg ? System.IO.Directory.GetFiles(sub, "*.dwg").Length + " arquivo(s) em " + sub : "FALHOU"));
    }
}
System.IO.File.WriteAllText(System.IO.Path.Combine(pasta, "EMISSAO.txt"), registro.ToString(), System.Text.Encoding.UTF8);
log.AppendLine("Pasta: " + pasta); log.AppendLine("Registro: EMISSAO.txt");
return log.ToString();
