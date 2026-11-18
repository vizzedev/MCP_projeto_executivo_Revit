// SKILL EXECUTIVO REVIT — Fases 2 e 8: áreas da implantação (hachuras) + legenda atualizada automaticamente
// send_code_to_revit · transactionMode "none"
// Regras do escritório (SKILL.md › "Área construída, área ocupada e hachuras da implantação"):
//   construída de cada pavimento = contorno pela face externa das alvenarias do corpo principal (sem muros de divisa)
//     + área coberta fora das paredes cuja cobertura avança MAIS de 1,5 m do corpo de onde ELA sai (entra inteira);
//     cobertura = telhado ou laje/piso de nível acima; térreo: candidata = lote; acima: candidata = piso do pavimento.
//   ocupada = união das construídas; permeável = piso de GRAMA dentro do lote; piscina = piso/ambiente "PISCINA" no lote.
// Desenha as regiões (de baixo para cima: permeável, piscina, térreo, superior…) sem "dentes" (saliências ≤ 0,16 × 0,32 m)
// e atualiza a legenda da vista (textos achados pelo conteúdo, não por Id):
//   "…PAVIMENTO TÉRREO" → linha "A = x M²"; "…PAVIMENTO SUPERIOR" → idem; "ÁREA PERMEÁVEL" → "A = x M²" + "%";
//   "ÁREA DA PISCINA" → "A = x M²" (sem piscina: oculta na legenda a amostra e os textos da piscina);
//   bloco "ÁREA DO TERRENO / ÁREA OCUPADA / ÁREA TOTAL CONSTRUÍDA"; corrige "PÁVIMENTO" e a hachura da amostra de cada pavimento.
// Grava também nas Informações do projeto, se existirem: PAV. TÉRREO, PAV. SUPERIOR, ÁREA CONSTRUÍDA, ÁREA OCUPADA,
//   ÁREA PERMEÁVEL, ÁREA PISCINA, TAXA DE OCUPAÇÃO, TAXA DE PERMEABILIDADE.
// parameters[0] = vista das hachuras (padrão: planta com "IMPLANTA" e "LAYOUT" no nome)
// parameters[1] = legenda (padrão: legenda com "IMPLANTA" e "LAYOUT" no nome)
// parameters[2] = "simular" só relata (não desenha nem grava)
// Uma transação "Skill: áreas e legenda da implantação" (Ctrl+Z desfaz). Não salva.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
var doc = document;
Func<int, string> P = i => parameters != null && parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
Func<string, string> Sem = s => new string((s ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
var log = new System.Text.StringBuilder();
bool simular = P(2).ToLower() == "simular";
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
doc.Application.FailuresProcessing += __avisos;
try {
    // ---------- geometria em planta (sólidos finos em z = -100)
    var plano = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, -100));
    Func<Solid, Solid, BooleanOperationsType, Solid> B = (a, b, op) => { if (a == null) return op == BooleanOperationsType.Union ? b : null; if (b == null) return op == BooleanOperationsType.Intersect ? null : a; try { return BooleanOperationsUtils.ExecuteBooleanOperation(a, b, op); } catch { return a; } };
    Func<CurveLoop, Solid> Ext = lo => GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lo }, XYZ.BasisZ, 1.0);
    Func<IEnumerable<Element>, Solid> Proj = els => { Solid u = null;
        foreach (var e in els) foreach (var o in e.get_Geometry(new Options())) { var s = o as Solid; if (s == null || s.Volume < 1e-6) continue;
            try { var f = ExtrusionAnalyzer.Create(s, plano, XYZ.BasisZ).GetExtrusionBase(); u = B(u, GeometryCreationUtilities.CreateExtrusionGeometry(f.GetEdgesAsCurveLoops(), XYZ.BasisZ, 1.0), BooleanOperationsType.Union); } catch { } }
        return u; };
    Func<Solid, List<Face>> Bases = s => { var r = new List<Face>(); if (s == null) return r; foreach (Face f in s.Faces) if (f.ComputeNormal(new UV(0.5, 0.5)).Z < -0.99) r.Add(f); return r; };
    Func<Solid, double> A = s => Bases(s).Sum(f => f.Area) * 0.092903;
    Func<Solid, Solid> Cheio = s => { Solid r = null; foreach (var f in Bases(s)) r = B(r, Ext(f.GetEdgesAsCurveLoops().OrderByDescending(l => l.GetExactLength()).First()), BooleanOperationsType.Union); return r; };
    Func<CurveLoop, double, CurveLoop> Off = (lo, d) => { CurveLoop best = null; double am = 0; foreach (var sg in new[] { 1.0, -1.0 }) { try { var o = CurveLoop.CreateViaOffset(lo, sg * d, XYZ.BasisZ); double a = A(Ext(o)); if (a > am) { am = a; best = o; } } catch { } } return best; };
    Func<Solid, double, Solid> Faixa = (s, d) => { Solid r = null; foreach (var f in Bases(s)) { var o = Off(f.GetEdgesAsCurveLoops().OrderByDescending(l => l.GetExactLength()).First(), d); if (o != null) r = B(r, Ext(o), BooleanOperationsType.Union); } return r; };
    // ---------- lote
    var props = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_SiteProperty).OfClass(typeof(PropertyLine)).ToElements()
        .Where(e => !((e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "").ToUpper().Contains("RECUO"))).ToList();
    if (props.Count == 0) return "ERRO: sem linha de propriedade do lote.";
    var lote = props.OrderByDescending(e => e.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0).First();
    var lbb = lote.get_BoundingBox(null); double aLote = (lote.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0) * 0.092903;
    var lp = new List<XYZ>(); foreach (var o in lote.get_Geometry(new Options())) { var c = o as Curve; if (c != null) { var p = c.GetEndPoint(0); lp.Add(new XYZ(p.X, p.Y, -100)); } }
    var cl = new CurveLoop(); for (int i = 0; i < lp.Count; i++) cl.Append(Line.CreateBound(lp[i], lp[(i + 1) % lp.Count]));
    var sLote = Ext(cl);
    var pLote = doc.ProjectInformation.LookupParameter("Área do lote");
    if (pLote != null && pLote.AsDouble() > 0 && Math.Abs(pLote.AsDouble() * 0.092903 - aLote) > 0.05) log.AppendLine("⚠ Área do lote no carimbo (" + (pLote.AsDouble() * 0.092903).ToString("0.00") + ") difere da linha de divisa (" + aLote.ToString("0.00") + ") — usei a da divisa");
    // ---------- muros de divisa (não são corpo principal)
    Func<Wall, bool> Muro = w => { var bb = w.get_BoundingBox(null); if (bb == null || (bb.Max.Z - bb.Min.Z) * 0.3048 > 2.6) return false; var lc = (w.Location as LocationCurve)?.Curve; if (lc == null) return false; var a = lc.GetEndPoint(0); var b = lc.GetEndPoint(1); double tol = F(0.40);
        return (Math.Abs(a.X - b.X) < F(0.01) && (Math.Abs(a.X - lbb.Min.X) < tol || Math.Abs(a.X - lbb.Max.X) < tol)) || (Math.Abs(a.Y - b.Y) < F(0.01) && (Math.Abs(a.Y - lbb.Min.Y) < tol || Math.Abs(a.Y - lbb.Max.Y) < tol)); };
    var paredes = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().Where(w => !Muro(w)).ToList();
    var pisos = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();
    var telhados = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType().ToList();
    var pavs = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Where(l => Sem(l.Name).StartsWith("PAV.") && Sem(l.Name).EndsWith("N.A") && paredes.Any(w => w.LevelId == l.Id)).OrderBy(l => l.Elevation).ToList();
    if (pavs.Count == 0) return "ERRO: nenhum pavimento \"PAV. X N.A\" com paredes.";
    Func<Level, string> Rot = l => { var n = Sem(l.Name); return n.Contains("TERREO") ? "Térreo" : n.Contains("SUPERIOR") ? "Superior" : l.Name.Substring(4, l.Name.Length - 7).Trim(); };
    var corpo = pavs.ToDictionary(l => l, l => Cheio(Proj(paredes.Where(w => w.LevelId == l.Id))));
    // cobertura válida sobre cada pavimento: cada telhado/laje medido a partir do corpo de onde ELE sai (pavimento logo abaixo)
    Func<Solid, Solid, string, Solid> Valida = (cob, apoio, nome) => { var val = B(cob, apoio, BooleanOperationsType.Intersect); var fx = Faixa(apoio, F(1.5));
        foreach (var f in Bases(B(cob, apoio, BooleanOperationsType.Difference))) { if (f.Area * 0.092903 < 0.05) continue; var comp = GeometryCreationUtilities.CreateExtrusionGeometry(f.GetEdgesAsCurveLoops(), XYZ.BasisZ, 1.0);
            double alem = A(B(comp, fx, BooleanOperationsType.Difference)); bool entra = alem > 0.05;
            log.AppendLine("  " + nome + ": peça de " + (f.Area * 0.092903).ToString("0.00") + " m² além do corpo, " + alem.ToString("0.00") + " m² além de 1,5 m → " + (entra ? "ENTRA" : "fica fora"));
            if (entra) val = B(val, comp, BooleanOperationsType.Union); }
        return val; };
    Func<double, Level> CorpoAbaixo = z => pavs.Where(l => l.Elevation < z - F(0.5)).OrderByDescending(l => l.Elevation).FirstOrDefault();
    var cobs = new List<Tuple<double, Solid>>();   // (elevação da cobertura, área válida)
    foreach (var lv in pavs.Skip(1)) { var apoio = CorpoAbaixo(lv.Elevation); var laje = Proj(pisos.Where(f => f.LevelId == lv.Id)); if (apoio != null && laje != null) cobs.Add(Tuple.Create(lv.Elevation, Valida(laje, corpo[apoio], "laje do " + Rot(lv) + " (sai do " + Rot(apoio) + ")"))); }
    foreach (var g in telhados.GroupBy(r => r.LevelId)) { var z = ((Level)doc.GetElement(g.Key)).Elevation; var apoio = CorpoAbaixo(z) ?? pavs.Last(); cobs.Add(Tuple.Create(z, Valida(Proj(g), corpo[apoio], "telhado/marquise (sai do " + Rot(apoio) + ")"))); }
    // ---------- construída por pavimento
    var construida = new List<Tuple<Level, Solid>>();
    for (int i = 0; i < pavs.Count; i++) {
        var lv = pavs[i]; Solid acima = null; foreach (var c in cobs.Where(c => c.Item1 > lv.Elevation + F(0.5))) acima = B(acima, c.Item2, BooleanOperationsType.Union);
        var cand = i == 0 ? sLote : B(Proj(pisos.Where(f => f.LevelId == lv.Id)), sLote, BooleanOperationsType.Intersect);
        var s = B(corpo[lv], B(B(cand, acima, BooleanOperationsType.Intersect), corpo[lv], BooleanOperationsType.Difference), BooleanOperationsType.Union);
        Solid lim = null; foreach (var f in Bases(s)) if (f.Area * 0.092903 >= 0.5) lim = B(lim, GeometryCreationUtilities.CreateExtrusionGeometry(f.GetEdgesAsCurveLoops(), XYZ.BasisZ, 1.0), BooleanOperationsType.Union);
        construida.Add(Tuple.Create(lv, lim));
    }
    var perm = B(Proj(pisos.Where(f => Sem(f.Name).Contains("GRAMA"))), sLote, BooleanOperationsType.Intersect);
    var pisc = B(Proj(pisos.Where(f => Sem(f.Name).Contains("PISCINA"))), sLote, BooleanOperationsType.Intersect);
    // ---------- contornos sem "dentes"
    double raso = F(0.16), estreito = F(0.32), tolp = F(0.002);
    Func<List<XYZ>, List<XYZ>> Colin = pts => { var r = new List<XYZ>(pts); bool m = true; while (m && r.Count > 3) { m = false; for (int i = 0; i < r.Count; i++) { var a = r[(i - 1 + r.Count) % r.Count]; var b = r[i]; var c = r[(i + 1) % r.Count]; if (b.DistanceTo(a) < tolp || Math.Abs((b - a).CrossProduct(c - b).Z) < 1e-6) { r.RemoveAt(i); m = true; break; } } } return r; };
    Func<List<XYZ>, List<XYZ>> Dentes = pts => { var r = Colin(pts); bool m = true; while (m && r.Count > 4) { m = false;
        for (int i = 0; i < r.Count; i++) { var p1 = r[i]; var p2 = r[(i + 1) % r.Count]; var p3 = r[(i + 2) % r.Count]; var p4 = r[(i + 3) % r.Count]; var d1 = p2 - p1; var d3 = p4 - p3;
            if (d1.GetLength() <= raso && d3.GetLength() <= raso && (p3 - p2).GetLength() <= estreito && d1.Normalize().DotProduct(d3.Normalize()) < -0.99 && Math.Abs(d1.GetLength() - d3.GetLength()) < tolp) {
                int i2 = (i + 1) % r.Count, i3 = (i + 2) % r.Count; r.RemoveAt(Math.Max(i2, i3)); r.RemoveAt(Math.Min(i2, i3)); r = Colin(r); m = true; break; } } } return r; };
    Func<Solid, double, List<CurveLoop>> Loops = (s, z) => { var res = new List<CurveLoop>(); foreach (var f in Bases(s)) foreach (var lo in f.GetEdgesAsCurveLoops()) {
        var pts = Dentes(lo.Select(c => new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, z)).ToList()); var n = new CurveLoop(); for (int i = 0; i < pts.Count; i++) n.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count])); res.Add(n); } return res; };
    Func<List<CurveLoop>, double> AL = ls => { try { return GeometryCreationUtilities.CreateExtrusionGeometry(ls.Select(l => { var n = new CurveLoop(); foreach (Curve c in l) n.Append(c.CreateTransformed(Transform.CreateTranslation(new XYZ(0, 0, -100 - c.GetEndPoint(0).Z)))); return n; }).ToList(), XYZ.BasisZ, 1.0).Volume * 0.092903; } catch { return 0; } };
    // ---------- vista e legenda
    var vista = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate && (P(0) != "" ? v.Name == P(0) : Sem(v.Name).Contains("IMPLANTA") && Sem(v.Name).Contains("LAYOUT")));
    if (vista == null) return "ERRO: vista das hachuras não encontrada (" + P(0) + ").";
    var legenda = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v => v.ViewType == ViewType.Legend && (P(1) != "" ? v.Name == P(1) : Sem(v.Name).Contains("IMPLANTA") && Sem(v.Name).Contains("LAYOUT")));
    double zv = vista.GenLevel != null ? vista.GenLevel.Elevation : 0;
    var tiposR = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().ToList();
    Func<string, FilledRegionType> TipoR = trecho => tiposR.FirstOrDefault(t => Sem(t.Name).Contains(Sem(trecho)));
    var cult = new System.Globalization.CultureInfo("pt-BR"); Func<double, string> N = x => x.ToString("0.00", cult);
    var areas = new Dictionary<string, double>();
    using (var t = new Transaction(doc, "Skill: áreas e legenda da implantação")) {
        t.Start();
        // apaga as regiões de área anteriores da vista e redesenha de baixo para cima
        doc.Delete(new FilteredElementCollector(doc, vista.Id).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().Where(r => { var n = Sem(doc.GetElement(r.GetTypeId()).Name); return n.Contains("AREA CONSTRUIDA") || n.Contains("AREA PERMEAVEL") || n.Contains("PISCINA") || n.Contains("AREA OCUPADA"); }).Select(r => r.Id).ToList());
        Action<string, Solid, string> Desenha = (chave, s, tipoTrecho) => { if (s == null) { areas[chave] = 0; return; } var ls = Loops(s, zv); areas[chave] = AL(ls); var tp = TipoR(tipoTrecho);
            if (tp == null) { log.AppendLine("⚠ tipo de região \"" + tipoTrecho + "\" não existe no template"); return; } FilledRegion.Create(doc, tp.Id, vista.Id, ls); };
        Desenha("PERMEAVEL", perm, "Área Permeável");
        Desenha("PISCINA", pisc, "Piscina");
        Solid uniao = null;
        foreach (var c in construida) { Desenha("PAV:" + Rot(c.Item1), c.Item2, "Área Construída Pavimento " + Rot(c.Item1)); uniao = B(uniao, c.Item2, BooleanOperationsType.Union); }
        double ocup = AL(Loops(uniao, -100)), constr = construida.Sum(c => areas["PAV:" + Rot(c.Item1)]);
        areas["OCUPADA"] = ocup; areas["CONSTRUIDA"] = constr;
        // ----- legenda
        if (legenda != null) {
            var textos = new FilteredElementCollector(doc, legenda.Id).OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
            Func<TextNote, TextNote> Valor = rot => textos.Where(x => x.Id != rot.Id && Sem(x.Text).TrimStart().StartsWith("A =") && x.Coord.Y < rot.Coord.Y + F(0.01)).OrderBy(x => Math.Abs(x.Coord.Y - rot.Coord.Y) + Math.Abs(x.Coord.X - rot.Coord.X) * 2).FirstOrDefault();
            foreach (var x in textos.Where(x => x.Text.Contains("PÁVIMENTO"))) x.Text = x.Text.Replace("PÁVIMENTO", "PAVIMENTO").TrimEnd('\r', '\n');
            foreach (var c in construida) { var rot = textos.FirstOrDefault(x => Sem(x.Text).Contains("PAVIMENTO " + Sem(Rot(c.Item1)))); var val = rot != null ? Valor(rot) : null;
                if (val != null) val.Text = "A = " + N(areas["PAV:" + Rot(c.Item1)]) + " M²"; else log.AppendLine("⚠ legenda: sem texto do pavimento " + Rot(c.Item1));
                // amostra com a hachura certa: região da legenda mais próxima à esquerda do rótulo
                var tp = TipoR("Área Construída Pavimento " + Rot(c.Item1));
                if (rot != null && tp != null) { var am = new FilteredElementCollector(doc, legenda.Id).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().Where(r => r.get_BoundingBox(legenda) != null && r.get_BoundingBox(legenda).Max.X <= rot.Coord.X + F(0.01)).OrderBy(r => { var b = r.get_BoundingBox(legenda); return Math.Abs((b.Min.Y + b.Max.Y) / 2 - rot.Coord.Y) + (rot.Coord.X - b.Max.X); }).FirstOrDefault();
                    if (am != null && am.GetTypeId() != tp.Id) { am.ChangeTypeId(tp.Id); log.AppendLine("legenda: amostra do " + Rot(c.Item1) + " corrigida para " + tp.Name); } } }
            var rp = textos.FirstOrDefault(x => Sem(x.Text).Contains("AREA PERMEAVEL"));
            if (rp != null) { rp.Text = "ÁREA PERMEÁVEL"; var vp = Valor(rp); if (vp != null) vp.Text = "A = " + N(areas["PERMEAVEL"]) + " M²\r" + N(areas["PERMEAVEL"] / aLote * 100) + "%"; }
            var rpi = textos.FirstOrDefault(x => Sem(x.Text).Contains("PISCINA"));
            if (rpi != null) { var vpi = Valor(rpi);
                var amPi = new FilteredElementCollector(doc, legenda.Id).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().Where(r => Sem(doc.GetElement(r.GetTypeId()).Name).Contains("PISCINA")).Select(r => r.Id).ToList();
                var ids = new List<ElementId> { rpi.Id }; if (vpi != null) ids.Add(vpi.Id); ids.AddRange(amPi);
                if (areas["PISCINA"] < 0.01) { legenda.HideElements(ids); log.AppendLine("legenda: sem piscina no projeto → amostra e textos da piscina ocultos"); }
                else { legenda.UnhideElements(ids); if (vpi != null) vpi.Text = "A = " + N(areas["PISCINA"]) + " M²"; } }
            var rs = textos.FirstOrDefault(x => Sem(x.Text).Contains("AREA DO TERRENO"));
            if (rs != null) rs.Text = "ÁREA DO TERRENO:            " + N(aLote) + " m²\r\rÁREA OCUPADA:                 " + N(ocup) + "m²     " + N(ocup / aLote * 100) + " %\r\rÁREA TOTAL CONSTRUÍDA: " + N(constr) + "m²";
        } else log.AppendLine("⚠ legenda da implantação de layout não encontrada");
        // ----- Informações do projeto
        var pi = doc.ProjectInformation;
        Action<string, double, bool> Grava = (nome, val, area) => { var p = pi.LookupParameter(nome); if (p == null || p.IsReadOnly || p.StorageType != StorageType.Double) return; p.Set(area ? val / 0.092903 : val); };
        foreach (var c in construida) Grava("PAV. " + Sem(Rot(c.Item1)).Replace("TERREO", "TÉRREO"), areas["PAV:" + Rot(c.Item1)], true);
        Grava("ÁREA CONSTRUÍDA", constr, true); Grava("ÁREA OCUPADA", ocup, true); Grava("ÁREA PERMEÁVEL", areas["PERMEAVEL"], true); Grava("ÁREA PISCINA", areas["PISCINA"], true);
        Grava("TAXA DE OCUPAÇÃO", Math.Round(ocup / aLote * 100, 2), false); Grava("TAXA DE PERMEABILIDADE", Math.Round(areas["PERMEAVEL"] / aLote * 100, 2), false);
        if (simular) t.RollBack(); else t.Commit();
    }
    log.AppendLine("Lote " + N(aLote) + " m² | " + string.Join(" | ", construida.Select(c => Rot(c.Item1) + " " + N(areas["PAV:" + Rot(c.Item1)]))) + " | total construída " + N(areas["CONSTRUIDA"]) + " | ocupada " + N(areas["OCUPADA"]) + " (" + N(areas["OCUPADA"] / aLote * 100) + "%) | permeável " + N(areas["PERMEAVEL"]) + " (" + N(areas["PERMEAVEL"] / aLote * 100) + "%)" + (areas["PISCINA"] > 0 ? " | piscina " + N(areas["PISCINA"]) : " | sem piscina") + (simular ? " — SIMULAÇÃO, nada gravado" : ""));
    return log.ToString();
} finally { doc.Application.FailuresProcessing -= __avisos; }
