// SKILL EXECUTIVO REVIT — Fase 5 (e de novo na 6): nível da calçada + TERRA nos cortes
// send_code_to_revit · transactionMode "none"
// Regra do escritório: entre o nível do térreo e o nível da calçada, todo corte leva região preenchida "VZ-Terra",
//   na largura do lote (de divisa a divisa, onde o plano do corte atravessa o lote). Topo da terra = nível "TERREO N.O"
//   (osso) se existir, senão o "TERREO N.A"; base = nível "CALÇADA". Calçada e rua ficam fora (estão no nível da calçada).
// parameters[0] = vistas separadas por ";" (vazio = todos os cortes cujo nome começa com "CORTE")
// parameters[1] = cota da calçada em metros, relativa ao térreo (ex.: "-0,30"). Vazio = mantém o nível como está.
//   Com valor: move o nível CALÇADA e passa os pisos de fora do lote (calçada, asfalto) para o nível CALÇADA, desvio 0.
// parameters[2] = "refazer" apaga antes as regiões de terra que a skill fez nessas vistas (comentário "SKILL TERRA")
// Também baixa o recorte inferior dos cortes para 0,50 m abaixo da calçada (tira a faixa vazia).
// Uma transação "Skill: terra nos cortes". Não salva. Lote retangular (caixa da linha de propriedade sem "RECUO").
// --- projeto pelo caminho (regra 5): se o 1º parâmetro for "DOC:<caminho do .rvt>", trabalha nesse arquivo aberto.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
Func<int, string> P = i => parameters != null && parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
Func<string, string> N = s => s.ToUpper().Replace("Ç", "C").Replace("É", "E");
var log = new System.Text.StringBuilder();
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().ToList();
var nCalc = niveis.FirstOrDefault(l => N(l.Name).Contains("CALCADA"));
var nTerreoNA = niveis.FirstOrDefault(l => N(l.Name).Contains("TERREO") && N(l.Name).Contains("N.A")) ?? niveis.OrderBy(l => Math.Abs(l.Elevation)).First();
var nTerreoNO = niveis.FirstOrDefault(l => N(l.Name).Contains("TERREO") && N(l.Name).Contains("N.O"));
if (nCalc == null) return "ERRO: não há nível com \"CALÇADA\" no nome. Crie o nível da calçada ou informe o nome.";
var tipo = new FilteredElementCollector(document).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().FirstOrDefault(t => N(t.Name).Contains("TERRA"));
if (tipo == null) return "ERRO: não há tipo de região com \"TERRA\" no nome (o template tem VZ-Terra).";
var props = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).OfClass(typeof(PropertyLine)).ToElements()
    .Where(e => !((e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "").ToUpper().Contains("RECUO"))).ToList();
if (props.Count == 0) return "ERRO: sem linha de propriedade do lote.";
var lote = props.OrderByDescending(e => e.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0).First();
var lb = lote.get_BoundingBox(null); double x0 = lb.Min.X, y0 = lb.Min.Y, x1 = lb.Max.X, y1 = lb.Max.Y;
var nomes = P(0) == "" ? new List<string>() : P(0).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var cortes = new FilteredElementCollector(document).OfClass(typeof(ViewSection)).Cast<ViewSection>().Where(v => !v.IsTemplate && v.ViewType == ViewType.Section &&
    (nomes.Count > 0 ? nomes.Contains(v.Name) : N(v.Name).StartsWith("CORTE"))).ToList();
if (cortes.Count == 0) return "ERRO: nenhum corte encontrado.";
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
document.Application.FailuresProcessing += __avisos;
try {
    using (var t = new Transaction(document, "Skill: terra nos cortes")) {
        t.Start();
        // 1) nível da calçada + pisos de fora do lote
        if (P(1) != "") {
            double cota = double.Parse(P(1).Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            nCalc.Elevation = nTerreoNA.Elevation + F(cota);
            log.AppendLine("nível " + nCalc.Name + " → " + (nCalc.Elevation * 0.3048).ToString("0.00") + " m");
            foreach (Floor fl in new FilteredElementCollector(document).OfClass(typeof(Floor)).WhereElementIsNotElementType()) {
                var bb = fl.get_BoundingBox(null); if (bb == null || bb.Max.Z > nTerreoNA.Elevation + F(0.5)) continue;
                double cx = (bb.Min.X + bb.Max.X) / 2, cy = (bb.Min.Y + bb.Max.Y) / 2;
                if (cx > x0 && cx < x1 && cy > y0 && cy < y1) continue; // dentro do lote: fica no térreo
                var pl = fl.get_Parameter(BuiltInParameter.LEVEL_PARAM); var po = fl.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                if (pl != null && !pl.IsReadOnly) pl.Set(nCalc.Id); if (po != null && !po.IsReadOnly) po.Set(0.0);
                log.AppendLine("piso " + fl.Name + " (" + fl.Id + ") → nível " + nCalc.Name);
            }
        }
        double zBase = nCalc.Elevation, zTopo = (nTerreoNO ?? nTerreoNA).Elevation;
        if (zTopo - zBase < F(0.01)) { t.RollBack(); return "ERRO: calçada não está abaixo do térreo (nada a preencher)."; }
        foreach (var v in cortes) {
            if (P(2).ToLower() == "refazer")
                foreach (var r in new FilteredElementCollector(document, v.Id).OfClass(typeof(FilledRegion)).ToElements()
                    .Where(e => (e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "") == "SKILL TERRA").Select(e => e.Id).ToList())
                    document.Delete(r);
            // 2) trecho do plano do corte dentro do lote (recorte da reta origem + u·R pela caixa do lote)
            XYZ o = v.Origin, R = v.RightDirection; double umin = double.MinValue, umax = double.MaxValue; bool ok = true;
            foreach (var par in new[] { Tuple.Create(o.X, R.X, x0, x1), Tuple.Create(o.Y, R.Y, y0, y1) }) {
                if (Math.Abs(par.Item2) < 1e-9) { if (par.Item1 < par.Item3 || par.Item1 > par.Item4) ok = false; continue; }
                double a = (par.Item3 - par.Item1) / par.Item2, b = (par.Item4 - par.Item1) / par.Item2;
                umin = Math.Max(umin, Math.Min(a, b)); umax = Math.Min(umax, Math.Max(a, b));
            }
            if (!ok || umax - umin < F(0.05)) { log.AppendLine(v.Name + ": o corte não atravessa o lote — pulei"); continue; }
            Func<double, double, XYZ> Pt = (u, z) => new XYZ(o.X + R.X * u, o.Y + R.Y * u, z);
            var loop = new CurveLoop();
            var pts = new[] { Pt(umin, zBase), Pt(umax, zBase), Pt(umax, zTopo), Pt(umin, zTopo) };
            for (int i = 0; i < 4; i++) loop.Append(Line.CreateBound(pts[i], pts[(i + 1) % 4]));
            var reg = FilledRegion.Create(document, tipo.Id, v.Id, new List<CurveLoop> { loop });
            reg.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("SKILL TERRA");
            // 3) recorte inferior: 0,50 m abaixo da calçada
            if (v.CropBoxActive) {
                var cb = v.CropBox; var inv = cb.Transform.Inverse; double yLoc = inv.OfPoint(new XYZ(o.X, o.Y, zBase - F(0.50))).Y;
                if (yLoc > cb.Min.Y || cb.Min.Y < yLoc - F(0.30)) { cb.Min = new XYZ(cb.Min.X, yLoc, cb.Min.Z); v.CropBox = cb; }
            }
            log.AppendLine(v.Name + ": terra " + ((umax - umin) * 0.3048).ToString("0.00") + " m × " + ((zTopo - zBase) * 0.3048).ToString("0.00") + " m");
        }
        t.Commit();
    }
} finally { document.Application.FailuresProcessing -= __avisos; }
return log.ToString();
