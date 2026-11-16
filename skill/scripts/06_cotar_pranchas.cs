// SKILL EXECUTIVO REVIT — Fase 6: cotas de cada prancha, seguindo o conteúdo da prancha
// send_code_to_revit · transactionMode "none"
// Regras do escritório:
//   executivo   — fica no 06_anotar_planta.cs (cadeia integrada + total do lote por fora dos muros, nas 4 faces)
//   layout      — medidas internas de cada ambiente (largura e comprimento), tipo de cota só com texto
//   tomadas     — ao longo de cada parede com tomada: limite da parede → tomada → tomada → limite da parede
//   iluminacao  — por ambiente, eixo e fileira: parede → luminária → luminária → parede, com a linha passando pela fileira; linear pelas pontas
//   cobertura   — por fora, nas 4 faces: contorno do telhado com corpo × beiral (faces externas da edificação) + total
//   corte       — só na vertical: uma cadeia por ambiente cortado (piso acabado → forro → laje acima dele, com a espessura)
//                 + linha à esquerda com o piso acabado de cada pavimento e o topo (pé-direito por pavimento + total)
// parameters[0] = vistas separadas por ";" (obrigatório)
// parameters[1] = modo: auto (padrão) | layout | tomadas | iluminacao | cobertura | corte
//                 auto = pelo nome do modelo de vista e da vista (LAYOUT, TOMADA/PONTOS ELÉTRICOS, ILUMINAÇÃO/LUMINOTÉCNICO, COBERTURA, corte)
// parameters[2] = "refazer" apaga as cotas lineares da vista antes (sem isso, vista que já tem cota linear é pulada)
// parameters[3] = nome do tipo de cota (opcional; padrão: layout "VZ-Cota Interna", demais "VZ-Cota 1-<escala mais próxima>")
// Uma transação "Skill: cotar pranchas" (Ctrl+Z desfaz). Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var nomes = P(0).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
if (nomes.Count == 0) return "ERRO: informe as vistas em parameters[0].";
var modoPedido = P(1) == "" ? "auto" : P(1).ToLower();
bool refazer = P(2).ToLower() == "refazer";
var tiposL = new FilteredElementCollector(document).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(d => d.StyleType == DimensionStyleType.Linear).ToList();
Func<DimensionType, int> EscTipo = d => { var m = System.Text.RegularExpressions.Regex.Match(d.Name.Replace("/", "-"), @"1-(\d+)\s*$"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; };
Func<string, string> SemAcento = s => new string(s.Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
Func<View, string> Modo = v => {
    if (modoPedido != "auto") return modoPedido;
    var tn = v.ViewTemplateId != ElementId.InvalidElementId ? SemAcento(document.GetElement(v.ViewTemplateId).Name) : ""; var n = SemAcento(v.Name) + " " + tn;
    if (v.ViewType == ViewType.Section) return "corte";
    if (n.Contains("LAYOUT") || n.Contains("SETORIZA")) return "layout";
    if (n.Contains("TOMADA") || n.Contains("PONTOS ELETRICOS")) return "tomadas";
    if (n.Contains("ILUMINA") || n.Contains("LUMINOTEC")) return "iluminacao";
    if (n.Contains("COBERTURA")) return "cobertura";
    return "";
};
Func<View, string, DimensionType> Tipo = (v, modo) => {
    if (P(3) != "") return tiposL.FirstOrDefault(d => d.Name == P(3));
    if (modo == "layout") return tiposL.FirstOrDefault(d => d.Name.ToUpper().Contains("LAYOUT")) ?? tiposL.FirstOrDefault(d => d.Name == "VZ-Cota Interna");
    var comuns = tiposL.Where(d => EscTipo(d) > 0 && !d.Name.ToUpper().Contains("EXECUTIVO") && !d.Name.ToUpper().Contains("AZUL")).ToList();
    if (comuns.Count == 0) comuns = tiposL.Where(d => EscTipo(d) > 0).ToList();
    return comuns.OrderBy(d => Math.Abs(EscTipo(d) - v.Scale)).ThenBy(d => EscTipo(d)).FirstOrDefault() ?? tiposL.FirstOrDefault(d => d.Name.StartsWith("VZ"));
};
// face lateral (PlanarFace com referência) de uma parede que passa sobre um segmento de contorno de ambiente
Func<Wall, Line, Reference> FaceSobre = (w, seg) => {
    foreach (var lado in new[] { ShellLayerType.Interior, ShellLayerType.Exterior })
        foreach (var rf in HostObjectUtils.GetSideFaces(w, lado)) {
            var face = w.GetGeometryObjectFromReference(rf) as PlanarFace; if (face == null) continue;
            foreach (var tpar in new[] { 0.5, 0.15, 0.85, 0.3, 0.7, 0.05, 0.95 }) {
                var meio = seg.Evaluate(tpar, true); var pr = face.Project(new XYZ(meio.X, meio.Y, face.Origin.Z));
                if (pr != null && pr.Distance < F(0.01)) return rf; }
        }
    return null;
};
// referência do plano central de uma família, perpendicular ao eixo pedido
Func<FamilyInstance, XYZ, Reference> RefCentro = (fi, eixo) => {
    var tr = fi.GetTransform();
    var tipo = Math.Abs(tr.BasisX.DotProduct(eixo)) > 0.99 ? FamilyInstanceReferenceType.CenterLeftRight : Math.Abs(tr.BasisY.DotProduct(eixo)) > 0.99 ? FamilyInstanceReferenceType.CenterFrontBack : (FamilyInstanceReferenceType?)null;
    if (tipo == null) return null;
    var refs = fi.GetReferences(tipo.Value); return refs.Count > 0 ? refs[0] : null;
};
Func<XYZ, XYZ, double> Coord = (p, eixo) => p.DotProduct(eixo);
var bOpt = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };

using (var t = new Transaction(document, "Skill: cotar pranchas")) {
    t.Start();
    foreach (var nome in nomes) {
        var vista = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v => !v.IsTemplate && v.Name == nome);
        if (vista == null) { log.AppendLine(nome + ": vista não encontrada"); continue; }
        var modo = Modo(vista); if (modo == "") { log.AppendLine(nome + ": modo não reconhecido (informe em parameters[1])"); continue; }
        var tipo = Tipo(vista, modo); double esc = vista.Scale;
        var existentes = new FilteredElementCollector(document, vista.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Where(d => !(d is SpotDimension)).Select(d => d.Id).ToList();
        if (existentes.Count > 0) { if (refazer) document.Delete(existentes); else { log.AppendLine(nome + " [" + modo + "]: já tem " + existentes.Count + " cotas — pulada (use \"refazer\")"); continue; } }
        int n = 0, falhas = 0, descartadas = 0; var criadas = new List<Dimension>(); var avisos = new List<string>();
        var nivel = (vista as ViewPlan)?.GenLevel; double z = nivel != null ? nivel.Elevation : 0;
        // cria a cota; se o Revit recusar o conjunto, testa referência a referência e refaz só com as aceitas
        Func<List<Reference>, Line, Dimension> Cotar = (refs, linha) => {
            if (refs.Count < 2) return null;
            var ra = new ReferenceArray(); foreach (var r in refs) ra.Append(r);
            try { var d = tipo != null ? document.Create.NewDimension(vista, linha, ra, tipo) : document.Create.NewDimension(vista, linha, ra); if (d != null) { criadas.Add(d); n++; } return d; } catch { }
            var boas = new List<Reference> { refs[0] };
            for (int i = 1; i < refs.Count; i++) using (var st = new SubTransaction(document)) { st.Start(); bool ok = false;
                try { var rr = new ReferenceArray(); rr.Append(refs[0]); rr.Append(refs[i]); ok = document.Create.NewDimension(vista, linha, rr) != null; } catch { }
                st.RollBack(); if (ok) boas.Add(refs[i]); else descartadas++; }
            if (boas.Count < 2) { falhas++; return null; }
            var rb = new ReferenceArray(); foreach (var r in boas) rb.Append(r);
            try { var d = tipo != null ? document.Create.NewDimension(vista, linha, rb, tipo) : document.Create.NewDimension(vista, linha, rb); if (d != null) { criadas.Add(d); n++; } return d; } catch { falhas++; return null; }
        };
        // ordena por coordenada e junta referências a menos de 1 cm
        Func<List<Tuple<double, Reference>>, List<Tuple<double, Reference>>> Limpa = lst => {
            var res = new List<Tuple<double, Reference>>();
            foreach (var r in lst.Where(q => q.Item2 != null).OrderBy(q => q.Item1)) { if (res.Count > 0 && r.Item1 - res[res.Count - 1].Item1 < F(0.01)) continue; res.Add(r); }
            return res;
        };
        var rooms = nivel == null ? new List<Autodesk.Revit.DB.Architecture.Room>() : new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
            .Where(r => r.Area > 0 && r.LevelId == nivel.Id).ToList();
        // segmentos do contorno de um ambiente perpendiculares a um eixo (paredes), com a coordenada no eixo
        Func<Autodesk.Revit.DB.Architecture.Room, XYZ, List<Tuple<double, Line, Wall>>> Perp = (r, eixo) => {
            var res = new List<Tuple<double, Line, Wall>>(); var segs = r.GetBoundarySegments(bOpt); if (segs == null) return res;
            foreach (var lp in segs) foreach (var s in lp) { var l = s.GetCurve() as Line; var w = document.GetElement(s.ElementId) as Wall; if (l == null || w == null) continue;
                if (Math.Abs(l.Direction.DotProduct(eixo)) < 0.01) res.Add(Tuple.Create(Coord(l.GetEndPoint(0), eixo), l, w)); }
            return res;
        };
        // o segmento perpendicular mais próximo antes (lado -1) ou depois (lado +1) de uma coordenada, que cruza a transversal t0
        Func<List<Tuple<double, Line, Wall>>, double, int, XYZ, double, Reference> Limite = (perp, c, lado, transv, t0) => {
            var cand = perp.Where(p => lado < 0 ? p.Item1 <= c + F(0.001) : p.Item1 >= c - F(0.001)).Where(p => {
                double a = Coord(p.Item2.GetEndPoint(0), transv), b = Coord(p.Item2.GetEndPoint(1), transv); return t0 >= Math.Min(a, b) - F(0.05) && t0 <= Math.Max(a, b) + F(0.05); })
                .OrderBy(p => Math.Abs(p.Item1 - c)).ToList();
            foreach (var p in cand) { var rf = FaceSobre(p.Item3, p.Item2); if (rf != null) return rf; }
            return null;
        };

        if (modo == "layout") {
            foreach (var r in rooms) {
                var bb = r.get_BoundingBox(null); if (bb == null) continue; var c = (bb.Min + bb.Max) / 2;
                foreach (var eixo in new[] { XYZ.BasisX, XYZ.BasisY }) {
                    var transv = eixo.IsAlmostEqualTo(XYZ.BasisX) ? XYZ.BasisY : XYZ.BasisX;
                    double t0 = Coord(bb.Min, transv) + (Coord(bb.Max, transv) - Coord(bb.Min, transv)) * 0.35;   // fora do centro (identificador)
                    var perp = Perp(r, eixo); if (perp.Count < 2) continue;
                    var r1 = Limite(perp, Coord(bb.Min, eixo), +1, transv, t0); var r2 = Limite(perp, Coord(bb.Max, eixo), -1, transv, t0);
                    if (r1 == null || r2 == null) { avisos.Add(r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() + ": contorno sem parede num dos lados"); continue; }
                    var a = eixo.Multiply(Coord(bb.Min, eixo)) + transv.Multiply(t0) + XYZ.BasisZ * z; var b = eixo.Multiply(Coord(bb.Max, eixo)) + transv.Multiply(t0) + XYZ.BasisZ * z;
                    Cotar(new List<Reference> { r1, r2 }, Line.CreateBound(a, b));
                }
            }
        }
        else if (modo == "tomadas" || modo == "iluminacao") {
            var cat = modo == "tomadas" ? BuiltInCategory.OST_ElectricalFixtures : BuiltInCategory.OST_LightingFixtures;
            var pecas = new FilteredElementCollector(document, vista.Id).OfCategory(cat).WhereElementIsNotElementType().OfType<FamilyInstance>().ToList();
            if (modo == "tomadas") pecas.AddRange(new FilteredElementCollector(document, vista.Id).OfCategory(BuiltInCategory.OST_LightingDevices).WhereElementIsNotElementType().OfType<FamilyInstance>());   // interruptores
            Func<FamilyInstance, XYZ> Pt = fi => (fi.Location as LocationPoint)?.Point;
            Func<FamilyInstance, Autodesk.Revit.DB.Architecture.Room> Amb = fi => { var p = Pt(fi); if (p == null || nivel == null) return null;
                return rooms.FirstOrDefault(r => r.IsPointInRoom(new XYZ(p.X, p.Y, z + F(1.0)))) ?? rooms.FirstOrDefault(r => r.IsPointInRoom(new XYZ(p.X - fi.FacingOrientation.X * F(0.1), p.Y - fi.FacingOrientation.Y * F(0.1), z + F(1.0))))
                    ?? rooms.FirstOrDefault(r => r.IsPointInRoom(new XYZ(p.X + fi.FacingOrientation.X * F(0.1), p.Y + fi.FacingOrientation.Y * F(0.1), z + F(1.0)))); };
            int semAmb = 0;
            if (modo == "tomadas") {
                // agrupa por ambiente + parede (direção da face): uma cadeia por trecho de parede com tomadas
                var grupos = pecas.Where(fi => Pt(fi) != null).GroupBy(fi => { var r = Amb(fi); var f = fi.FacingOrientation; return (r == null ? "-" : r.Id.ToString()) + "|" + Math.Round(f.X, 2) + "|" + Math.Round(f.Y, 2) + "|" + Math.Round(Coord(Pt(fi), new XYZ(f.X, f.Y, 0).Normalize()) * 0.3048 * 10) ; });
                foreach (var g in grupos) {
                    var fi0 = g.First(); var r = Amb(fi0); if (r == null) { semAmb += g.Count(); continue; }
                    var nrm = new XYZ(fi0.FacingOrientation.X, fi0.FacingOrientation.Y, 0); if (nrm.GetLength() < 0.5) continue; nrm = nrm.Normalize();
                    var eixo = new XYZ(-nrm.Y, nrm.X, 0); if (Math.Abs(eixo.X) < 0.99 && Math.Abs(eixo.Y) < 0.99) { avisos.Add("parede inclinada — pulada"); continue; }
                    eixo = Math.Abs(eixo.X) > 0.99 ? XYZ.BasisX : XYZ.BasisY;
                    var refsF = g.Select(fi => Tuple.Create(Coord(Pt(fi), eixo), RefCentro(fi, eixo))).ToList();
                    if (refsF.Any(q => q.Item2 == null)) avisos.Add(g.Count(fi => RefCentro(fi, eixo) == null) + " peça(s) sem plano central de referência");
                    var perp = Perp(r, eixo); double t0 = Coord(Pt(fi0), nrm) + F(0.05);
                    var minC = refsF.Min(q => q.Item1); var maxC = refsF.Max(q => q.Item1);
                    var r1 = Limite(perp, minC, -1, nrm, t0); var r2 = Limite(perp, maxC, +1, nrm, t0);
                    var lst = new List<Tuple<double, Reference>>(refsF); if (r1 != null) lst.Add(Tuple.Create(minC - F(0.0001) - F(1000), r1)); if (r2 != null) lst.Add(Tuple.Create(maxC + F(1000), r2));
                    var cad = Limpa(lst); if (r1 == null || r2 == null) avisos.Add("tomada(s) em " + r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() + ": limite de parede não encontrado num dos lados");
                    double off = F(0.004 * esc);   // 4 mm no papel para dentro do ambiente, colada à parede da tomada
                    var perpC = Perp(r, eixo); double ini = perpC.Count > 0 ? perpC.Min(p => p.Item1) : minC - F(0.5), fim = perpC.Count > 0 ? perpC.Max(p => p.Item1) : maxC + F(0.5);
                    var basep = nrm.Multiply(t0 - F(0.05) + off) + XYZ.BasisZ * z;
                    Cotar(cad.Select(q => q.Item2).ToList(), Line.CreateBound(basep + eixo.Multiply(Math.Min(ini, minC)), basep + eixo.Multiply(Math.Max(fim, maxC))));
                }
            } else {
                // iluminação (padrão do projeto de referência): por ambiente e por eixo, uma cadeia por FILEIRA de luminárias, com a linha
                // passando pela fileira; parede → luminária → luminária → parede. Luminária linear (perfil, trilho, > 0,60 m) é cotada pelas pontas.
                Func<FamilyInstance, XYZ, List<Tuple<double, Reference>>> RefsPeca = (fi, eixo) => {
                    var res = new List<Tuple<double, Reference>>(); var bbf = fi.get_BoundingBox(null); var tr = fi.GetTransform();
                    double comp = bbf == null ? 0 : Math.Abs(Coord(bbf.Max, eixo) - Coord(bbf.Min, eixo));
                    if (bbf != null && comp > F(0.60)) {
                        bool porX = Math.Abs(tr.BasisX.DotProduct(eixo)) > 0.99, porY = Math.Abs(tr.BasisY.DotProduct(eixo)) > 0.99;
                        if (porX || porY) { var r1 = fi.GetReferences(porX ? FamilyInstanceReferenceType.Left : FamilyInstanceReferenceType.Front); var r2 = fi.GetReferences(porX ? FamilyInstanceReferenceType.Right : FamilyInstanceReferenceType.Back);
                            if (r1.Count > 0 && r2.Count > 0) { double c1 = Coord(bbf.Min, eixo), c2 = Coord(bbf.Max, eixo); bool inv = Coord(tr.BasisX, eixo) < 0 && porX || Coord(tr.BasisY, eixo) < 0 && porY;
                                res.Add(Tuple.Create(inv ? c2 : c1, r1[0])); res.Add(Tuple.Create(inv ? c1 : c2, r2[0])); return res; } }
                    }
                    var rc = RefCentro(fi, eixo); if (rc != null) res.Add(Tuple.Create(Coord(Pt(fi), eixo), rc)); return res;
                };
                foreach (var g in pecas.Where(fi => Pt(fi) != null).GroupBy(fi => { var r = Amb(fi); return r == null ? "-" : r.Id.ToString(); })) {
                    if (g.Key == "-") { semAmb += g.Count(); continue; }
                    var r = rooms.First(x => x.Id.ToString() == g.Key); var bb = r.get_BoundingBox(null);
                    foreach (var eixo in new[] { XYZ.BasisX, XYZ.BasisY }) {
                        var transv = eixo.IsAlmostEqualTo(XYZ.BasisX) ? XYZ.BasisY : XYZ.BasisX;
                        // fileiras: luminárias com a mesma coordenada transversal (tolerância 0,30 m)
                        var fileiras = new List<List<FamilyInstance>>();
                        foreach (var fi in g.OrderBy(x => Coord(Pt(x), transv))) { var ult = fileiras.LastOrDefault(); if (ult != null && Coord(Pt(fi), transv) - Coord(Pt(ult.Last()), transv) < F(0.30)) ult.Add(fi); else fileiras.Add(new List<FamilyInstance> { fi }); }
                        foreach (var fil in fileiras) {
                            var refsF = fil.SelectMany(fi => RefsPeca(fi, eixo)).ToList(); if (refsF.Count == 0) continue;
                            double t0 = fil.Average(fi => Coord(Pt(fi), transv));
                            var perp = Perp(r, eixo); var minC = refsF.Min(q => q.Item1); var maxC = refsF.Max(q => q.Item1);
                            var r1 = Limite(perp, minC, -1, transv, t0); var r2 = Limite(perp, maxC, +1, transv, t0);
                            var lst = new List<Tuple<double, Reference>>(refsF); if (r1 != null) lst.Add(Tuple.Create(Coord(bb.Min, eixo) - F(1), r1)); if (r2 != null) lst.Add(Tuple.Create(Coord(bb.Max, eixo) + F(1), r2));
                            if (r1 == null || r2 == null) avisos.Add("luminária(s) em " + r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() + ": limite de parede não encontrado");
                            var cad = Limpa(lst);
                            var a = eixo.Multiply(Coord(bb.Min, eixo)) + transv.Multiply(t0) + XYZ.BasisZ * z; var b = eixo.Multiply(Coord(bb.Max, eixo)) + transv.Multiply(t0) + XYZ.BasisZ * z;
                            Cotar(cad.Select(q => q.Item2).ToList(), Line.CreateBound(a, b));
                        }
                    }
                }
            }
            if (pecas.Count == 0) avisos.Add("nenhuma " + (modo == "tomadas" ? "tomada/interruptor" : "luminária") + " visível na vista");
            if (semAmb > 0) avisos.Add(semAmb + " peça(s) fora de ambiente — não cotadas");
        }
        else if (modo == "cobertura") {
            var telhados = new FilteredElementCollector(document, vista.Id).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType().ToElements();
            if (telhados.Count == 0) avisos.Add("nenhum telhado visível");
            var go = new Options { ComputeReferences = true, View = vista };
            var rx = new List<Tuple<double, Reference>>(); var ry = new List<Tuple<double, Reference>>();
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            foreach (var tel in telhados) {
                var bb = tel.get_BoundingBox(null); if (bb == null) continue; x0 = Math.Min(x0, bb.Min.X); y0 = Math.Min(y0, bb.Min.Y); x1 = Math.Max(x1, bb.Max.X); y1 = Math.Max(y1, bb.Max.Y);
                var ge = tel.get_Geometry(go); if (ge == null) continue;
                foreach (var gobj in ge) { var so = gobj as Solid; if (so == null) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.Reference == null) continue;
                        if (Math.Abs(pf.FaceNormal.X) > 0.999) rx.Add(Tuple.Create(pf.Origin.X, pf.Reference)); else if (Math.Abs(pf.FaceNormal.Y) > 0.999) ry.Add(Tuple.Create(pf.Origin.Y, pf.Reference)); } }
            }
            // corpo × beiral (17,95 | 0,97 na referência): acrescenta as faces externas extremas das paredes da edificação
            var paredesCasa = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => { var b = w.get_BoundingBox(null); return b != null && (b.Max.Z - b.Min.Z) > F(3.0); }).ToList();
            if (paredesCasa.Count > 0) {
                var ex = new List<Tuple<double, Reference>>(); var ey = new List<Tuple<double, Reference>>(); var go3 = new Options { ComputeReferences = true };
                foreach (var w in paredesCasa) { var ge = w.get_Geometry(go3); if (ge == null) continue; foreach (var gobj in ge) { var so = gobj as Solid; if (so == null) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.Reference == null) continue; if (Math.Abs(pf.FaceNormal.X) > 0.999) ex.Add(Tuple.Create(pf.Origin.X, pf.Reference)); else if (Math.Abs(pf.FaceNormal.Y) > 0.999) ey.Add(Tuple.Create(pf.Origin.Y, pf.Reference)); } } }
                foreach (var par in new[] { Tuple.Create(ex, rx), Tuple.Create(ey, ry) }) { if (par.Item1.Count == 0) continue; double mn = par.Item1.Min(q => q.Item1), mx = par.Item1.Max(q => q.Item1);
                    par.Item2.AddRange(par.Item1.Where(q => q.Item1 < mn + F(0.01) || q.Item1 > mx - F(0.01)).GroupBy(q => Math.Round(q.Item1 / F(0.01))).Select(gq => gq.First())); }
            }
            if (telhados.Count > 0) {
                var cx = Limpa(rx); var cy = Limpa(ry); double o1 = F(0.010 * esc), o2 = F(0.018 * esc); z = telhados.Max(tl => tl.get_BoundingBox(null)?.Min.Z ?? z);
                if (cx.Count < 2 || cy.Count < 2) avisos.Add("telhado sem faces laterais verticais (beiral inclinado?) — cote à mão");
                foreach (var face in new[] { "frente", "fundo", "esquerda", "direita" }) {
                    bool hz = face == "frente" || face == "fundo"; var cad = hz ? cx : cy; if (cad.Count < 2) continue;
                    double p1 = face == "frente" ? y0 - o1 : face == "fundo" ? y1 + o1 : face == "esquerda" ? x0 - o1 : x1 + o1, p2 = face == "frente" ? y0 - o2 : face == "fundo" ? y1 + o2 : face == "esquerda" ? x0 - o2 : x1 + o2;
                    Func<double, Line> L = pos => hz ? Line.CreateBound(new XYZ(x0, pos, z), new XYZ(x1, pos, z)) : Line.CreateBound(new XYZ(pos, y0, z), new XYZ(pos, y1, z));
                    if (cad.Count > 2) Cotar(cad.Select(q => q.Item2).ToList(), L(p1));
                    Cotar(new List<Reference> { cad[0].Item2, cad[cad.Count - 1].Item2 }, L(cad.Count > 2 ? p2 : p1));
                }
            }
        }
        else if (modo == "corte") {
            var vs = vista as ViewSection; if (vs == null) { log.AppendLine(nome + ": não é corte"); continue; }
            var go = new Options { ComputeReferences = true, View = vista };
            var rz = new List<Tuple<double, Reference>>();
            var cats = new[] { BuiltInCategory.OST_Floors, BuiltInCategory.OST_Ceilings, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_StructuralFraming };
            foreach (var cat in cats) foreach (var el in new FilteredElementCollector(document, vista.Id).OfCategory(cat).WhereElementIsNotElementType()) {
                GeometryElement ge; try { ge = el.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                foreach (var gobj in ge) { var so = gobj as Solid; if (so == null) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf != null && pf.Reference != null && Math.Abs(pf.FaceNormal.Z) > 0.999) rz.Add(Tuple.Create(pf.Origin.Z, pf.Reference)); } }
            }
            // peitoril e verga das janelas que o corte atravessa (planos Inferior/Superior da família)
            foreach (var fi in new FilteredElementCollector(document, vista.Id).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().OfType<FamilyInstance>()) {
                var bb = fi.get_BoundingBox(null); if (bb == null) continue;
                foreach (var tp in new[] { FamilyInstanceReferenceType.Bottom, FamilyInstanceReferenceType.Top }) { var refs = fi.GetReferences(tp); if (refs.Count > 0) rz.Add(Tuple.Create(tp == FamilyInstanceReferenceType.Bottom ? bb.Min.Z : bb.Max.Z, refs[0])); }
            }
            var tot = Limpa(rz);
            var cb = vs.CropBox; var tr = cb.Transform; var dirH = vs.RightDirection; double prof = tr.Origin.DotProduct(vs.ViewDirection);
            Func<double, XYZ> PtH = h => dirH.Multiply(h) + vs.ViewDirection.Multiply(prof);
            // uma cadeia vertical por ambiente cortado (padrão da referência: piso → forro → laje → topo), encostada na parede esquerda do ambiente
            var todosAmb = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
            var faces = new List<Tuple<PlanarFace, Reference, BuiltInCategory, ElementId>>();
            foreach (var cat in cats) foreach (var el in new FilteredElementCollector(document, vista.Id).OfCategory(cat).WhereElementIsNotElementType()) {
                GeometryElement ge; try { ge = el.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                foreach (var gobj in ge) { var so = gobj as Solid; if (so == null) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.Reference == null || Math.Abs(pf.FaceNormal.Z) < 0.999) continue;
                        if (cat == BuiltInCategory.OST_Ceilings && pf.FaceNormal.Z > 0) continue;   // forro: só a face de baixo (a espessura do forro não é cotada)
                        faces.Add(Tuple.Create(pf, pf.Reference, cat, el.Id)); } }
            }
            int nAmb = 0; var pendentes = new List<Tuple<List<Reference>, Line>>(); var pisosPav = new List<Tuple<double, Reference>>();
            foreach (var amb in todosAmb) {
                var bbA = amb.get_BoundingBox(null); if (bbA == null) continue;
                double hA = Math.Min(bbA.Min.DotProduct(dirH), bbA.Max.DotProduct(dirH)), hB = Math.Max(bbA.Min.DotProduct(dirH), bbA.Max.DotProduct(dirH));
                double zl = bbA.Min.Z; XYZ pt = null; double hUsado = 0;
                for (double h = hA + F(0.30); h < hB; h += F(0.20)) { var q = PtH(h); var q3 = new XYZ(q.X, q.Y, zl + F(1.0)); if (amb.IsPointInRoom(q3)) { pt = q3; hUsado = h; break; } }
                if (pt == null) continue;   // o corte não passa por este ambiente
                var lst = new List<Tuple<double, Reference>>(); double pisoTopo = double.MinValue; Reference pisoRef = null;
                var acima = new List<Tuple<double, Reference, BuiltInCategory, ElementId>>();
                foreach (var f in faces) { var pr = f.Item1.Project(new XYZ(pt.X, pt.Y, f.Item1.Origin.Z)); if (pr == null || pr.Distance > F(0.002)) continue;
                    double fz = f.Item1.Origin.Z;
                    if (fz <= zl + F(0.30)) { if (f.Item3 == BuiltInCategory.OST_Floors && f.Item1.FaceNormal.Z > 0 && fz > pisoTopo) { pisoTopo = fz; pisoRef = f.Item2; } continue; }   // abaixo: só o piso acabado
                    acima.Add(Tuple.Create(fz, f.Item2, f.Item3, f.Item4)); }
                if (pisoRef == null) continue;
                // vários pavimentos: a cadeia do ambiente para na laje (ou telhado) logo acima dele, incluindo a espessura dela;
                // o que fica acima pertence à cadeia do ambiente do pavimento de cima
                acima = acima.OrderBy(q => q.Item1).ToList();
                var laje = acima.FirstOrDefault(q => q.Item3 == BuiltInCategory.OST_Floors || q.Item3 == BuiltInCategory.OST_Roofs);
                double zCorte = laje != null ? acima.Where(q => q.Item4 == laje.Item4).Max(q => q.Item1) : double.MaxValue;
                foreach (var qa in acima) if (qa.Item1 <= zCorte + F(0.002)) lst.Add(Tuple.Create(qa.Item1, qa.Item2));
                lst.Add(Tuple.Create(pisoTopo, pisoRef)); pisosPav.Add(Tuple.Create(pisoTopo, pisoRef));
                var cad = Limpa(lst); if (cad.Count < 2) continue;
                var bse = PtH(hUsado); pendentes.Add(Tuple.Create(cad.Select(q => q.Item2).ToList(), Line.CreateBound(new XYZ(bse.X, bse.Y, cad[0].Item1), new XYZ(bse.X, bse.Y, cad[cad.Count - 1].Item1)))); nAmb++;
            }
            // as faces ficam inválidas depois que o documento muda: primeiro calcula todas as cadeias, depois cria as cotas
            foreach (var pd in pendentes) Cotar(pd.Item1, pd.Item2);
            if (nAmb == 0) avisos.Add("nenhum ambiente cortado com piso e forro/laje acima");
            // altura total do volume, à esquerda do desenho
            if (tot.Count >= 2) {
                double o2 = F(0.010 * esc); var orig = tr.OfPoint(new XYZ(cb.Min.X, 0, 0));
                if (!vs.CropBoxActive) { var bbs = new FilteredElementCollector(document, vista.Id).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
                    if (bbs.Count > 0) orig = dirH.Multiply(bbs.Min(b => Math.Min(b.Min.DotProduct(dirH), b.Max.DotProduct(dirH)))); }
                var bse = PtH(orig.DotProduct(dirH) - F(0.3) - o2);
                var piso0 = tot.Where(q => q.Item1 < tot[0].Item1 + F(0.30)).Last();   // piso acabado (face mais alta das camadas do piso térreo)
                // vários pavimentos: piso acabado de cada pavimento cortado entra na linha total (pé-direito por pavimento + total)
                var refsTot = new List<Reference> { piso0.Item2 };
                double zAnt = piso0.Item1;
                foreach (var pp in pisosPav.OrderBy(q => q.Item1)) if (pp.Item1 > zAnt + F(1.0) && pp.Item1 < tot[tot.Count - 1].Item1 - F(0.5)) { refsTot.Add(pp.Item2); zAnt = pp.Item1; }
                refsTot.Add(tot[tot.Count - 1].Item2);
                Cotar(refsTot, Line.CreateBound(new XYZ(bse.X, bse.Y, piso0.Item1), new XYZ(bse.X, bse.Y, tot[tot.Count - 1].Item1)));
                if (refsTot.Count > 2) log.AppendLine(nome + ": linha total com " + (refsTot.Count - 2) + " pavimento(s) acima do térreo");
            }
        }
        else { log.AppendLine(nome + ": modo \"" + modo + "\" desconhecido"); continue; }

        // trechos de valor zero: texto apagado
        document.Regenerate();
        foreach (var d in criadas) foreach (DimensionSegment sg in d.Segments) if (sg.Value.HasValue && sg.Value.Value < F(0.001)) { try { sg.ValueOverride = " "; } catch { } }
        log.AppendLine(nome + " [" + modo + "]: " + n + " cotas" + (tipo != null ? " (tipo \"" + tipo.Name + "\")" : "") + (falhas > 0 ? ", " + falhas + " não criadas" : "") + (descartadas > 0 ? ", " + descartadas + " referências descartadas" : "")
            + (avisos.Count > 0 ? "\n  " + string.Join("\n  ", avisos.Distinct()) : ""));
    }
    t.Commit();
}
return log.ToString();
