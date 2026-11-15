// SKILL EXECUTIVO REVIT — Fase 6: anotação automática de plantas
// send_code_to_revit · transactionMode "none"
// parameters[0] = nome da vista (planta) a anotar (ex.: "PAVIMENTO TERREO EXECUTIVO")
// parameters[1] = o que anotar, separado por ";" (padrão "portas;janelas;ambientes;niveis;cotas_externas"):
//   portas / janelas — identificador em cada porta/janela ainda sem identificador na vista
//   ambientes        — identificador de ambiente em cada ambiente ainda sem identificador na vista
//   niveis           — cota de nível (elevação de ponto) no piso de cada ambiente
//   cotas_externas   — regra do escritório: TODAS as cotas ficam fora do lote (por fora dos muros), nas 4 faces:
//                      1ª linha = cadeia integrada de divisa a divisa (limites de paredes e pisos; vãos não);
//                      2ª linha = blocos (divisas, muros, extremos da edificação); 3ª linha = total do lote. Cotas lineares fora do lote são tratadas como da skill.
//   refazer_cotas    — apaga as cotas lineares fora do lote e cria de novo
//   apagar_cotas_internas — apaga as cotas lineares com origem dentro do lote (versões antigas da skill cotavam por dentro)
// Tipos do escritório (procurados pelo nome; se não houver, usa o primeiro da categoria):
//   "Identificador da porta", "Identificador de Janela", "Identificador de Ambiente", "VZ-Nível Planta", "VZ-Cota Interna"
// Idempotente: o que já está anotado na vista é pulado. Uma transação "Skill: anotar planta" (Ctrl+Z desfaz). Não salva.
// Textos e linhas de detalhe ficam com o arquiteto; trechos de cota muito curtos podem precisar de ajuste do texto.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var nomeVista = P(0);
var opcoes = (P(1) == "" ? "portas;janelas;ambientes;niveis;cotas_externas" : P(1).ToLower()).Split(';').Select(s => s.Trim()).ToList();
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
    // cotas externas em cadeia (regra do escritório): todas as cotas ficam FORA do lote, nas quatro faces;
    // cada face tem uma linha integrada, de divisa a divisa, sem trecho sem cotar, com os limites de todas as paredes
    // (faces e vãos) e pisos projetados naquele eixo; e uma segunda linha com o total do lote.
    if (opcoes.Contains("cotas_internas")) log.AppendLine("cotas internas: desativadas — pela regra do escritório as cotas ficam fora do lote (use \"cotas_externas\").");
    if (opcoes.Contains("cotas_externas") || opcoes.Contains("refazer_cotas") || opcoes.Contains("apagar_cotas_internas")) {
        // --- lote: linhas de propriedade > muros de divisa (paredes até 3 m) > retângulo das paredes (mesma regra do 02_desenhar_lote)
        var paredesL = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.get_BoundingBox(null) != null).ToList();
        Func<Wall, double> Alt = w => { var b = w.get_BoundingBox(null); return (b.Max.Z - b.Min.Z) * 0.3048; };
        var murosL = paredesL.Where(w => Alt(w) <= 3.0).ToList();
        var propsL = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements().Where(e => e.get_BoundingBox(null) != null).ToList();
        var caixas = propsL.Count > 0 ? propsL.Select(e => e.get_BoundingBox(null)).ToList() : (murosL.Count >= 2 ? murosL : paredesL).Select(w => w.get_BoundingBox(null)).ToList();
        if (caixas.Count == 0) { log.AppendLine("cotas externas: sem paredes nem linhas de propriedade — nada a cotar."); }
        else {
        double lx0 = caixas.Min(b => b.Min.X), ly0 = caixas.Min(b => b.Min.Y), lx1 = caixas.Max(b => b.Max.X), ly1 = caixas.Max(b => b.Max.Y);
        var calcL = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().FirstOrDefault(r => r.Area > 0 && (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper().Contains("CALÇADA"));
        if (calcL != null) { var cb = calcL.get_BoundingBox(null); if (cb.Max.Y <= ly0 + F(0.2) && cb.Max.Y > ly0 - F(0.2)) ly0 = cb.Max.Y; if (cb.Min.Y >= ly1 - F(0.2) && cb.Min.Y < ly1 + F(0.2)) ly1 = cb.Min.Y; if (cb.Max.X <= lx0 + F(0.2) && cb.Max.X > lx0 - F(0.2)) lx0 = cb.Max.X; if (cb.Min.X >= lx1 - F(0.2) && cb.Min.X < lx1 + F(0.2)) lx1 = cb.Min.X; }
        double z = nivel != null ? nivel.Elevation : 0, tol = F(0.02);
        Func<XYZ, bool> DentroLote = p => p.X > lx0 + tol && p.X < lx1 - tol && p.Y > ly0 + tol && p.Y < ly1 - tol;
        // Origin lança exceção em cota de vários trechos: usa a origem do 1º trecho
        Func<Dimension, XYZ> Org = d => { try { return d.NumberOfSegments > 1 ? d.Segments.get_Item(0).Origin : d.Origin; } catch { return null; } };
        var dimsVista = new FilteredElementCollector(document, vista.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Where(d => !(d is SpotDimension) && Org(d) != null).ToList();
        if (opcoes.Contains("apagar_cotas_internas")) {
            var internas = dimsVista.Where(d => DentroLote(Org(d))).Select(d => d.Id).ToList();
            if (internas.Count > 0) document.Delete(internas); log.AppendLine("cotas dentro do lote apagadas: " + internas.Count);
            dimsVista = dimsVista.Where(d => !internas.Contains(d.Id)).ToList();
        }
        // cotas não têm parâmetro Comentários: as da skill são as cotas lineares com origem fora do lote (regra: nada de cota dentro)
        var nossas = dimsVista.Where(d => !DentroLote(Org(d))).Select(d => d.Id).ToList();
        if (nossas.Count > 0 && opcoes.Contains("refazer_cotas")) { document.Delete(nossas); nossas.Clear(); }
        if (nossas.Count > 0) log.AppendLine("cotas externas: a vista já tem " + nossas.Count + " — puladas (use \"refazer_cotas\").");
        else if (opcoes.Contains("cotas_externas") || opcoes.Contains("refazer_cotas")) {
            var tiposL = new FilteredElementCollector(document).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(d => d.StyleType == DimensionStyleType.Linear).ToList();
            // tipo: "…Executivo 1-<n>" com a escala mais próxima da vista (empate = a maior escala de texto, n menor); senão qualquer "…1-<n>"; senão o primeiro VZ
            Func<DimensionType, int> EscTipo = d => { var m = System.Text.RegularExpressions.Regex.Match(d.Name.Replace("/", "-"), @"1-(\d+)\s*$"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; };
            var tipoExt = tiposL.Where(d => d.Name.ToUpper().Contains("EXECUTIVO") && EscTipo(d) > 0).OrderBy(d => Math.Abs(EscTipo(d) - vista.Scale)).ThenBy(d => EscTipo(d)).FirstOrDefault()
                ?? tiposL.Where(d => EscTipo(d) > 0).OrderBy(d => Math.Abs(EscTipo(d) - vista.Scale)).ThenBy(d => EscTipo(d)).FirstOrDefault()
                ?? tiposL.OrderByDescending(d => d.Name.ToUpper().Contains("VZ")).FirstOrDefault();
            // --- referências: faces verticais de paredes e pisos (e linhas de propriedade), com a coordenada no eixo
            var go = new Options { ComputeReferences = true, View = vista };
            var refsX = new List<Tuple<double, Reference, bool>>(); var refsY = new List<Tuple<double, Reference, bool>>();   // bool = é divisa
            // 2ª linha (blocos): divisas, faces dos muros e os extremos da edificação — como no projeto de referência (6,65 | 0,15 | 2,00 | 8,35 ...)
            var blocoX = new List<Tuple<double, Reference, bool>>(); var blocoY = new List<Tuple<double, Reference, bool>>();
            var casaX = new List<Tuple<double, Reference, bool>>(); var casaY = new List<Tuple<double, Reference, bool>>();
            var elems = new List<Element>(); elems.AddRange(paredesL.Cast<Element>());
            elems.AddRange(new FilteredElementCollector(document, vista.Id).OfClass(typeof(Floor)).ToElements());
            foreach (var el in elems) {
                var b = el.get_BoundingBox(null); if (b == null || b.Max.X < lx0 - tol || b.Min.X > lx1 + tol || b.Max.Y < ly0 - tol || b.Min.Y > ly1 + tol) continue;
                if (nivel != null && (b.Max.Z < nivel.Elevation - F(0.5) || b.Min.Z > nivel.Elevation + F(2.0))) continue;   // só o que está neste pavimento
                GeometryElement ge; try { ge = el.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                // parede: faces ao longo da direção da parede só nas pontas (as do meio são ombreiras de vão — fora da regra "limites de paredes e pisos")
                bool ehMuro = el is Wall && murosL.Any(m => m.Id == el.Id), ehCasa = el is Wall && !ehMuro;
                var wl = el is Wall ? (el.Location as LocationCurve)?.Curve as Line : null; double wmeia = el is Wall ? ((Wall)el).Width / 2 + F(0.01) : 0;
                foreach (var g in ge) { var so = g as Solid; if (so == null || so.Faces.Size == 0) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.Reference == null) continue;
                        if (wl != null && Math.Abs(pf.FaceNormal.DotProduct(wl.Direction)) > 0.999) {
                            double sF = (pf.Origin - wl.GetEndPoint(0)).DotProduct(wl.Direction), sL = wl.Length;
                            if (sF > wmeia && sF < sL - wmeia) continue; }
                        if (Math.Abs(pf.FaceNormal.X) > 0.999) { refsX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); if (ehMuro) blocoX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); if (ehCasa) casaX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); }
                        else if (Math.Abs(pf.FaceNormal.Y) > 0.999) { refsY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); if (ehMuro) blocoY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); if (ehCasa) casaY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); } } }
            }
            foreach (var pl in propsL) { GeometryElement ge; try { ge = pl.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                foreach (var g in ge) { var ln = g as Line; if (ln == null || ln.Reference == null) continue;
                    if (Math.Abs(ln.Direction.Y) > 0.999) refsX.Add(Tuple.Create(ln.Origin.X, ln.Reference, true));
                    else if (Math.Abs(ln.Direction.X) > 0.999) refsY.Add(Tuple.Create(ln.Origin.Y, ln.Reference, true)); } }
            // ordena, descarta o que está fora do lote e junta coordenadas a menos de 1 cm (a divisa tem prioridade)
            Func<List<Tuple<double, Reference, bool>>, double, double, List<Tuple<double, Reference, bool>>> Cadeia = (lst, a, bfim) => {
                var res = new List<Tuple<double, Reference, bool>>();
                foreach (var r in lst.Where(q => q.Item1 > a - tol && q.Item1 < bfim + tol).OrderBy(q => q.Item1).ThenByDescending(q => q.Item3)) {
                    if (res.Count > 0 && r.Item1 - res[res.Count - 1].Item1 < F(0.01)) continue; res.Add(r); }
                return res;
            };
            var cadX = Cadeia(refsX, lx0, lx1); var cadY = Cadeia(refsY, ly0, ly1);
            // blocos: muros + as faces mais externas da edificação em cada eixo
            Func<List<Tuple<double, Reference, bool>>, List<Tuple<double, Reference, bool>>> Extremos = l => { if (l.Count == 0) return l; double mn = l.Min(q => q.Item1), mx = l.Max(q => q.Item1); return l.Where(q => q.Item1 < mn + F(0.01) || q.Item1 > mx - F(0.01)).ToList(); };
            blocoX.AddRange(Extremos(casaX)); blocoY.AddRange(Extremos(casaY));
            var bloX = Cadeia(blocoX, lx0, lx1); var bloY = Cadeia(blocoY, ly0, ly1);
            // três linhas por face (padrão do escritório): detalhada a 5 mm do que estiver desenhado, blocos e total a cada 6,5 mm (no papel)
            double esc = vista.Scale, o1 = F(0.005 * esc), o2 = F(0.0115 * esc), o3 = F(0.018 * esc);
            // a linha fica fora do lote E de tudo o que está modelado além dele (calçada, passeio): usa a caixa de paredes e pisos da vista
            double ex0 = lx0, ey0 = ly0, ex1 = lx1, ey1 = ly1;
            foreach (var el in elems) { var b = el.get_BoundingBox(null); if (b == null) continue; if (nivel != null && (b.Max.Z < nivel.Elevation - F(0.5) || b.Min.Z > nivel.Elevation + F(2.0))) continue;
                if (b.Max.X - b.Min.X > F(200) || b.Max.Y - b.Min.Y > F(200)) continue;   // topografia/piso gigante não empurra as cotas
                ex0 = Math.Min(ex0, b.Min.X); ey0 = Math.Min(ey0, b.Min.Y); ex1 = Math.Max(ex1, b.Max.X); ey1 = Math.Max(ey1, b.Max.Y); }
            int nCot = 0, nSeg = 0, curtos = 0, descartadas = 0; var avisos = new List<string>(); var criadasExt = new List<Dimension>(); var fora = new Dictionary<ElementId, XYZ>();
            Func<List<Tuple<double, Reference, bool>>, Line, Dimension> Cotar = (cad, linha) => {
                if (cad.Count < 2) return null;
                var ra = new ReferenceArray(); foreach (var r in cad) ra.Append(r.Item2);
                try { return tipoExt != null ? document.Create.NewDimension(vista, linha, ra, tipoExt) : document.Create.NewDimension(vista, linha, ra); }
                catch { }
                // alguma referência não é cotável nesta vista: testa uma a uma contra a primeira e refaz só com as válidas
                var boas = new List<Tuple<double, Reference, bool>> { cad[0] };
                for (int i = 1; i < cad.Count; i++) {
                    using (var st = new SubTransaction(document)) { st.Start(); bool ok = false;
                        try { var rr = new ReferenceArray(); rr.Append(cad[0].Item2); rr.Append(cad[i].Item2); ok = document.Create.NewDimension(vista, linha, rr) != null; } catch { }
                        st.RollBack(); if (ok) boas.Add(cad[i]); else descartadas++; }
                }
                if (boas.Count < 2) return null; var rb = new ReferenceArray(); foreach (var r in boas) rb.Append(r.Item2);
                try { return tipoExt != null ? document.Create.NewDimension(vista, linha, rb, tipoExt) : document.Create.NewDimension(vista, linha, rb); } catch { return null; }
            };
            foreach (var face in new[] { "frente", "fundo", "esquerda", "direita" }) {
                bool horiz = face == "frente" || face == "fundo";   // frente = lado de menor Y
                var cad = horiz ? cadX : cadY; if (cad.Count < 2) { avisos.Add(face + ": sem referências"); continue; }
                double ini = horiz ? lx0 : ly0, fim = horiz ? lx1 : ly1;
                if (cad[0].Item1 - ini > F(0.05) || fim - cad[cad.Count - 1].Item1 > F(0.05)) avisos.Add(face + ": a cadeia não chega até a divisa (falta muro ou linha de propriedade nesse trecho)");
                double pos1 = face == "frente" ? ey0 - o1 : face == "fundo" ? ey1 + o1 : face == "esquerda" ? ex0 - o1 : ex1 + o1;
                double pos2 = face == "frente" ? ey0 - o2 : face == "fundo" ? ey1 + o2 : face == "esquerda" ? ex0 - o2 : ex1 + o2;
                double pos3 = face == "frente" ? ey0 - o3 : face == "fundo" ? ey1 + o3 : face == "esquerda" ? ex0 - o3 : ex1 + o3;
                Func<double, Line> L = pos => horiz ? Line.CreateBound(new XYZ(lx0, pos, z), new XYZ(lx1, pos, z)) : Line.CreateBound(new XYZ(pos, ly0, z), new XYZ(pos, ly1, z));
                var dParc = Cotar(cad, L(pos1));
                // blocos: sempre com as duas divisas da cadeia detalhada nas pontas; só cria se tiver trecho intermediário
                var blo = new List<Tuple<double, Reference, bool>> { cad[0] }; blo.AddRange((horiz ? bloX : bloY).Where(q => q.Item1 > cad[0].Item1 + F(0.01) && q.Item1 < cad[cad.Count - 1].Item1 - F(0.01))); blo.Add(cad[cad.Count - 1]);
                Dimension dBlo = blo.Count > 2 ? Cotar(blo, L(pos2)) : null; if (dBlo != null) { nCot++; criadasExt.Add(dBlo); fora[dBlo.Id] = face == "frente" ? -XYZ.BasisY : face == "fundo" ? XYZ.BasisY : face == "esquerda" ? -XYZ.BasisX : XYZ.BasisX; }
                var dTot = Cotar(new List<Tuple<double, Reference, bool>> { cad[0], cad[cad.Count - 1] }, L(dBlo != null ? pos3 : pos2));
                foreach (var d in new[] { dParc, dTot }) { if (d == null) continue; nCot++; criadasExt.Add(d); }
                if (dParc != null) fora[dParc.Id] = face == "frente" ? -XYZ.BasisY : face == "fundo" ? XYZ.BasisY : face == "esquerda" ? -XYZ.BasisX : XYZ.BasisX;
                if (dParc == null) avisos.Add(face + ": cadeia não criada");
            }
            document.Regenerate();
            // trecho curto (texto mais largo que o trecho, ~6 mm no papel): textos alternados — um na linha, o seguinte deslocado para fora
            double curto = F(0.006 * esc), passo = F(0.0035 * esc);
            foreach (var d in criadasExt) {
                if (d.Segments.Size == 0) { nSeg++; continue; }
                int seq = 0;
                foreach (DimensionSegment sg in d.Segments) { nSeg++; if (!sg.Value.HasValue) { seq = 0; continue; }
                    if (sg.Value.Value < F(0.001)) { try { sg.ValueOverride = " "; } catch { } seq = 0; continue; }
                    if (sg.Value.Value >= curto) { seq = 0; continue; }
                    curtos++; seq++;
                    if (seq % 2 == 0 && fora.ContainsKey(d.Id)) { try { sg.TextPosition = sg.TextPosition + fora[d.Id].Multiply(passo); } catch { } } }
            }
            // recorte ativo que não alcança as cotas: amplia até o lote + linha do total + 1 m
            if (vista.CropBoxActive) {
                var cbv = vista.CropBox; var inv = cbv.Transform.Inverse; double mg = o3 + F(1.0);
                var c0 = inv.OfPoint(new XYZ(ex0 - mg, ey0 - mg, z)); var c1 = inv.OfPoint(new XYZ(ex1 + mg, ey1 + mg, z));
                double nx0 = Math.Min(cbv.Min.X, Math.Min(c0.X, c1.X)), ny0 = Math.Min(cbv.Min.Y, Math.Min(c0.Y, c1.Y)), nx1 = Math.Max(cbv.Max.X, Math.Max(c0.X, c1.X)), ny1 = Math.Max(cbv.Max.Y, Math.Max(c0.Y, c1.Y));
                if (nx0 < cbv.Min.X - tol || ny0 < cbv.Min.Y - tol || nx1 > cbv.Max.X + tol || ny1 > cbv.Max.Y + tol) { cbv.Min = new XYZ(nx0, ny0, cbv.Min.Z); cbv.Max = new XYZ(nx1, ny1, cbv.Max.Z); try { vista.CropBox = cbv; avisos.Add("recorte da vista ampliado para caber as cotas"); } catch { } }
            }
            log.AppendLine("cotas externas: " + nCot + " linhas (cadeia + total em cada face), " + nSeg + " trechos; lote " + Math.Round((lx1 - lx0) * 0.3048, 2) + " × " + Math.Round((ly1 - ly0) * 0.3048, 2) + " m"
                + (curtos > 0 ? "; " + curtos + " trechos curtos com texto escalonado (confira)" : "") + (descartadas > 0 ? "; " + descartadas + " referências não cotáveis descartadas" : "")
                + (tipoExt == null ? " (sem tipo de cota linear)" : " (tipo \"" + tipoExt.Name + "\")") + (avisos.Count > 0 ? "\n  " + string.Join("\n  ", avisos) : ""));
        }
        }
    }
    t.Commit();
}
return "Planta \"" + nomeVista + "\" — " + rooms.Count + " ambientes no nível " + (nivel != null ? nivel.Name : "?") + "\n" + log.ToString();
