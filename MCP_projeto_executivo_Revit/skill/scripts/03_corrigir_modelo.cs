// SKILL EXECUTIVO REVIT — Fase 3: correções seguras do modelo (só depois que o arquiteto confirmar)
// send_code_to_revit · transactionMode "none"
// parameters[0] = correções separadas por ";":
//   "ambientes_nao_colocados" — apaga ambientes sem localização (sobras de listas; aparecem em tabelas sem área)
//   "paredes_curtas"          — apaga paredes com menos de 5 cm sem portas/janelas hospedadas
//   "renumerar_esquadrias"    — renumera a marca de tipo das portas (P01, P02…) e janelas (J01, J02…) usadas no modelo,
//                               da maior largura para a menor; tipos não usados ficam como estão
//   Regras de projeto do checklist executivo (só depois de o arquiteto aprovar a lista da conferência):
//   "peitoril"       — janelas fora de banheiros/lavabos com peitoril ≠ 1,00 m passam para 1,00 m
//   "portas_largura" — porta de entrada < 1,30 m, de lavabo < 0,70 m, de alumínio em corredor < 0,90 m: troca pelo tipo da
//                      mesma família com a menor largura que atende (se a família não tiver, avisa)
//   "bancadas"       — marcenaria "BANCADA"/"ILHA"/"CHURRASQ": ajusta os parâmetros de profundidade, altura e largura que a
//                      família tiver (cozinha 0,70 × h 0,90; lavabo 0,55; lavanderia 0,60; ilha ≥ 0,90; churrasqueira
//                      1,10 × 0,85). Parâmetro de tipo usado por outras peças: duplica o tipo antes de mudar.
// parameters[1] = (opcional) ids aprovados, separados por "," — sem isso, corrige todos os que fogem da regra
// Porta de entrada não pivotante não é trocada (é outra família): fica no relatório.
// Tudo numa única transação "Skill: corrigir modelo" (desfazível com Ctrl+Z). Não salva.
// O que depende de decisão do arquiteto (ambientes não fechados, marcas de tipo repetidas, linhas de separação sobrepostas,
// CAD importado) fica só no relatório de 03_conferir_modelo.cs.
var pedidas = (parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "").ToLower().Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
if (pedidas.Count == 0) return "Nada pedido. Opções: ambientes_nao_colocados; paredes_curtas; renumerar_esquadrias; peitoril; portas_largura; bancadas";
var aprovados = new HashSet<long>((parameters.Length > 1 ? ((parameters[1] as string) ?? "") : "").Split(',').Select(x => { long v; return long.TryParse(x.Trim(), out v) ? v : -1; }).Where(v => v > 0));
Func<Element, bool> Aprovado = e => aprovados.Count == 0 || aprovados.Contains(e.Id.Value);
Func<double, double> F = m => m / 0.3048; double tol = F(0.02);
Func<string, string> SemAc = x => new string((x ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
Phase faseFinal = null; foreach (Phase ph in document.Phases) faseFinal = ph;
Func<Autodesk.Revit.DB.Architecture.Room, string> NomeR = r => r == null ? "" : SemAc(r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
Func<FamilyInstance, List<string>> Lados = fi => { var l = new List<string>(); try { l.Add(NomeR(fi.get_FromRoom(faseFinal))); l.Add(NomeR(fi.get_ToRoom(faseFinal))); } catch { } return l; };
var cultR = new System.Globalization.CultureInfo("pt-BR"); Func<double, string> M = v => (v * 0.3048).ToString("0.00", cultR);
var log = new System.Text.StringBuilder();
using (var t = new Transaction(document, "Skill: corrigir modelo")) {
    t.Start();
    if (pedidas.Contains("ambientes_nao_colocados")) {
        var ids = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
            .Where(r => r.Location == null).Select(r => r.Id).ToList();
        if (ids.Count > 0) document.Delete(ids);
        log.AppendLine("Ambientes não colocados apagados: " + ids.Count);
    }
    if (pedidas.Contains("paredes_curtas")) {
        var hospedes = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(fi => fi.Host is Wall).Select(fi => fi.Host.Id));
        var ids = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>()
            .Where(w => w.Location is LocationCurve && ((LocationCurve)w.Location).Curve.Length < 0.05 / 0.3048 && !hospedes.Contains(w.Id)).Select(w => w.Id).ToList();
        if (ids.Count > 0) document.Delete(ids);
        log.AppendLine("Paredes com menos de 5 cm apagadas: " + ids.Count);
    }
    if (pedidas.Contains("renumerar_esquadrias")) {
        foreach (var par in new[] { Tuple.Create(BuiltInCategory.OST_Doors, "P"), Tuple.Create(BuiltInCategory.OST_Windows, "J") }) {
            var tipos = new FilteredElementCollector(document).OfCategory(par.Item1).WhereElementIsNotElementType().Cast<FamilyInstance>().Select(fi => fi.Symbol).GroupBy(x => x.Id).Select(g => g.First()).ToList();
            Func<FamilySymbol, double> Larg = fs => { var p = fs.get_Parameter(BuiltInParameter.DOOR_WIDTH) ?? fs.get_Parameter(BuiltInParameter.WINDOW_WIDTH) ?? fs.LookupParameter("Largura"); return p != null && p.StorageType == StorageType.Double ? p.AsDouble() : 0; };
            int n = 0; var mud = new List<string>();
            foreach (var fs in tipos.OrderByDescending(Larg).ThenBy(x => x.FamilyName).ThenBy(x => x.Name)) {
                var nova = par.Item2 + (++n).ToString("00"); var pm = fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK); var velha = pm.AsString() ?? "";
                if (velha != nova) { pm.Set(nova); mud.Add(fs.Name + ": " + (velha == "" ? "(vazia)" : velha) + " → " + nova); }
            }
            log.AppendLine((par.Item2 == "P" ? "Portas" : "Janelas") + " renumeradas: " + mud.Count + (mud.Count > 0 ? " (" + string.Join("; ", mud) + ")" : ""));
        }
    }
    var umidos = new[] { "WC", "BANHEIRO", "LAVABO", "BANHO", "LAVATORIO" };
    if (pedidas.Contains("peitoril")) {
        int n = 0;
        foreach (var fi in new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().Cast<FamilyInstance>()) {
            var p = fi.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM); if (p == null || p.IsReadOnly || !Aprovado(fi)) continue;
            if (Lados(fi).Any(nm => umidos.Any(u => nm.Contains(u)))) continue;
            if (Math.Abs(p.AsDouble() - F(1.00)) > tol) { p.Set(F(1.00)); n++; } }
        log.AppendLine("Peitoris ajustados para 1,00 m: " + n);
    }
    if (pedidas.Contains("portas_largura")) {
        var entrada = new[] { "SALA", "ESTAR", "LIVING", "HALL", "ENTRADA", "VESTIBULO" }; var mud = new List<string>(); var sem = new List<string>();
        Func<FamilySymbol, double> LargT = fs => { var p = fs.get_Parameter(BuiltInParameter.DOOR_WIDTH) ?? fs.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM); return p != null && p.HasValue ? p.AsDouble() : 0; };
        foreach (var fi in new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList()) {
            if (!Aprovado(fi)) continue; var nomes = Lados(fi); double w = LargT(fi.Symbol); if (w <= 0) continue;
            string tipo = SemAc(fi.Symbol.FamilyName + " " + fi.Symbol.Name); double min = 0;
            bool externa = nomes.Any(nm => nm == "" || nm.Contains("ALPENDRE") || nm.Contains("VARANDA") || nm.Contains("CALCADA"));
            if (externa && nomes.Any(nm => entrada.Any(e => nm.Contains(e)))) min = 1.30;
            else if (nomes.Any(nm => nm.Contains("LAVABO"))) min = 0.70;
            else if (tipo.Contains("ALUM") && nomes.Any(nm => nm.Contains("CORREDOR") || nm.Contains("CIRCULA"))) min = 0.90;
            if (min == 0 || w >= F(min) - tol) continue;
            var alternativa = fi.Symbol.Family.GetFamilySymbolIds().Select(id => document.GetElement(id) as FamilySymbol).Where(fs => fs != null && LargT(fs) >= F(min) - tol).OrderBy(LargT).FirstOrDefault();
            if (alternativa == null) { sem.Add(fi.Symbol.Name + " [" + fi.Id.Value + "]: a família não tem tipo com " + min.ToString("0.00", cultR) + " m"); continue; }
            if (!alternativa.IsActive) alternativa.Activate();
            fi.Symbol = alternativa; mud.Add("[" + fi.Id.Value + "] " + M(w) + " → " + alternativa.Name + " (" + M(LargT(alternativa)) + " m)");
        }
        log.AppendLine("Portas trocadas: " + mud.Count + (mud.Count > 0 ? " (" + string.Join("; ", mud) + ")" : "") + (sem.Count > 0 ? "\n  sem tipo adequado: " + string.Join("; ", sem) : ""));
    }
    if (pedidas.Contains("bancadas")) {
        var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
        var mud = new List<string>(); var sem = new List<string>();
        foreach (var fi in new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Casework).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList()) {
            if (!Aprovado(fi)) continue; string nm = SemAc(fi.Symbol.FamilyName + " " + fi.Symbol.Name);
            if (!nm.Contains("BANCADA") && !nm.Contains("ILHA") && !nm.Contains("CHURRASQ")) continue;
            var b = fi.get_BoundingBox(null); if (b == null) continue; var c = (b.Min + b.Max) / 2;
            var r = rooms.FirstOrDefault(x => { try { return x.IsPointInRoom(new XYZ(c.X, c.Y, x.Level.Elevation + F(0.5))); } catch { return false; } }); string amb = NomeR(r);
            // alvo: (profundidade, altura, largura) — 0 = não mexe
            double prof = 0, alt = 0, larg = 0;
            if (nm.Contains("CHURRASQ")) { prof = 0.85; larg = 1.10; }
            else if (nm.Contains("ILHA")) { double pAt = Math.Min(b.Max.X - b.Min.X, b.Max.Y - b.Min.Y); if (pAt < F(0.90) - tol) prof = 0.90; }
            else if (amb.Contains("COZINHA") || amb.Contains("GOURMET")) { prof = 0.70; double hAt = r != null ? b.Max.Z - r.Level.Elevation : 0; if (r != null && Math.Abs(hAt - F(0.95)) > tol) alt = 0.90; }
            else if (amb.Contains("LAVABO")) prof = 0.55;
            else if (amb.Contains("LAVANDERIA") || amb.Contains("SERVICO")) prof = 0.60;
            if (prof == 0 && alt == 0 && larg == 0) continue;
            var feitas = new List<string>();
            foreach (var alvo in new[] { Tuple.Create("PROFUNDIDADE", prof), Tuple.Create("ALTURA", alt), Tuple.Create("LARGURA", larg) }) {
                if (alvo.Item2 == 0) continue;
                Parameter p = fi.Parameters.Cast<Parameter>().FirstOrDefault(q => q.Definition != null && SemAc(q.Definition.Name).StartsWith(alvo.Item1) && q.StorageType == StorageType.Double && !q.IsReadOnly);
                if (p == null) {
                    var pt = fi.Symbol.Parameters.Cast<Parameter>().FirstOrDefault(q => q.Definition != null && SemAc(q.Definition.Name).StartsWith(alvo.Item1) && q.StorageType == StorageType.Double && !q.IsReadOnly);
                    if (pt == null) { sem.Add(fi.Symbol.Name + " [" + fi.Id.Value + "]: sem parâmetro de " + alvo.Item1.ToLower()); continue; }
                    if (Math.Abs(pt.AsDouble() - F(alvo.Item2)) <= tol) continue;
                    // tipo compartilhado: duplica para não mudar outras peças
                    bool outros = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Casework).WhereElementIsNotElementType().Cast<FamilyInstance>().Any(o => o.Id != fi.Id && o.Symbol.Id == fi.Symbol.Id);
                    var tipoAlvo = fi.Symbol;
                    if (outros) { var nomeNovo = fi.Symbol.Name + " " + (alvo.Item2 * 100).ToString("0") + "cm"; var ex = fi.Symbol.Family.GetFamilySymbolIds().Select(id => document.GetElement(id) as FamilySymbol).FirstOrDefault(fs => fs.Name == nomeNovo);
                        tipoAlvo = ex ?? (FamilySymbol)fi.Symbol.Duplicate(nomeNovo); fi.Symbol = tipoAlvo; }
                    tipoAlvo.LookupParameter(pt.Definition.Name).Set(F(alvo.Item2)); feitas.Add(alvo.Item1.ToLower() + " " + alvo.Item2.ToString("0.00", cultR) + (outros ? " (tipo novo " + tipoAlvo.Name + ")" : " (tipo)"));
                } else if (Math.Abs(p.AsDouble() - F(alvo.Item2)) > tol) { p.Set(F(alvo.Item2)); feitas.Add(alvo.Item1.ToLower() + " " + alvo.Item2.ToString("0.00", cultR)); }
            }
            if (feitas.Count > 0) mud.Add(fi.Symbol.Name + " em " + (amb == "" ? "?" : amb) + " [" + fi.Id.Value + "]: " + string.Join(", ", feitas));
        }
        log.AppendLine("Bancadas ajustadas: " + mud.Count + (mud.Count > 0 ? "\n  " + string.Join("\n  ", mud) : "") + (sem.Count > 0 ? "\n  não ajustadas: " + string.Join("; ", sem) : ""));
    }
    t.Commit();
}
log.AppendLine("Feito numa transação só (Ctrl+Z desfaz). O arquivo não foi salvo.");
return log.ToString();
