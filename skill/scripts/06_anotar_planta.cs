// SKILL EXECUTIVO REVIT — Fase 6: anotação automática de plantas
// send_code_to_revit · transactionMode "none"
// parameters[0] = nome da vista (planta) a anotar (ex.: "PAVIMENTO TERREO EXECUTIVO")
// parameters[1] = o que anotar, separado por ";" (padrão "portas;janelas;ambientes;niveis;cotas_externas;paisagismo"):
//   portas / janelas — identificador em cada porta/janela ainda sem identificador na vista
//   ambientes        — identificador de ambiente em cada ambiente ainda sem identificador na vista; o checklist pede
//                      nome, área, perímetro e nível: usa o tipo com "PERÍMETRO" no nome, se o modelo tiver
//   paisagismo       — esconde vegetação e entorno (categorias Plantação e Entorno) nesta vista: o paisagismo fica só na
//                      planta de layout. Se a vista usa modelo de vista que controla as categorias, só avisa.
//   niveis           — cota de nível (elevação de ponto) no piso de cada ambiente, entre o nome e a área (igual ao layout)
//   cotas_externas   — regra do escritório: TODAS as cotas ficam fora do lote (por fora dos muros), nas 4 faces, em TODOS os pavimentos:
//                      cotagem em três escalas (checklist executivo), todas integradas de divisa a divisa:
//                      1ª linha (micro)  = vãos de portas e janelas da fachada daquela face + extremos da edificação;
//                      2ª linha (médio)  = ambiente por ambiente (limites de paredes e pisos; ombreiras não);
//                      3ª linha (macro)  = total do terreno (em todas as plantas). Sem vão externo na face, a micro não é criada.
//                      Cotas lineares fora do lote são tratadas como da skill.
//   refazer_cotas    — apaga as cotas lineares fora do lote e cria de novo
//   apagar_cotas_internas — apaga as cotas lineares com origem dentro do lote (versões antigas da skill cotavam por dentro)
// Lado da calçada: as três linhas ficam depois da calçada (rua/asfalto não contam). Divisa a menos de 1 cm de um muro: vale a divisa.
// Recorte: sempre ligado e justo às cotas + 1 m.
// Tipos do escritório (procurados pelo nome; se não houver, usa o primeiro da categoria):
//   "Identificador da porta", "Identificador de Janela", "Identificador de Ambiente", "VZ-Nível Planta", "VZ-Cota Interna"
// Idempotente: o que já está anotado na vista é pulado. Uma transação "Skill: anotar planta" (Ctrl+Z desfaz). Não salva.
// Textos e linhas de detalhe ficam com o arquiteto; trechos de cota muito curtos podem precisar de ajuste do texto.
// Vários pavimentos: o lote (divisas, muros) vem sempre do térreo; a cadeia detalhada e os extremos da edificação vêm das
// paredes e pisos do pavimento da vista. No pavimento superior os muros só são cotáveis se aparecerem na vista
// (linhas de propriedade visíveis ou subposição do térreo); senão a cadeia fica só na edificação e o relatório avisa.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var nomeVista = P(0);
var opcoes = (P(1) == "" ? "portas;janelas;ambientes;niveis;cotas_externas;paisagismo" : P(1).ToLower()).Split(';').Select(s => s.Trim()).ToList();
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
        var tiposAmb = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_RoomTags).WhereElementIsElementType().ToElements();
        var tipoPer = tiposAmb.FirstOrDefault(x => { var nn = x.Name.ToUpper(); return nn.Contains("PERÍMETRO") || nn.Contains("PERIMETRO"); });
        var tipo = tipoPer != null ? tipoPer.Id : TipoTag(BuiltInCategory.OST_RoomTags, "Identificador de Ambiente"); int n = 0, pulados = 0;
        if (tipoPer == null) log.AppendLine("ambientes: o modelo não tem identificador com PERÍMETRO no nome — usado o padrão (o checklist pede nome, área, perímetro e nível)");
        foreach (var r in rooms) {
            if (jaRoomTag.Contains(r.Id)) { pulados++; continue; }
            var p = ((LocationPoint)r.Location).Point;
            var tag = document.Create.NewRoomTag(new LinkElementId(r.Id), new UV(p.X, p.Y), vista.Id);
            if (tag != null && tipo != ElementId.InvalidElementId) tag.ChangeTypeId(tipo); n++;
        }
        log.AppendLine("ambientes: " + n + " identificadores novos" + (pulados > 0 ? ", " + pulados + " já tinham" : ""));
    }
    // paisagismo só na planta de layout
    if (opcoes.Contains("paisagismo") && !vista.Name.ToUpper().Contains("LAYOUT")) {
        var catsP = new[] { BuiltInCategory.OST_Planting, BuiltInCategory.OST_Entourage };
        bool controlado = false;
        if (vista.ViewTemplateId != ElementId.InvalidElementId) { var vt = document.GetElement(vista.ViewTemplateId) as View;
            try { controlado = vt != null && !vt.GetNonControlledTemplateParameterIds().Contains(new ElementId(BuiltInParameter.VIS_GRAPHICS_MODEL)); } catch { controlado = true; } }
        if (controlado) log.AppendLine("paisagismo: o modelo de vista controla as categorias — esconda Plantação e Entorno no modelo de vista \"" + document.GetElement(vista.ViewTemplateId).Name + "\"");
        else { int nc = 0; foreach (var bc in catsP) { var cid = new ElementId(bc); try { if (vista.CanCategoryBeHidden(cid) && !vista.GetCategoryHidden(cid)) { vista.SetCategoryHidden(cid, true); nc++; } } catch { } }
            log.AppendLine("paisagismo: " + (nc > 0 ? "vegetação e entorno escondidos nesta vista" : "já estava escondido")); }
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
            var origem = ponto - new XYZ(0, F(0.0014 * vista.Scale), 0);   // regra do escritório: entre o nome do ambiente e a área (1,4 mm no papel abaixo do ponto do identificador), igual ao layout
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
        // muro = parede até 3 m que nasce no pavimento mais baixo e não delimita ambiente interno
        // (casas de vários pavimentos têm paredes de 2,8–3 m em cada nível: altura sozinha não basta)
        var niveisComAmb = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0 && r.Level != null).Select(r => r.Level.Elevation).ToList();
        double zTerreo = niveisComAmb.Count > 0 ? niveisComAmb.Min() : new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Min(l => l.Elevation);
        var exterioresL = new[] { "CALÇADA", "CALCADA", "EXTERNA", "QUINTAL", "JARDIM", "PASSEIO", "RECUO", "DESCOBERTA", "ESTACIONAMENTO", "PISCINA", "DECK", "VARANDA", "SACADA", "GARAGEM", "VAGA" };   // ambientes abertos: as paredes deles podem ser muros
        var paredesDeAmbiente = new HashSet<ElementId>();
        foreach (var ra in new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0)) {
            var nmA = (ra.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper(); if (exterioresL.Any(e => nmA.Contains(e))) continue;
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segs = null; try { segs = ra.GetBoundarySegments(new SpatialElementBoundaryOptions()); } catch { }
            if (segs != null) foreach (var lp in segs) foreach (var sg in lp) paredesDeAmbiente.Add(sg.ElementId);
        }
        var murosNome = paredesL.Where(w => (w.WallType.Name ?? "").ToUpper().Contains("MURO")).ToList();
        var murosL = murosNome.Count > 0 ? murosNome
            : paredesL.Where(w => Alt(w) <= 3.0 && w.get_BoundingBox(null).Min.Z < zTerreo + F(1.0) && !paredesDeAmbiente.Contains(w.Id)).ToList();
        bool superior = nivel != null && nivel.Elevation > zTerreo + F(1.0);
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
            // borda de piso que cai DENTRO da espessura de uma parede não vira referência (senão a parede de 0,25 vira 0,13 | 0,13)
            Func<bool, List<double[]>> Faixas = vert => paredesL.Where(w => { var l = (w.Location as LocationCurve)?.Curve as Line; return l != null && Math.Abs(vert ? l.Direction.Y : l.Direction.X) > 0.999; })
                .Select(w => { var l = (Line)((LocationCurve)w.Location).Curve; double c = vert ? l.Origin.X : l.Origin.Y; return new[] { c - w.Width / 2, c + w.Width / 2 }; }).ToList();
            var faixasX = Faixas(true); var faixasY = Faixas(false);
            Func<double, List<double[]>, bool> DentroParede = (c, fx) => fx.Any(q => c > q[0] + F(0.01) && c < q[1] - F(0.01));
            foreach (var el in elems) {
                var b = el.get_BoundingBox(null); if (b == null || b.Max.X < lx0 - tol || b.Min.X > lx1 + tol || b.Max.Y < ly0 - tol || b.Min.Y > ly1 + tol) continue;
                bool ehMuro = el is Wall && murosL.Any(m => m.Id == el.Id), ehCasa = el is Wall && !ehMuro;
                // só o que está neste pavimento — menos os muros, que limitam o lote em todas as plantas
                if (!ehMuro && nivel != null && (b.Max.Z < nivel.Elevation - F(0.5) || b.Min.Z > nivel.Elevation + F(2.0))) continue;
                GeometryElement ge; try { ge = el.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                // parede: faces ao longo da direção da parede só nas pontas (as do meio são ombreiras de vão — fora da regra "limites de paredes e pisos")
                var wl = el is Wall ? (el.Location as LocationCurve)?.Curve as Line : null; double wmeia = el is Wall ? ((Wall)el).Width / 2 + F(0.01) : 0;
                foreach (var g in ge) { var so = g as Solid; if (so == null || so.Faces.Size == 0) continue;
                    foreach (Face fc in so.Faces) { var pf = fc as PlanarFace; if (pf == null || pf.Reference == null) continue;
                        if (wl != null && Math.Abs(pf.FaceNormal.DotProduct(wl.Direction)) > 0.999) {
                            double sF = (pf.Origin - wl.GetEndPoint(0)).DotProduct(wl.Direction), sL = wl.Length;
                            if (sF > wmeia && sF < sL - wmeia) continue; }
                        if (el is Floor && ((Math.Abs(pf.FaceNormal.X) > 0.999 && DentroParede(pf.Origin.X, faixasX)) || (Math.Abs(pf.FaceNormal.Y) > 0.999 && DentroParede(pf.Origin.Y, faixasY)))) continue;
                        if (Math.Abs(pf.FaceNormal.X) > 0.999) { refsX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); if (ehMuro) blocoX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); if (ehCasa) casaX.Add(Tuple.Create(pf.Origin.X, pf.Reference, false)); }
                        else if (Math.Abs(pf.FaceNormal.Y) > 0.999) { refsY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); if (ehMuro) blocoY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); if (ehCasa) casaY.Add(Tuple.Create(pf.Origin.Y, pf.Reference, false)); } } }
            }
            foreach (var pl in propsL) { GeometryElement ge; try { ge = pl.get_Geometry(go); } catch { continue; } if (ge == null) continue;
                foreach (var g in ge) { var ln = g as Line; if (ln == null || ln.Reference == null) continue;
                    if (Math.Abs(ln.Direction.Y) > 0.999) refsX.Add(Tuple.Create(ln.Origin.X, ln.Reference, true));
                    else if (Math.Abs(ln.Direction.X) > 0.999) refsY.Add(Tuple.Create(ln.Origin.Y, ln.Reference, true)); } }
            // meio-fio da skill ("VZ-Meio-Fio…"): faces ao longo da testada (pontos do rebaixo) e transversais (guia, sarjeta)
            var mfX = new List<Tuple<double, Reference, bool>>(); var mfY = new List<Tuple<double, Reference, bool>>(); BoundingBoxXYZ mfCaixa = null;
            foreach (var mf in new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(i => i.Symbol.FamilyName.ToUpper().StartsWith("VZ-MEIO-FIO"))) {
                // a família gerada por 06_meio_fio.cs tem planos de referência nomeados: MF_U_<u> (ao longo da testada) e MF_W_<peça>_<w>
                mfCaixa = mf.get_BoundingBox(null); var T = mf.GetTotalTransform();
                var rs = new List<Reference>(); foreach (var rt in new[] { FamilyInstanceReferenceType.WeakReference, FamilyInstanceReferenceType.StrongReference }) rs.AddRange(mf.GetReferences(rt));
                foreach (var r in rs) { var nm = mf.GetReferenceName(r) ?? ""; var pt = nm.Split('_'); if (pt.Length < 3 || pt[0] != "MF") continue;
                    double val; if (!double.TryParse(pt[pt.Length - 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val)) continue;
                    XYZ p0, nrm; if (pt[1] == "U") { p0 = T.OfPoint(new XYZ(F(val), 0, 0)); nrm = T.BasisX; } else { p0 = T.OfPoint(new XYZ(0, -F(val), 0)); nrm = T.BasisY; }
                    if (Math.Abs(nrm.X) > 0.999) mfX.Add(Tuple.Create(p0.X, r, false)); else if (Math.Abs(nrm.Y) > 0.999) mfY.Add(Tuple.Create(p0.Y, r, false)); } }
            // ordena, descarta o que está fora do lote e junta coordenadas a menos de 1 cm (a divisa tem prioridade)
            Func<List<Tuple<double, Reference, bool>>, double, double, List<Tuple<double, Reference, bool>>> Cadeia = (lst, a, bfim) => {
                var res = new List<Tuple<double, Reference, bool>>();
                foreach (var r in lst.Where(q => q.Item1 > a - tol && q.Item1 < bfim + tol).OrderBy(q => q.Item1).ThenByDescending(q => q.Item3)) {
                    if (res.Count > 0 && r.Item1 - res[res.Count - 1].Item1 < F(0.01)) { if (r.Item3 && !res[res.Count - 1].Item3) res[res.Count - 1] = r; continue; } res.Add(r); }
                return res;
            };
            var cadX = Cadeia(refsX, lx0, lx1); var cadY = Cadeia(refsY, ly0, ly1);
            // blocos: muros + as faces mais externas da edificação em cada eixo
            Func<List<Tuple<double, Reference, bool>>, List<Tuple<double, Reference, bool>>> Extremos = l => { if (l.Count == 0) return l; double mn = l.Min(q => q.Item1), mx = l.Max(q => q.Item1); return l.Where(q => q.Item1 < mn + F(0.01) || q.Item1 > mx - F(0.01)).ToList(); };
            blocoX.AddRange(Extremos(casaX)); blocoY.AddRange(Extremos(casaY));
            var bloX = Cadeia(blocoX, lx0, lx1); var bloY = Cadeia(blocoY, ly0, ly1);
            // micro: vãos das portas e janelas externas (um lado sem ambiente), pelas referências Esquerda/Direita da família,
            // na face do lote para a qual a fachada está voltada (pelo centro da edificação)
            var extCasaX = Extremos(casaX); var extCasaY = Extremos(casaY);
            double cxCasa = casaX.Count > 0 ? (casaX.Min(q => q.Item1) + casaX.Max(q => q.Item1)) / 2 : (lx0 + lx1) / 2;
            double cyCasa = casaY.Count > 0 ? (casaY.Min(q => q.Item1) + casaY.Max(q => q.Item1)) / 2 : (ly0 + ly1) / 2;
            Phase faseV = null; try { faseV = document.GetElement(vista.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId()) as Phase; } catch { }
            var vaosF = new Dictionary<string, List<Tuple<double, Reference, bool>>> { { "frente", new List<Tuple<double, Reference, bool>>() }, { "fundo", new List<Tuple<double, Reference, bool>>() }, { "esquerda", new List<Tuple<double, Reference, bool>>() }, { "direita", new List<Tuple<double, Reference, bool>>() } };
            int vaosSemRef = 0, nVaos = 0;
            var esquadrias = new FilteredElementCollector(document, vista.Id).WherePasses(new LogicalOrFilter(new ElementCategoryFilter(BuiltInCategory.OST_Doors), new ElementCategoryFilter(BuiltInCategory.OST_Windows))).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
            foreach (var fiV in esquadrias) {
                Autodesk.Revit.DB.Architecture.Room rDe = null, rPara = null;
                try { if (faseV != null) { rDe = fiV.get_FromRoom(faseV); rPara = fiV.get_ToRoom(faseV); } else { rDe = fiV.FromRoom; rPara = fiV.ToRoom; } } catch { }
                if (rDe != null && rPara != null) {   // vão interno — salvo se um dos lados é ambiente aberto (varanda, sacada…)
                    var nDe = (rDe.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper(); var nPa = (rPara.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper();
                    if (!exterioresL.Any(e => nDe.Contains(e)) && !exterioresL.Any(e => nPa.Contains(e))) continue; }
                var lpV = fiV.Location as LocationPoint; if (lpV == null) continue;
                var mao = new XYZ(fiV.HandOrientation.X, fiV.HandOrientation.Y, 0); if (mao.GetLength() < 0.5) continue; mao = mao.Normalize();
                double wV = 0; foreach (var bip in new[] { BuiltInParameter.DOOR_WIDTH, BuiltInParameter.WINDOW_WIDTH, BuiltInParameter.FAMILY_WIDTH_PARAM, BuiltInParameter.FAMILY_ROUGH_WIDTH_PARAM }) {
                    var pw = fiV.get_Parameter(bip); if (pw == null || !pw.HasValue || pw.AsDouble() <= 0) pw = fiV.Symbol.get_Parameter(bip); if (pw != null && pw.HasValue && pw.AsDouble() > 0) { wV = pw.AsDouble(); break; } }
                if (wV <= 0) { vaosSemRef++; continue; }
                IList<Reference> rEsq = null, rDir = null; try { rEsq = fiV.GetReferences(FamilyInstanceReferenceType.Left); rDir = fiV.GetReferences(FamilyInstanceReferenceType.Right); } catch { }
                if (rEsq == null || rDir == null || rEsq.Count == 0 || rDir.Count == 0) { vaosSemRef++; continue; }
                var pE = lpV.Point - mao.Multiply(wV / 2); var pD = lpV.Point + mao.Multiply(wV / 2);
                string faceV;
                if (Math.Abs(mao.X) > 0.99) faceV = lpV.Point.Y < cyCasa ? "frente" : "fundo";
                else if (Math.Abs(mao.Y) > 0.99) faceV = lpV.Point.X < cxCasa ? "esquerda" : "direita";
                else continue;   // parede inclinada: fica sem micro
                bool hz = faceV == "frente" || faceV == "fundo";
                vaosF[faceV].Add(Tuple.Create(hz ? pE.X : pE.Y, rEsq[0], false)); vaosF[faceV].Add(Tuple.Create(hz ? pD.X : pD.Y, rDir[0], false)); nVaos++;
            }
            // três linhas por face (checklist): micro (vãos) a 5 mm do que estiver desenhado, médio (ambientes) e macro (total) a cada 6,5 mm no papel
            double esc = vista.Scale, o1 = F(0.005 * esc), o2 = F(0.0115 * esc), o3 = F(0.018 * esc);
            // a linha fica fora do lote E de tudo o que está modelado além dele (calçada, passeio): usa a caixa de paredes e pisos da vista
            double ex0 = lx0, ey0 = ly0, ex1 = lx1, ey1 = ly1;
            foreach (var el in elems) { var b = el.get_BoundingBox(null); if (b == null) continue; if (nivel != null && (b.Max.Z < nivel.Elevation - F(0.5) || b.Min.Z > nivel.Elevation + F(2.0))) continue;
                if (b.Max.X - b.Min.X > F(200) || b.Max.Y - b.Min.Y > F(200)) continue;   // topografia/piso gigante não empurra as cotas
                double cxE = (b.Min.X + b.Max.X) / 2, cyE = (b.Min.Y + b.Max.Y) / 2; if (cxE < lx0 - tol || cxE > lx1 + tol || cyE < ly0 - tol || cyE > ly1 + tol) continue;   // rua/asfalto fora do lote não empurram
                ex0 = Math.Min(ex0, b.Min.X); ey0 = Math.Min(ey0, b.Min.Y); ex1 = Math.Max(ex1, b.Max.X); ey1 = Math.Max(ey1, b.Max.Y); }
            if (mfCaixa != null) { ex0 = Math.Min(ex0, mfCaixa.Min.X); ey0 = Math.Min(ey0, mfCaixa.Min.Y); ex1 = Math.Max(ex1, mfCaixa.Max.X); ey1 = Math.Max(ey1, mfCaixa.Max.Y); }   // do lado da rua, depois do meio-fio e da sarjeta
            if (calcL != null) { var cbC = calcL.get_BoundingBox(null); ex0 = Math.Min(ex0, cbC.Min.X); ey0 = Math.Min(ey0, cbC.Min.Y); ex1 = Math.Max(ex1, cbC.Max.X); ey1 = Math.Max(ey1, cbC.Max.Y); }   // regra: do lado da calçada, cotas depois da calçada
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
            // lado da rua (onde está a CALÇADA) e a face externa da guia (fim da calçada)
            string ladoRua = null; double guiaExt = double.NaN;
            if (calcL != null) { var cbR = calcL.get_BoundingBox(null);
                if (cbR.Min.X >= lx1 - tol) { ladoRua = "direita"; guiaExt = cbR.Max.X; } else if (cbR.Max.X <= lx0 + tol) { ladoRua = "esquerda"; guiaExt = cbR.Min.X; }
                else if (cbR.Min.Y >= ly1 - tol) { ladoRua = "fundo"; guiaExt = cbR.Max.Y; } else if (cbR.Max.Y <= ly0 + tol) { ladoRua = "frente"; guiaExt = cbR.Min.Y; } }
            bool ruaEmX = ladoRua == "direita" || ladoRua == "esquerda";
            foreach (var face in new[] { "frente", "fundo", "esquerda", "direita" }) {
                bool horiz = face == "frente" || face == "fundo";   // "frente" aqui = lado de menor Y (nome interno)
                var cad = horiz ? cadX : cadY; if (cad.Count < 2) { avisos.Add(face + ": sem referências"); continue; }
                double ini = horiz ? lx0 : ly0, fim = horiz ? lx1 : ly1;
                if (cad[0].Item1 - ini > F(0.05) || fim - cad[cad.Count - 1].Item1 > F(0.05)) avisos.Add(face + ": a cadeia não chega até a divisa (falta muro ou linha de propriedade nesse trecho" + (superior ? "; no pavimento superior, deixe as linhas de propriedade visíveis ou use o térreo como subposição" : "") + ")");
                double baseF = face == "frente" ? ey0 : face == "fundo" ? ey1 : face == "esquerda" ? ex0 : ex1; double sinal = (face == "frente" || face == "esquerda") ? -1 : 1;
                Func<int, double> Pos = k => baseF + sinal * F((0.005 + 0.0065 * k) * esc);   // 5 mm do desenho e 6,5 mm entre linhas, no papel
                Func<List<Tuple<double, Reference, bool>>, double, Line> L = (cd, pos) => { double a0 = Math.Min(ini, cd.Min(q => q.Item1)), a1 = Math.Max(fim, cd.Max(q => q.Item1));
                    return horiz ? Line.CreateBound(new XYZ(a0, pos, z), new XYZ(a1, pos, z)) : Line.CreateBound(new XYZ(pos, a0, z), new XYZ(pos, a1, z)); };
                Func<Dimension, Dimension> Marcar = d => { if (d != null) { nCot++; criadasExt.Add(d); fora[d.Id] = face == "frente" ? -XYZ.BasisY : face == "fundo" ? XYZ.BasisY : face == "esquerda" ? -XYZ.BasisX : XYZ.BasisX; } return d; };
                var linhas = new List<List<Tuple<double, Reference, bool>>>();
                // 0) rebaixo do meio-fio: só na face da rua, ao longo da testada (divisa → abas → pleno → divisa)
                if (face == ladoRua) { var pts = (horiz ? mfX : mfY).Where(q => q.Item1 > cad[0].Item1 + F(0.01) && q.Item1 < cad[cad.Count - 1].Item1 - F(0.01)).ToList();
                    if (pts.Count > 0) { var rb = new List<Tuple<double, Reference, bool>> { cad[0] }; rb.AddRange(pts); rb.Add(cad[cad.Count - 1]); linhas.Add(Cadeia(rb, ini, fim)); } }
                // 1) micro: divisas + extremos da edificação + vãos da fachada (só se houver vão externo nesta face)
                if (vaosF[face].Count > 0) {
                    var mic = new List<Tuple<double, Reference, bool>> { cad[0] };
                    mic.AddRange((horiz ? extCasaX : extCasaY).Where(q => q.Item1 > cad[0].Item1 + F(0.01) && q.Item1 < cad[cad.Count - 1].Item1 - F(0.01)));
                    mic.AddRange(vaosF[face].Where(q => q.Item1 > cad[0].Item1 + F(0.01) && q.Item1 < cad[cad.Count - 1].Item1 - F(0.01)));
                    mic.Add(cad[cad.Count - 1]); linhas.Add(Cadeia(mic, ini, fim)); }
                // 2) médio: ambiente por ambiente; nas faces perpendiculares à rua segue até a face externa da guia (largura da calçada)
                var med = new List<Tuple<double, Reference, bool>>(cad);
                if (ladoRua != null && horiz == ruaEmX && !double.IsNaN(guiaExt)) {
                    var gRef = (horiz ? mfX : mfY).OrderBy(q => Math.Abs(q.Item1 - guiaExt)).FirstOrDefault();
                    if (gRef != null && Math.Abs(gRef.Item1 - guiaExt) < F(0.02)) med.Add(gRef); else avisos.Add(face + ": sem meio-fio da skill — largura da calçada não cotada (rode 06_meio_fio.cs)");
                    med = med.OrderBy(q => q.Item1).ToList(); }
                linhas.Add(med);
                // 3) macro: total do terreno
                linhas.Add(new List<Tuple<double, Reference, bool>> { cad[0], cad[cad.Count - 1] });
                for (int k = 0; k < linhas.Count; k++) { var d = Marcar(Cotar(linhas[k], L(linhas[k], Pos(k)))); if (d == null) avisos.Add(face + ": linha " + (k + 1) + " não criada"); }
            }
            document.Regenerate();
            // trecho curto (texto mais largo que o trecho, ~6 mm no papel): textos alternados — um na linha, o seguinte deslocado para fora
            double curto = F(0.006 * esc), passo = F(0.0035 * esc), folga = F(0.003 * esc);
            // trecho curto (texto mais largo que o trecho): o texto sai ao longo da linha para o lado livre — para fora nas pontas da
            // cadeia, para o lado do vizinho comprido no meio; só quando os dois vizinhos são curtos ele é afastado para fora da linha
            foreach (var d in criadasExt) {
                if (d.Segments.Size == 0) { nSeg++; continue; }
                var sgs = new List<DimensionSegment>(); foreach (DimensionSegment sg in d.Segments) sgs.Add(sg);
                var dl = (d.Curve as Line).Direction; if (sgs.Count > 1 && (sgs[sgs.Count - 1].Origin - sgs[0].Origin).DotProduct(dl) < 0) dl = -dl;
                Func<int, double> Val = i => i < 0 || i >= sgs.Count || !sgs[i].Value.HasValue ? double.MaxValue : sgs[i].Value.Value;
                for (int i = 0; i < sgs.Count; i++) { nSeg++; var sg = sgs[i]; if (!sg.Value.HasValue) continue;
                    if (sg.Value.Value < F(0.001)) { try { sg.ValueOverride = " "; } catch { } continue; }
                    if (sg.Value.Value >= curto) continue; curtos++;
                    bool pc = i > 0 && Val(i - 1) < curto, nc = i < sgs.Count - 1 && Val(i + 1) < curto; int lado;
                    if (i == 0) lado = -1; else if (i == sgs.Count - 1) lado = 1; else if (pc && !nc) lado = 1; else if (!pc && nc) lado = -1; else if (!pc && !nc) lado = Val(i + 1) >= Val(i - 1) ? 1 : -1; else lado = 0;
                    try { sg.TextPosition = lado != 0 ? sg.TextPosition + dl.Multiply(lado * (sg.Value.Value / 2 + folga)) : (fora.ContainsKey(d.Id) ? sg.TextPosition + fora[d.Id].Multiply(passo) : sg.TextPosition); } catch { } }
            }
            // recorte ativo que não alcança as cotas: amplia até o lote + linha do total + 1 m
            {   // recorte: ativo e justo às cotas (o recorte herdado do template pode estar desligado ou enorme)
                var cbv = vista.CropBox; var inv = cbv.Transform.Inverse; double mg = F((0.005 + 0.0065 * 4) * esc) + F(1.0);   // até 4 linhas (rebaixo, vãos, ambientes, total) + 1 m
                var c0 = inv.OfPoint(new XYZ(ex0 - mg, ey0 - mg, z)); var c1 = inv.OfPoint(new XYZ(ex1 + mg, ey1 + mg, z));
                cbv.Min = new XYZ(Math.Min(c0.X, c1.X), Math.Min(c0.Y, c1.Y), cbv.Min.Z); cbv.Max = new XYZ(Math.Max(c0.X, c1.X), Math.Max(c0.Y, c1.Y), cbv.Max.Z);
                try { vista.CropBox = cbv; if (!vista.CropBoxActive) { vista.CropBoxActive = true; avisos.Add("recorte da vista estava desligado — ligado e ajustado às cotas"); } vista.CropBoxVisible = false;
                      var pAn = vista.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE); if (pAn != null && !pAn.IsReadOnly) pAn.Set(1); } catch { }
            }
            if (vaosSemRef > 0) avisos.Add(vaosSemRef + " vão(s) externo(s) sem referências Esquerda/Direita ou largura na família — ficaram fora da linha dos vãos");
            if (superior && descartadas > 0) avisos.Add("pavimento superior: " + descartadas + " referências do térreo (muros/divisas) não aparecem nesta vista e ficaram de fora");
            log.AppendLine("cotas externas: " + nCot + " linhas (vãos, ambientes e total em cada face; " + nVaos + " vãos externos), " + nSeg + " trechos; lote " + Math.Round((lx1 - lx0) * 0.3048, 2) + " × " + Math.Round((ly1 - ly0) * 0.3048, 2) + " m"
                + (curtos > 0 ? "; " + curtos + " trechos curtos com texto escalonado (confira)" : "") + (descartadas > 0 ? "; " + descartadas + " referências não cotáveis descartadas" : "")
                + (tipoExt == null ? " (sem tipo de cota linear)" : " (tipo \"" + tipoExt.Name + "\")") + (avisos.Count > 0 ? "\n  " + string.Join("\n  ", avisos) : ""));
        }
        }
    }
    t.Commit();
}
return "Planta \"" + nomeVista + "\" — " + rooms.Count + " ambientes no nível " + (nivel != null ? nivel.Name : "?") + "\n" + log.ToString();
