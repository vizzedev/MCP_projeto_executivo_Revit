// SKILL EXECUTIVO REVIT — Fase 10: conferência final antes de emitir (somente leitura)
// send_code_to_revit · transactionMode "none"
// parameters[0] = (opcional) prefixo do número das folhas a conferir (ex.: "SKILL " em teste; vazio = todas)
//
// Cadernos pelo carimbo: PROPOSTA ("Proposta"), ARQ ("Executivo Arquitetônico"), DET ("Detalhamento").
// BLOQUEIA: folha sem nenhuma vista/tabela; numeração "i/N" quebrada (falta número, N errado, repetida) no caderno;
//           campos obrigatórios do carimbo vazios (cliente, endereço, autor, CAU, nome do projeto, área do lote).
// CORRIGIR: tabela na folha sem itens; vista fora da moldura ou sobre o carimbo; vistas sobrepostas (> 15 mm);
//           CONTEÚDO DA PRANCHA vazio (quando o parâmetro existe).
// ATENÇÃO:  vistas do caderno fora de folha (cortes, fachadas, plantas executivas, vistas de ambiente, perspectivas);
//           RRT/ART/executor vazios; avisos do Revit.
var prefixo = parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "";
Func<double, double> ToMM = v => v * 304.8;
var bloqueia = new List<string>(); var corrigir = new List<string>(); var atencao = new List<string>(); var ok = new List<string>();

// --- carimbo (Informações do projeto)
var pi = document.ProjectInformation;
Func<string, string> Valor = n => { var p = pi.LookupParameter(n); return p == null ? null : (p.StorageType == StorageType.String ? (p.AsString() ?? "") : p.AsValueString() ?? ""); };
foreach (var n in new[] { "Nome do cliente", "Endereço do projeto", "Autor do projeto", "Numero CAU", "Nome do projeto", "Área do lote" }) {
    var v = Valor(n); if (v == null) continue;
    if (v.Trim() == "" || v.Trim() == "0" || v.StartsWith("0,00") || v.StartsWith("0.00")) bloqueia.Add("Carimbo: \"" + n + "\" vazio.");
}
foreach (var n in new[] { "RRT", "ART", "Executor da obra", "Numero CREA", "Desenhista", "ETAPA DO PROJETO" }) { var v = Valor(n); if (v != null && v.Trim() == "") atencao.Add("Carimbo: \"" + n + "\" vazio."); }

// --- folhas por caderno
var folhas = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => !s.IsPlaceholder && s.SheetNumber.StartsWith(prefixo)).ToList();
var cadernos = new Dictionary<string, List<Tuple<ViewSheet, FamilyInstance>>>();
foreach (var s in folhas) {
    var tb = new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
    var fam = tb == null ? "" : tb.Symbol.FamilyName.ToUpper();
    var cad = fam.Contains("PROPOSTA") ? "PROPOSTA" : fam.Contains("DETALHAMENTO") || fam.Contains("INTERIORES") ? "DET" : fam.Contains("EXECUTIVO") ? "ARQ" : "OUTRO";
    if (!cadernos.ContainsKey(cad)) cadernos[cad] = new List<Tuple<ViewSheet, FamilyInstance>>();
    cadernos[cad].Add(Tuple.Create(s, tb));
}
var vistasEmFolha = new HashSet<ElementId>();
foreach (var kv in cadernos) {
    var lista = kv.Value;
    // numeração i/N
    var nums = lista.Select(x => x.Item1.SheetNumber.Substring(prefixo.Length).Trim()).ToList();
    var partes = nums.Select(n => { var p = n.Split('/'); int a, b; return (p.Length == 2 && int.TryParse(p[0], out a) && int.TryParse(p[1], out b)) ? new[] { a, b } : null; }).ToList();
    if (partes.Any(p => p == null)) corrigir.Add("Caderno " + kv.Key + ": números fora do padrão i/N → " + string.Join(", ", nums.Where((n, i) => partes[i] == null)));
    var validas = partes.Where(p => p != null).ToList();
    if (validas.Count > 0) {
        int total = lista.Count;
        var errados = validas.Where(p => p[1] != total).Select(p => p[0] + "/" + p[1]).ToList();
        var faltam = Enumerable.Range(1, total).Where(i => !validas.Any(p => p[0] == i)).ToList();
        var repetidos = validas.GroupBy(p => p[0]).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (errados.Count > 0 || faltam.Count > 0 || repetidos.Count > 0)
            bloqueia.Add("Caderno " + kv.Key + " (" + total + " folhas): numeração quebrada" + (errados.Count > 0 ? " · total diferente de " + total + ": " + string.Join(", ", errados) : "") + (faltam.Count > 0 ? " · falta " + string.Join(", ", faltam.Select(f => f + "/" + total)) : "") + (repetidos.Count > 0 ? " · repetido: " + string.Join(", ", repetidos) : ""));
        else ok.Add("Caderno " + kv.Key + ": " + total + " folhas, numeração 1/" + total + " a " + total + "/" + total);
    }
    foreach (var x in lista) {
        var s = x.Item1; var o = x.Item2 != null ? x.Item2.GetTransform().Origin : XYZ.Zero;
        var vps = s.GetAllViewports().Select(id => (Viewport)document.GetElement(id)).ToList();
        var tabs = new FilteredElementCollector(document, s.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Where(si => !si.IsTitleblockRevisionSchedule).ToList();
        foreach (var vp in vps) vistasEmFolha.Add(vp.ViewId);
        if (vps.Count == 0 && tabs.Count == 0) { bloqueia.Add("Folha " + s.SheetNumber + " (" + s.Name + ") está vazia."); continue; }
        var pc = s.LookupParameter("CONTEÚDO DA PRANCHA"); if (pc != null && string.IsNullOrWhiteSpace(pc.AsString())) corrigir.Add("Folha " + s.SheetNumber + ": CONTEÚDO DA PRANCHA vazio.");
        foreach (var si in tabs) {
            int itens = new FilteredElementCollector(document, si.ScheduleId).WhereElementIsNotElementType().GetElementCount();
            if (itens == 0) corrigir.Add("Folha " + s.SheetNumber + ": tabela \"" + document.GetElement(si.ScheduleId).Name + "\" sem itens.");
        }
        // área útil por caderno (A1): ARQ = moldura menos o carimbo no canto; PROPOSTA = acima da faixa do carimbo
        if (kv.Key == "ARQ" || kv.Key == "PROPOSTA") {
            var caixas = vps.Select(vp => { var b = vp.GetBoxOutline(); return new { Nome = ((View)document.GetElement(vp.ViewId)).Name, x0 = ToMM(b.MinimumPoint.X - o.X), y0 = ToMM(b.MinimumPoint.Y - o.Y), x1 = ToMM(b.MaximumPoint.X - o.X), y1 = ToMM(b.MaximumPoint.Y - o.Y) }; }).ToList();
            foreach (var c in caixas) {
                bool fora = c.x0 < 20 || c.x1 > 837 || c.y1 > 612 || c.y0 < 5 || (kv.Key == "ARQ" && c.x1 > 661 && c.y0 < 297) || (kv.Key == "PROPOSTA" && c.y0 < 53);
                if (fora) corrigir.Add("Folha " + s.SheetNumber + ": \"" + c.Nome + "\" fora da área útil ou sobre o carimbo.");
            }
            for (int i = 0; i < caixas.Count; i++) for (int j = i + 1; j < caixas.Count; j++) {
                var a = caixas[i]; var b = caixas[j];
                double sx = Math.Min(a.x1, b.x1) - Math.Max(a.x0, b.x0), sy = Math.Min(a.y1, b.y1) - Math.Max(a.y0, b.y0);
                if (sx > 25 && sy > 25) corrigir.Add("Folha " + s.SheetNumber + ": \"" + a.Nome + "\" e \"" + b.Nome + "\" se sobrepõem (" + Math.Round(sx) + " × " + Math.Round(sy) + " mm) — confira na imagem.");
            }
        }
    }
}
// --- vistas do caderno fora de folha
var palavras = new[] { "CORTE", "FACHADA", "EXECUTIVO", "PERSPECTIVA", "PAGINAÇÃO", "ILUMINAÇÃO", "TOMADA", "FORRO", "COBERTURA", "IMPLANTA", "SITUAÇÃO", "LAYOUT" };
var foraDeFolha = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>()
    .Where(v => !v.IsTemplate && !(v is ViewSheet) && !(v is ViewSchedule) && v.ViewType != ViewType.Legend && v.ViewType != ViewType.ProjectBrowser && v.ViewType != ViewType.SystemBrowser && v.ViewType != ViewType.Internal
        && v.Name.StartsWith(prefixo) && palavras.Any(p => v.Name.ToUpper().Contains(p)) && !v.Name.ToUpper().Contains("TRABALHO") && !vistasEmFolha.Contains(v.Id)).Select(v => v.Name).OrderBy(n => n).ToList();
if (foraDeFolha.Count > 0) atencao.Add("Vistas do caderno que não estão em nenhuma folha (" + foraDeFolha.Count + "): " + string.Join("; ", foraDeFolha.Take(12)) + (foraDeFolha.Count > 12 ? "; …" : ""));
var avisos = document.GetWarnings().Count; if (avisos > 0) atencao.Add("Avisos do Revit: " + avisos + " (detalhes na conferência do modelo, fase 3).");

var sb = new System.Text.StringBuilder();
sb.AppendLine("CONFERÊNCIA FINAL — " + document.Title + (prefixo != "" ? " (folhas \"" + prefixo + "…\")" : ""));
sb.AppendLine(bloqueia.Count == 0 ? "Pronto para emitir" + (corrigir.Count > 0 ? ", com " + corrigir.Count + " item(ns) a corrigir." : ".") : "⛔ " + bloqueia.Count + " bloqueio(s): não emitir antes de resolver.");
if (bloqueia.Count > 0) { sb.AppendLine("\nBLOQUEIA"); foreach (var x in bloqueia) sb.AppendLine("  • " + x); }
if (corrigir.Count > 0) { sb.AppendLine("\nCORRIGIR"); foreach (var x in corrigir) sb.AppendLine("  • " + x); }
if (atencao.Count > 0) { sb.AppendLine("\nATENÇÃO"); foreach (var x in atencao) sb.AppendLine("  • " + x); }
if (ok.Count > 0) { sb.AppendLine("\nOK"); foreach (var x in ok) sb.AppendLine("  • " + x); }
return sb.ToString();
