// SKILL EXECUTIVO REVIT — Manutenção: limpeza do template master (sobras do projeto-exemplo)
// send_code_to_revit · transactionMode "none"
// NUNCA rodar no template master original: o responsável pelo template abre uma CÓPIA, roda, confere e só então
// substitui o master. O script recusa o modo "aplicar" se o nome do arquivo não contiver "COPIA" ou "TESTE".
// parameters[0] = "relatorio" (padrão: só lista, não altera nada) | "aplicar"
// parameters[1] = itens a aplicar, separados por ";" (padrão: "divisas;regioes_area;modelo")
//   divisas      — apaga linhas de propriedade do exemplo (o lote vem do projeto)
//   regioes_area — apaga regiões preenchidas das vistas de área (construída, ocupada, permeável, implantação)
//   modelo       — apaga paredes, pisos, telhados, forros, portas, janelas, mobiliário e ambientes do exemplo
//   purgar       — apaga elementos não usados (Document.GetUnusedElements) — rode por último, depois de conferir
// Só relata (decisão do responsável): tipos de janela "Padrão" com largura acima de 2,00 m, portas e janelas sem
// ABERTURA/MATERIAL, marcas de tipo fora do padrão P01/J01, legendas com valores fixos (textos com números de área).
// Uma transação "Skill: limpar template" (Ctrl+Z desfaz). Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var modo = P(0) == "" ? "relatorio" : P(0).ToLower();
var itens = (P(1) == "" ? "divisas;regioes_area;modelo" : P(1).ToLower()).Split(';').Select(s => s.Trim()).ToList();
var nomeArq = System.IO.Path.GetFileNameWithoutExtension(document.PathName ?? "").ToUpper();
if (modo == "aplicar" && !nomeArq.Contains("COPIA") && !nomeArq.Contains("CÓPIA") && !nomeArq.Contains("TESTE") && !nomeArq.Contains("PILOTO"))
    return "RECUSADO: o modo \"aplicar\" só roda em cópia do template (nome com COPIA ou TESTE). Arquivo: " + nomeArq;
var log = new System.Text.StringBuilder(); log.AppendLine("Arquivo: " + nomeArq + " — modo " + modo);
Func<BuiltInCategory, List<ElementId>> Inst = c => new FilteredElementCollector(document).OfCategory(c).WhereElementIsNotElementType().ToElementIds().ToList();
// --- levantamento
var divisas = Inst(BuiltInCategory.OST_SiteProperty);
var vistasArea = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate && !(v is ViewSchedule) && (v.ViewType == ViewType.AreaPlan || v.Name.ToUpper().Contains("IMPLANTA") || v.Name.ToUpper().Contains("ÁREA"))).ToList();
var regioes = vistasArea.SelectMany(v => new FilteredElementCollector(document, v.Id).OfClass(typeof(FilledRegion)).ToElementIds()).Distinct().ToList();
var catsModelo = new[] { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings, BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows,
    BuiltInCategory.OST_Furniture, BuiltInCategory.OST_Casework, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_Rooms, BuiltInCategory.OST_Areas,
    BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_LightingFixtures, BuiltInCategory.OST_Planting };
var modelo = new Dictionary<string, List<ElementId>>();
foreach (var c in catsModelo) { var ids = Inst(c); if (ids.Count > 0) modelo[LabelUtils.GetLabelFor(c)] = ids; }
log.AppendLine("Linhas de propriedade: " + divisas.Count);
log.AppendLine("Regiões preenchidas em vistas de área/implantação: " + regioes.Count + " (" + string.Join(", ", vistasArea.Select(v => v.Name)) + ")");
log.AppendLine("Modelo do exemplo: " + (modelo.Count == 0 ? "nenhum" : string.Join(", ", modelo.Select(kv => kv.Key + " " + kv.Value.Count))));
// --- só relatório
var largos = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsElementType().Cast<ElementType>()
    .Select(tp => new { tp.Name, L = tp.get_Parameter(BuiltInParameter.WINDOW_WIDTH) ?? tp.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM) }).Where(x => x.L != null && x.L.AsDouble() > F(2.0)).ToList();
foreach (var x in largos) log.AppendLine("  ⚠ tipo de janela " + x.Name + " com " + Math.Round(x.L.AsDouble() * 0.3048, 2) + " m de largura" + (x.Name.ToUpper().Contains("PADR") ? " — é o tipo padrão: reduza para 1,20/1,50 m" : ""));
foreach (var c in new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows }) {
    var tipos = new FilteredElementCollector(document).OfCategory(c).WhereElementIsElementType().Cast<ElementType>().ToList();
    var foraPadrao = tipos.Select(tp => tp.get_Parameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsString() ?? "").Where(m => m != "" && !System.Text.RegularExpressions.Regex.IsMatch(m, c == BuiltInCategory.OST_Doors ? @"^P\d{2,}$" : @"^J\d{2,}$")).Distinct().ToList();
    if (foraPadrao.Count > 0) log.AppendLine("  ⚠ " + LabelUtils.GetLabelFor(c) + ": marcas de tipo fora do padrão: " + string.Join(", ", foraPadrao.Take(15)));
    int semInfo = tipos.Count(tp => new[] { "ABERTURA", "MATERIAL" }.Any(n => { var pr = tp.LookupParameter(n); return pr != null && string.IsNullOrWhiteSpace(pr.AsString()); }));
    if (semInfo > 0) log.AppendLine("  ⚠ " + LabelUtils.GetLabelFor(c) + ": " + semInfo + " tipos sem ABERTURA/MATERIAL preenchidos (as tabelas saem com colunas vazias)");
}
var legendasFixas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => v.ViewType == ViewType.Legend)
    .Select(v => new { v.Name, n = new FilteredElementCollector(document, v.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Count(tn => System.Text.RegularExpressions.Regex.IsMatch(tn.Text, @"\d+[,.]\d+\s*(m²|M²|%)")) })
    .Where(x => x.n > 0).ToList();
foreach (var x in legendasFixas) log.AppendLine("  ⚠ legenda " + x.Name + ": " + x.n + " textos com valores de área/percentual fixos (devem vir de rótulos ou ser preenchidos pela skill)");
if (modo != "aplicar") { log.AppendLine("Nada alterado (modo relatório). Para limpar, numa CÓPIA: parameters [\"aplicar\", \"divisas;regioes_area;modelo\"]."); return log.ToString(); }
// --- aplicar
using (var t = new Transaction(document, "Skill: limpar template")) {
    t.Start();
    int apagados = 0;
    Action<ICollection<ElementId>, string> Apagar = (ids, rot) => { if (ids.Count == 0) return; try { var r = document.Delete(ids); apagados += r.Count; log.AppendLine("apagado: " + rot + " (" + ids.Count + ")"); } catch (Exception ex) { log.AppendLine("falhou ao apagar " + rot + ": " + ex.Message); } };
    if (itens.Contains("divisas")) Apagar(divisas, "linhas de propriedade");
    if (itens.Contains("regioes_area")) Apagar(regioes, "regiões das vistas de área");
    if (itens.Contains("modelo")) {
        // ambientes e áreas primeiro, depois hospedados (portas/janelas), depois hospedeiros
        foreach (var k in new[] { "Ambientes", "Áreas", "Portas", "Janelas" }) if (modelo.ContainsKey(k)) Apagar(modelo[k].Where(i => document.GetElement(i) != null).ToList(), k);
        foreach (var kv in modelo) if (!new[] { "Ambientes", "Áreas", "Portas", "Janelas" }.Contains(kv.Key)) Apagar(kv.Value.Where(i => document.GetElement(i) != null).ToList(), kv.Key);
    }
    if (itens.Contains("purgar")) {
        for (int passo = 0; passo < 3; passo++) {   // purga em cascata: o que fica sem uso depois de uma rodada sai na seguinte
            var nao = document.GetUnusedElements(new HashSet<ElementId>()); if (nao.Count == 0) break;
            Apagar(nao, "não usados (rodada " + (passo + 1) + ")");
        }
    }
    t.Commit();
    log.AppendLine("Total apagado (com dependentes): " + apagados + ". Confira as vistas e salve a cópia com outro nome antes de substituir o master.");
}
return log.ToString();
