// SKILL EXECUTIVO REVIT — Fase 6: projeções tracejadas nas plantas executivas
// send_code_to_revit · transactionMode "none"
// Regra do escritório: na planta executiva de um pavimento aparecem, tracejadas, as projeções do que está acima dele:
//   1. paredes do pavimento de cima  — estilo "VZ-Projeção Pav. Superior/Inferior" (contorno da união das paredes)
//   2. borda da laje do pavimento de cima — mesmo estilo, texto "PROJEÇÃO PAV. SUPERIOR"
//   3. cobertura (telhados + platibandas acima do último pavimento) — estilo com "COBERTURA" no nome, senão "VZ-Projeção Preta";
//      texto "PROJEÇÃO COBERTURA"; trecho da cobertura que passa da laje de baixo = "PROJEÇÃO COBERTURA (MARQUISE)"
//   Trechos que caem sobre paredes do próprio pavimento (ou sobre as paredes de cima, para laje e cobertura) não são desenhados.
//   Textos ARIAL 1,8mm PRETO, girados ao longo do maior trecho de cada projeção, afastados para o lado do corpo da casa.
// parameters[0] = nome da planta (ex.: "PAVIMENTO TERREO EXECUTIVO")
// parameters[1] = "refazer" apaga antes as linhas de detalhe com estilo "VZ-Projeção…" e os textos "PROJEÇÃO…" da vista
// Paredes retas e ortogonais (paredes curvas/inclinadas: avisa). Uma transação "Skill: projeções". Não salva.
// --- projeto pelo caminho (regra 5): se o 1º parâmetro for "DOC:<caminho do .rvt>", trabalha nesse arquivo aberto.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
Func<int, string> P = i => parameters != null && parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var v = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(x => !x.IsTemplate && x.Name == P(0));
if (v == null || v.GenLevel == null) return "ERRO: planta '" + P(0) + "' não encontrada.";
double zV = v.GenLevel.Elevation;
var niveisAcima = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Where(l => l.Elevation > zV + F(2.0)).OrderBy(l => l.Elevation).ToList();
var lvS = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
    .Where(r => r.Area > 0 && r.Level != null && r.Level.Elevation > zV + F(2.0)).Select(r => r.Level).OrderBy(l => l.Elevation).FirstOrDefault();   // pavimento de cima = próximo nível com ambientes
var cat = document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
Func<Func<string, bool>, GraphicsStyle> GS = cond => { foreach (Category sc in cat.SubCategories) if (cond(sc.Name.ToUpper())) return sc.GetGraphicsStyle(GraphicsStyleType.Projection); return null; };
var gSup = GS(n => n.Contains("PROJEÇÃO PAV") || n.Contains("PROJECAO PAV")) ?? GS(n => n.Contains("PROJE"));
var gCob = GS(n => (n.Contains("PROJE") && (n.Contains("COBERT") || n.Contains("TELHA")))) ?? GS(n => n == "VZ-PROJEÇÃO PRETA") ?? gSup;
var tt = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault(x => x.Name == "ARIAL 1,8mm PRETO")
      ?? new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().First();
// retângulos 2D das paredes retas ortogonais
var avisos = new List<string>();
Func<Wall, double[]> Ret = w => { var c = ((LocationCurve)w.Location).Curve as Line; var a = c.GetEndPoint(0); var b = c.GetEndPoint(1); double h = w.Width / 2; bool vert = Math.Abs(c.Direction.Y) > 0.5;
    return new[] { Math.Min(a.X, b.X) - (vert ? h : 0), Math.Max(a.X, b.X) + (vert ? h : 0), Math.Min(a.Y, b.Y) - (vert ? 0 : h), Math.Max(a.Y, b.Y) + (vert ? 0 : h) }; };
var paredes = new List<Wall>();
foreach (Wall w in new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => !w.IsStackedWallMember && w.Location is LocationCurve)) {
    var ln = ((LocationCurve)w.Location).Curve as Line;
    if (ln == null || (Math.Abs(ln.Direction.X) < 0.999 && Math.Abs(ln.Direction.Y) < 0.999)) { avisos.Add("parede " + w.Id + " curva ou inclinada — projeção dela à mão"); continue; }
    paredes.Add(w);
}
Func<Wall, double> Zmin = w => w.get_BoundingBox(null).Min.Z; Func<Wall, double> Zmax = w => w.get_BoundingBox(null).Max.Z;
var daVista = paredes.Where(w => Zmin(w) < zV + F(0.5) && Zmax(w) > zV + F(1.0)).Select(Ret).ToList();
var deCima = lvS == null ? new List<double[]>() : paredes.Where(w => Math.Abs(Zmin(w) - lvS.Elevation) < F(0.3)).Select(Ret).ToList();
double topoCima = lvS == null ? zV : paredes.Where(w => Math.Abs(Zmin(w) - lvS.Elevation) < F(0.3)).Select(Zmax).DefaultIfEmpty(lvS.Elevation).Max();
var platib = paredes.Where(w => Zmin(w) > zV + F(2.0) && Zmin(w) >= topoCima - F(0.3) && Zmax(w) - Zmin(w) < F(2.5)).Select(Ret).ToList();   // platibandas/oitões acima do último pavimento
double tol = F(0.003), passo = F(0.01);
// união de retângulos (paredes estendidas meia largura nas pontas para fechar cantos, limitada à caixa real)
Func<List<double[]>, bool, Solid> Uniao = (rs, estender) => {
    Solid u = null;
    foreach (var r in rs) { double e = estender ? F(0.125) : 0; bool hz = (r[1] - r[0]) > (r[3] - r[2]);
        double x0 = r[0] - (hz ? e : 0), x1 = r[1] + (hz ? e : 0), y0 = r[2] - (hz ? 0 : e), y1 = r[3] + (hz ? 0 : e);
        var lp = new CurveLoop(); var p = new[] { new XYZ(x0, y0, 0), new XYZ(x1, y0, 0), new XYZ(x1, y1, 0), new XYZ(x0, y1, 0) }; for (int i = 0; i < 4; i++) lp.Append(Line.CreateBound(p[i], p[(i + 1) % 4]));
        var s = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lp }, XYZ.BasisZ, 1); u = u == null ? s : BooleanOperationsUtils.ExecuteBooleanOperation(u, s, BooleanOperationsType.Union); }
    if (u == null || !estender) return u;
    double bx0 = rs.Min(r => r[0]), bx1 = rs.Max(r => r[1]), by0 = rs.Min(r => r[2]), by1 = rs.Max(r => r[3]);
    var lb = new CurveLoop(); var pb = new[] { new XYZ(bx0, by0, -1), new XYZ(bx1, by0, -1), new XYZ(bx1, by1, -1), new XYZ(bx0, by1, -1) }; for (int i = 0; i < 4; i++) lb.Append(Line.CreateBound(pb[i], pb[(i + 1) % 4]));
    return BooleanOperationsUtils.ExecuteBooleanOperation(u, GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lb }, XYZ.BasisZ, 3), BooleanOperationsType.Intersect);
};
Func<Solid, List<Line>> Contorno = s => { var res = new List<Line>(); if (s == null) return res;
    foreach (Face fc in s.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.FaceNormal.Z > -0.9) continue;
        foreach (EdgeArray ea in pf.EdgeLoops) foreach (Edge ed in ea) { var ln = ed.AsCurve() as Line; if (ln != null) res.Add(Line.CreateBound(new XYZ(ln.GetEndPoint(0).X, ln.GetEndPoint(0).Y, 0), new XYZ(ln.GetEndPoint(1).X, ln.GetEndPoint(1).Y, 0))); } }
    return res; };
Func<List<double[]>, double, double, bool> Sobre = (rs, x, y) => rs.Any(r => x > r[0] - tol && x < r[1] + tol && y > r[2] - tol && y < r[3] + tol);
Func<List<Line>, List<double[]>, List<Line>> Recortar = (lns, fora) => { var res = new List<Line>();
    foreach (var ln in lns) { var a = ln.GetEndPoint(0); var b = ln.GetEndPoint(1); int n = Math.Max(1, (int)Math.Ceiling(ln.Length / passo)); XYZ ini = null, ult = null;
        for (int i = 0; i <= n; i++) { var q = a + (b - a) * ((double)i / n); bool ok = !Sobre(fora, q.X, q.Y);
            if (ok) { if (ini == null) ini = q; ult = q; }
            if ((!ok || i == n) && ini != null) { if (ult.DistanceTo(ini) > F(0.05)) res.Add(Line.CreateBound(ini, ult)); ini = null; } } }
    return res; };
// pegada de piso/telhado pelo esboço (laços externos)
Func<Element, List<Line>> Esboco = e => { var res = new List<Line>();
    var sk = e.GetDependentElements(new ElementClassFilter(typeof(Sketch))).Select(id => document.GetElement(id) as Sketch).FirstOrDefault(); if (sk == null) return res;
    foreach (CurveArray ca in sk.Profile) foreach (Curve c in ca) { var ln = c as Line; if (ln == null) { avisos.Add(e.Name + ": borda curva — projeção à mão"); continue; }
        res.Add(Line.CreateBound(new XYZ(ln.GetEndPoint(0).X, ln.GetEndPoint(0).Y, 0), new XYZ(ln.GetEndPoint(1).X, ln.GetEndPoint(1).Y, 0))); }
    return res; };
// 1) paredes de cima
var linSup = Recortar(Contorno(Uniao(deCima, true)), daVista);
// 2) laje de cima (pisos cujo topo está no nível de cima)
var linLaje = new List<Line>(); double lajeXmax = double.MinValue, lajeXmin = double.MaxValue, lajeYmax = double.MinValue, lajeYmin = double.MaxValue;
if (lvS != null) foreach (Floor fl in new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>()) {
    var bb = fl.get_BoundingBox(null); if (bb == null || Math.Abs(bb.Max.Z - lvS.Elevation) > F(0.15)) continue;
    lajeXmin = Math.Min(lajeXmin, bb.Min.X); lajeXmax = Math.Max(lajeXmax, bb.Max.X); lajeYmin = Math.Min(lajeYmin, bb.Min.Y); lajeYmax = Math.Max(lajeYmax, bb.Max.Y);
    linLaje.AddRange(Esboco(fl)); }
var foraLaje = daVista.Concat(deCima).ToList();
linLaje = Recortar(linLaje, foraLaje);
// 3) cobertura: telhados + platibandas
// só o contorno externo: união das platibandas com a pegada cheia dos telhados (a face interna da platibanda não aparece)
Solid cob = platib.Count > 0 ? Uniao(platib, true) : null;
foreach (var rf in new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType()) {
    var bb = rf.get_BoundingBox(null); if (bb == null || bb.Min.Z < zV + F(2.0)) continue;
    var lns = Esboco(rf); if (lns.Count < 3) continue;
    try { var lp = CurveLoop.Create(lns.Cast<Curve>().ToList()); var s = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lp }, XYZ.BasisZ, 1);
          cob = cob == null ? s : BooleanOperationsUtils.ExecuteBooleanOperation(cob, s, BooleanOperationsType.Union); }
    catch { avisos.Add(rf.Name + ": esboço do telhado com mais de um laço ou aberto — projeção à mão"); }
}
var linCob = Recortar(Contorno(cob), deCima.Concat(daVista).ToList());
// tira da cobertura o que coincide com linhas de laje/paredes já desenhadas
Func<Line, List<Line>, bool> Coincide = (l, outras) => outras.Any(o => { var d0 = o.Project(l.GetEndPoint(0)); var d1 = o.Project(l.GetEndPoint(1)); return d0 != null && d1 != null && d0.Distance < tol && d1.Distance < tol; });
linCob = linCob.Where(l => !Coincide(l, linLaje) && !Coincide(l, linSup)).ToList();
linLaje = linLaje.Where(l => !Coincide(l, linSup)).ToList();
// corpo da casa (para o lado dos textos)
double cx = daVista.Count > 0 ? (daVista.Min(r => r[0]) + daVista.Max(r => r[1])) / 2 : 0, cy = daVista.Count > 0 ? (daVista.Min(r => r[2]) + daVista.Max(r => r[3])) / 2 : 0;
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
document.Application.FailuresProcessing += __avisos;
try {
    using (var t = new Transaction(document, "Skill: projeções")) {
        t.Start();
        if (P(1).ToLower() == "refazer") {
            var velhas = new FilteredElementCollector(document, v.Id).OfClass(typeof(CurveElement)).Cast<CurveElement>().Where(c => c.LineStyle != null && c.LineStyle.Name.ToUpper().StartsWith("VZ-PROJEÇÃO") && !c.LineStyle.Name.ToUpper().Contains("RECÚO") && !c.LineStyle.Name.ToUpper().Contains("RECUO")).Select(c => c.Id).ToList();
            velhas.AddRange(new FilteredElementCollector(document, v.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(x => x.Text.StartsWith("PROJEÇÃO")).Select(x => x.Id));
            if (velhas.Count > 0) document.Delete(velhas); log.AppendLine("apagados: " + velhas.Count);
        }
        Action<List<Line>, GraphicsStyle, string> Desenhar = (lns, g, rot) => {
            foreach (var l in lns) { var dc = document.Create.NewDetailCurve(v, l); if (g != null) dc.LineStyle = g; }
            if (rot == null || lns.Count == 0) return;
            var maior = lns.OrderByDescending(l => l.Length).First(); var a = maior.GetEndPoint(0); var b = maior.GetEndPoint(1);
            bool vert = Math.Abs(maior.Direction.Y) > 0.5; var mid = (a + b) / 2;
            var lado = vert ? new XYZ(Math.Sign(cx - mid.X) == 0 ? -1 : Math.Sign(cx - mid.X), 0, 0) : new XYZ(0, Math.Sign(cy - mid.Y) == 0 ? -1 : Math.Sign(cy - mid.Y), 0);
            double ini = vert ? Math.Min(a.Y, b.Y) : Math.Min(a.X, b.X);
            // a caixa do texto cresce para +X (girado 90°) ou para −Y (horizontal) a partir do ponto de inserção
            double gap = F(0.001 * v.Scale), alt = F(0.0018 * v.Scale) * 1.2;
            var pos = vert ? new XYZ(lado.X < 0 ? mid.X - gap - alt : mid.X + gap, ini + F(0.3), 0)
                           : new XYZ(ini + F(0.3), lado.Y > 0 ? mid.Y + gap + alt : mid.Y - gap, 0);
            var op = new TextNoteOptions(tt.Id) { Rotation = vert ? Math.PI / 2 : 0, HorizontalAlignment = HorizontalTextAlignment.Left };
            TextNote.Create(document, v.Id, pos, rot, op);
        };
        Desenhar(linSup, gSup, null);
        Desenhar(linLaje, gSup, linLaje.Count > 0 ? "PROJEÇÃO PAV. SUPERIOR" : null);
        // cobertura: separa a marquise (trechos fora da laje de cima) do resto
        var marq = linCob.Where(l => { var m = (l.GetEndPoint(0) + l.GetEndPoint(1)) / 2; return lajeXmax > lajeXmin && (m.X > lajeXmax + tol || m.X < lajeXmin - tol || m.Y > lajeYmax + tol || m.Y < lajeYmin - tol); }).ToList();
        var resto = linCob.Except(marq).ToList();
        Desenhar(resto, gCob, resto.Count > 0 ? "PROJEÇÃO COBERTURA" : null);
        Desenhar(marq, gCob, marq.Count > 0 ? "PROJEÇÃO COBERTURA (MARQUISE)" : null);
        log.AppendLine("paredes de cima: " + linSup.Count + " linhas; laje: " + linLaje.Count + "; cobertura: " + resto.Count + " + marquise " + marq.Count);
        t.Commit();
    }
} finally { document.Application.FailuresProcessing -= __avisos; }
if (avisos.Count > 0) log.AppendLine(string.Join("\n", avisos.Distinct()));
log.AppendLine("Confira na imagem: texto sobre cota ou identificador se move à mão; o trecho da cobertura sobre a laje é marquise só se passar da laje.");
return log.ToString();
