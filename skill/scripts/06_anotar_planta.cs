// SKILL EXECUTIVO REVIT — Fase 6: anotação automática de plantas
// send_code_to_revit · transactionMode "none"
// parameters[0] = nome da vista (planta) a anotar (ex.: "PAVIMENTO TERREO EXECUTIVO")
// parameters[1] = o que anotar, separado por ";" (padrão "portas;janelas;ambientes;niveis;cotas_internas"):
//   portas / janelas — identificador em cada porta/janela ainda sem identificador na vista
//   ambientes        — identificador de ambiente em cada ambiente ainda sem identificador na vista
//   niveis           — cota de nível (elevação de ponto) no piso de cada ambiente
//   cotas_internas   — largura e comprimento de cada ambiente, entre as faces das paredes (ambientes de contorno simples)
// Tipos do escritório (procurados pelo nome; se não houver, usa o primeiro da categoria):
//   "Identificador da porta", "Identificador de Janela", "Identificador de Ambiente", "VZ-Nível Planta", "VZ-Cota Interna"
// Idempotente: o que já está anotado na vista é pulado. Uma transação "Skill: anotar planta" (Ctrl+Z desfaz). Não salva.
// Cotas externas em cadeia, textos e linhas de detalhe ficam com o arquiteto.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var nomeVista = P(0);
var opcoes = (P(1) == "" ? "portas;janelas;ambientes;niveis;cotas_internas" : P(1).ToLower()).Split(';').Select(s => s.Trim()).ToList();
var vista = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate && v.Name == nomeVista);
if (vista == null) return "ERRO: planta '" + nomeVista + "' não encontrada.";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var nivel = vista.GenLevel;
Func<BuiltInCategory, string, ElementId> TipoTag = (cat, nome) => {
    var tipos = new FilteredElementCollector(document).OfCategory(cat).WhereElementIsElementType().ToElements();
    var tp = tipos.FirstOrDefault(x => x.Name == nome) ?? tipos.FirstOrDefault(); return tp == null ? ElementId.InvalidElementId : tp.Id;
};
// o que já está anotado nesta vista
var jaTag = new HashSet<ElementId>(new FilteredElementCollector(document, vista.Id).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().SelectMany<IndependentTag, ElementId>(t => { try { return t.GetTaggedLocalElementIds(); } catch { return new List<ElementId>(); } }));
var jaRoomTag = new HashSet<ElementId>(new FilteredElementCollector(document, vista.Id).OfCategory(BuiltInCategory.OST_RoomTags).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.RoomTag>().Where(t => t.Room != null).Select(t => t.Room.Id));
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
    .Where(r => r.Area > 0 && r.Location != null && nivel != null && r.LevelId == nivel.Id).ToList();

using (var t = new Transaction(document, "Skill: anotar planta")) {
    t.Start();
    // portas e janelas
    foreach (var par in new[] { Tuple.Create("portas", BuiltInCategory.OST_Doors, BuiltInCategory.OST_DoorTags, "Identificador da porta"), Tuple.Create("janelas", BuiltInCategory.OST_Windows, BuiltInCategory.OST_WindowTags, "Identificador de Janela") }) {
        if (!opcoes.Contains(par.Item1)) continue;
        var tipo = TipoTag(par.Item3, par.Item4); int n = 0, pulados = 0, erros = 0;
        var visiveis = new FilteredElementCollector(document, vista.Id).OfCategory(par.Item2).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
        foreach (var fi in visiveis) {
            if (jaTag.Contains(fi.Id)) { pulados++; continue; }
            try {
                var lp = fi.Location as LocationPoint; if (lp == null) continue;
                // do lado de dentro, a 45 cm do eixo da parede
                var f = new XYZ(fi.FacingOrientation.X, fi.FacingOrientation.Y, 0); var ponto = lp.Point - (f.GetLength() > 0 ? f.Normalize().Multiply(F(0.45)) : XYZ.Zero);
                var tag = IndependentTag.Create(document, tipo, vista.Id, new Reference(fi), false, TagOrientation.Horizontal, new XYZ(ponto.X, ponto.Y, 0));
                n++;
            } catch { erros++; }
        }
        log.AppendLine(par.Item1 + ": " + n + " identificadores novos" + (pulados > 0 ? ", " + pulados + " já tinham" : "") + (erros > 0 ? ", " + erros + " com erro" : "") + " (tipo \"" + (tipo != ElementId.InvalidElementId ? document.GetElement(tipo).Name : "?") + "\")");
    }
    // ambientes
    if (opcoes.Contains("ambientes")) {
        var tipo = TipoTag(BuiltInCategory.OST_RoomTags, "Identificador de Ambiente"); int n = 0, pulados = 0;
        foreach (var r in rooms) {
            if (jaRoomTag.Contains(r.Id)) { pulados++; continue; }
            var p = ((LocationPoint)r.Location).Point;
            var tag = document.Create.NewRoomTag(new LinkElementId(r.Id), new UV(p.X, p.Y), vista.Id);
            if (tag != null && tipo != ElementId.InvalidElementId) tag.ChangeTypeId(tipo); n++;
        }
        log.AppendLine("ambientes: " + n + " identificadores novos" + (pulados > 0 ? ", " + pulados + " já tinham" : ""));
    }
    // cotas de nível no piso de cada ambiente
    if (opcoes.Contains("niveis")) {
        var tipoSpot = new FilteredElementCollector(document).OfClass(typeof(SpotDimensionType)).Cast<SpotDimensionType>().FirstOrDefault(x => x.Name == "VZ-Nível Planta");
        var jaSpot = new FilteredElementCollector(document, vista.Id).OfClass(typeof(SpotDimension)).Cast<SpotDimension>().Select(s => s.Origin).ToList();
        var pisos = new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>().ToList();
        int n = 0, semPiso = 0, pulados = 0;
        foreach (var r in rooms) {
            var p = ((LocationPoint)r.Location).Point;
            if (jaSpot.Any(o => Math.Abs(o.X - p.X) < F(1.5) && Math.Abs(o.Y - p.Y) < F(1.5))) { pulados++; continue; }
            // piso sob o ponto do ambiente: face superior que contém o ponto
            Reference refFace = null; XYZ ponto = null;
            foreach (var piso in pisos) {
                var bb = piso.get_BoundingBox(null); if (bb == null || p.X < bb.Min.X || p.X > bb.Max.X || p.Y < bb.Min.Y || p.Y > bb.Max.Y) continue;
                if (Math.Abs(bb.Max.Z - nivel.Elevation) > F(0.6)) continue;
                foreach (var refTopo in HostObjectUtils.GetTopFaces(piso)) {
                    var face = piso.GetGeometryObjectFromReference(refTopo) as Face; if (face == null) continue;
                    var pr = face.Project(new XYZ(p.X, p.Y, bb.Max.Z + F(1)));
                    if (pr != null && Math.Abs(pr.XYZPoint.X - p.X) < F(0.01) && Math.Abs(pr.XYZPoint.Y - p.Y) < F(0.01)) { refFace = refTopo; ponto = pr.XYZPoint; break; }
                }
                if (refFace != null) break;
            }
            if (refFace == null) { semPiso++; continue; }
            var origem = ponto - new XYZ(0, F(0.9), 0);   // um pouco abaixo do identificador do ambiente
            var pf = pisos.First(x => x.Id == refFace.ElementId);
            var face2 = pf.GetGeometryObjectFromReference(refFace) as Face; var pr2 = face2.Project(new XYZ(origem.X, origem.Y, ponto.Z + F(1)));
            if (pr2 == null) { origem = ponto; } else origem = pr2.XYZPoint;
            try {
                var sp = document.Create.NewSpotElevation(vista, refFace, origem, origem, origem, origem, false);
                if (sp != null && tipoSpot != null) sp.ChangeTypeId(tipoSpot.Id); n++;
            } catch { semPiso++; }
        }
        log.AppendLine("cotas de nível: " + n + " novas" + (pulados > 0 ? ", " + pulados + " já tinham" : "") + (semPiso > 0 ? ", " + semPiso + " ambientes sem piso modelado sob o identificador" : "") + (tipoSpot == null ? " (tipo VZ-Nível Planta não encontrado)" : ""));
    }
    // cotas internas: largura e comprimento entre as faces das paredes
    if (opcoes.Contains("cotas_internas")) {
        var tipoCota = new FilteredElementCollector(document).OfClass(typeof(DimensionType)).Cast<DimensionType>().FirstOrDefault(x => x.Name == "VZ-Cota Interna" || x.Name == "VZ-Cota Interna 1/75");
        var opt = new SpatialElementBoundaryOptions { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish };
        int n = 0, pulados = 0;
        // idempotência: se a vista já tem cotas internas, não cria de novo (o arquiteto pode ter editado)
        int jaCotas = tipoCota == null ? 0 : new FilteredElementCollector(document, vista.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Count(d => d.GetTypeId() == tipoCota.Id);
        if (jaCotas > 0) { log.AppendLine("cotas internas: a vista já tem " + jaCotas + " — puladas."); opcoes.Remove("cotas_internas"); }
        // face lateral da parede que fica sobre a linha do contorno do ambiente
        Func<Wall, Line, Reference> FaceSobre = (w, seg) => {
            foreach (var lado in new[] { ShellLayerType.Interior, ShellLayerType.Exterior })
                foreach (var rf in HostObjectUtils.GetSideFaces(w, lado)) {
                    var face = w.GetGeometryObjectFromReference(rf) as PlanarFace; if (face == null) continue;
                    var meio = seg.Evaluate(0.5, true); var pr = face.Project(new XYZ(meio.X, meio.Y, face.Origin.Z));
                    if (pr != null && pr.Distance < F(0.01)) return rf;
                }
            return null;
        };
        if (opcoes.Contains("cotas_internas")) foreach (var r in rooms) {
            var segs = r.GetBoundarySegments(opt); if (segs == null || segs.Count != 1) { pulados++; continue; }   // só contorno simples (sem ilhas)
            var lista = segs[0].Where(s => s.GetCurve() is Line).Select(s => new { s, l = (Line)s.GetCurve(), w = document.GetElement(s.ElementId) as Wall }).ToList();
            var bb = r.get_BoundingBox(null); var c = (bb.Min + bb.Max) / 2;
            foreach (var eixoX in new[] { true, false }) {
                // segmentos perpendiculares ao eixo da cota, nas duas pontas do ambiente, que cruzam a linha central
                var perp = lista.Where(x => x.w != null && Math.Abs(eixoX ? x.l.Direction.X : x.l.Direction.Y) < 0.01).Where(x => {
                    var a = x.l.GetEndPoint(0); var b = x.l.GetEndPoint(1); double v = eixoX ? c.Y : c.X, lo = Math.Min(eixoX ? a.Y : a.X, eixoX ? b.Y : b.X), hi = Math.Max(eixoX ? a.Y : a.X, eixoX ? b.Y : b.X); return v > lo && v < hi; }).ToList();
                if (perp.Count < 2) continue;
                var s1 = perp.OrderBy(x => eixoX ? x.l.GetEndPoint(0).X : x.l.GetEndPoint(0).Y).First(); var s2 = perp.OrderBy(x => eixoX ? x.l.GetEndPoint(0).X : x.l.GetEndPoint(0).Y).Last();
                var r1 = FaceSobre(s1.w, s1.l); var r2 = FaceSobre(s2.w, s2.l); if (r1 == null || r2 == null) continue;
                var ra = new ReferenceArray(); ra.Append(r1); ra.Append(r2);
                // linha da cota a 35% do ambiente (fora do centro, onde ficam identificador e nível)
                var linha = eixoX ? Line.CreateBound(new XYZ(bb.Min.X, bb.Min.Y + (bb.Max.Y - bb.Min.Y) * 0.35, nivel.Elevation), new XYZ(bb.Max.X, bb.Min.Y + (bb.Max.Y - bb.Min.Y) * 0.35, nivel.Elevation))
                                  : Line.CreateBound(new XYZ(bb.Min.X + (bb.Max.X - bb.Min.X) * 0.35, bb.Min.Y, nivel.Elevation), new XYZ(bb.Min.X + (bb.Max.X - bb.Min.X) * 0.35, bb.Max.Y, nivel.Elevation));
                try { var d = tipoCota != null ? document.Create.NewDimension(vista, linha, ra, tipoCota) : document.Create.NewDimension(vista, linha, ra); if (d != null) n++; } catch { }
            }
        }
        if (opcoes.Contains("cotas_internas")) log.AppendLine("cotas internas: " + n + " novas" + (pulados > 0 ? " (" + pulados + " ambientes de contorno complexo ficaram para o arquiteto)" : "") + (tipoCota == null ? " (tipo VZ-Cota Interna não encontrado)" : ""));
    }
    t.Commit();
}
return "Planta \"" + nomeVista + "\" — " + rooms.Count + " ambientes no nível " + (nivel != null ? nivel.Name : "?") + "\n" + log.ToString();
