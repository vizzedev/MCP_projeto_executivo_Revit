// 08_regioes_areas.cs — Fase 8: gera as regiões preenchidas de área na implantação a partir do modelo
// (usadas por calcular_areas_indices.cs). No projeto de referência o arquiteto desenhou essas regiões à mão;
// num projeto novo elas não existem e as áreas do carimbo saem zeradas.
// Regras:
//   - Área construída de cada pavimento = contorno dos pisos do nível "PAV. X N.A" que não são externos
//     (tipo sem GRAMA, JARDIM, INTERTRAVADO, CONCREGRAMA, ASFALTO, SEIXO, PEDRA, PISCINA, PRAINHA, CALÇADA, DECK, SOLEIRA, PINGADEIRA);
//   - Área permeável = contorno dos pisos com GRAMA, JARDIM, CONCREGRAMA ou SEIXO no nome do tipo (concregrama conta 100% —
//     confira a regra do município);
//   - Área da piscina = pisos com PISCINA ou PRAINHA no nome do tipo.
//   Tipos de região: o primeiro tipo cujo nome termina com "Área Construída Pavimento Térreo", "... Superior", "Área Permeável",
//   "Área da Piscina" (independe do prefixo do escritório).
// Parâmetros: [0] vista (padrão "IMPLANTAÇÃO"); [1] "criar" (padrão: só lista o que faria); [2] "refazer" apaga as regiões
// que este script criou antes (comentário "SKILL AREA"); "substituir" apaga TODAS as regiões de área da vista (inclusive
// sobras do template — no teste o template trazia 2 regiões de outro lote que somavam 106 m² a mais).
// Uma transação "Skill: regiões de área". Depois rode calcular_areas_indices.cs.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var nomeVista = P(0) != "" ? P(0) : "IMPLANTAÇÃO"; bool criar = P(1).ToLower() == "criar", refazer = P(2).ToLower() == "refazer", substituir = P(2).ToLower() == "substituir";
var vista = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate && v.Name == nomeVista);
if (vista == null) return "ERRO: vista '" + nomeVista + "' não encontrada.";
var log = new System.Text.StringBuilder();
double zVista = vista.GenLevel != null ? vista.GenLevel.Elevation : 0;
var tiposReg = new FilteredElementCollector(document).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().ToList();
Func<string, FilledRegionType> TipoReg = fim => tiposReg.FirstOrDefault(t => t.Name.Replace("_", "-").EndsWith(fim, StringComparison.OrdinalIgnoreCase));
var externos = new[] { "GRAMA", "JARDIM", "INTERTRAVADO", "CONCREGRAMA", "ASFALTO", "SEIXO", "PEDRA", "PISCINA", "PRAINHA", "CALÇADA", "CALCADA", "DECK", "SOLEIRA", "PINGADEIRA", "FLOREIRA" };
var permeaveis = new[] { "GRAMA", "JARDIM", "CONCREGRAMA", "SEIXO" };
var piscinas = new[] { "PISCINA", "PRAINHA" };
var pisos = new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>().ToList();
Func<Floor, string> NT = f => f.FloorType.Name.ToUpper();
Func<Floor, List<CurveLoop>> Contorno = f => {
    var sk = document.GetElement(f.SketchId) as Sketch; var loops = new List<CurveLoop>(); if (sk == null) return loops;
    foreach (CurveArray arr in sk.Profile) { var cl = new CurveLoop(); foreach (Curve c in arr) cl.Append(c.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, zVista - c.GetEndPoint(0).Z)))); loops.Add(cl); }
    return loops;
};
var plano = new List<Tuple<FilledRegionType, string, Floor>>();
foreach (var nv in new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Where(l => l.Name.ToUpper().StartsWith("PAV.") && l.Name.ToUpper().EndsWith("N.A"))) {
    bool terreo = nv.Name.ToUpper().Contains("TERREO") || nv.Name.ToUpper().Contains("TÉRREO");
    var tipo = TipoReg(terreo ? "Área Construída Pavimento Térreo" : "Área Construída Pavimento Superior");
    foreach (var f in pisos.Where(f => f.LevelId == nv.Id && !externos.Any(e => NT(f).Contains(e)))) plano.Add(Tuple.Create(tipo, "construída " + nv.Name, f));
}
foreach (var f in pisos.Where(f => permeaveis.Any(e => NT(f).Contains(e)))) plano.Add(Tuple.Create(TipoReg("Área Permeável"), "permeável", f));
foreach (var f in pisos.Where(f => piscinas.Any(e => NT(f).Contains(e)))) plano.Add(Tuple.Create(TipoReg("Área da Piscina"), "piscina", f));
foreach (var p in plano) {
    double a = p.Item3.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble() * 0.09290304;
    log.AppendLine("  " + p.Item2 + ": piso \"" + p.Item3.FloorType.Name + "\" " + a.ToString("0.00") + " m² → " + (p.Item1 != null ? p.Item1.Name : "⚠ TIPO DE REGIÃO NÃO ENCONTRADO"));
}
Func<FilledRegion, bool> DeArea = r => { var nm = document.GetElement(r.GetTypeId()).Name.Replace("_", "-"); return new[] { "Área Construída Pavimento Térreo", "Área Construída Pavimento Superior", "Área Ocupada", "Área Permeável", "Área da Piscina" }.Any(k => nm.EndsWith(k, StringComparison.OrdinalIgnoreCase)); };
var outras = new FilteredElementCollector(document, vista.Id).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().Where(r => DeArea(r) && (r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "") != "SKILL AREA").ToList();
if (outras.Count > 0) log.AppendLine("⚠ A vista já tem " + outras.Count + " região(ões) de área que não foram criadas por este script (desenho do arquiteto ou sobra do template) — elas entram na soma. Use \"substituir\" para apagá-las.");
if (!criar) return "PROPOSTA (nada criado)\n" + log.ToString();
int n = 0;
using (var t = new Transaction(document, "Skill: regiões de área")) {
    t.Start();
    if (substituir && outras.Count > 0) { document.Delete(outras.Select(r => r.Id).ToList()); log.AppendLine("Regiões de área anteriores apagadas: " + outras.Count); }
    if (refazer || substituir) { var antigas = new FilteredElementCollector(document, vista.Id).OfClass(typeof(FilledRegion)).Where(e => (e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "") == "SKILL AREA").Select(e => e.Id).ToList(); if (antigas.Count > 0) document.Delete(antigas); log.AppendLine("Regiões anteriores apagadas: " + antigas.Count); }
    foreach (var p in plano.Where(x => x.Item1 != null)) {
        try { var r = FilledRegion.Create(document, p.Item1.Id, vista.Id, Contorno(p.Item3)); r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("SKILL AREA"); n++; }
        catch (Exception ex) { log.AppendLine("  ⚠ " + p.Item2 + ": " + ex.Message); }
    }
    t.Commit();
}
return n + " regiões criadas na vista " + nomeVista + "\n" + log.ToString();
