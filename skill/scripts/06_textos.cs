// SKILL EXECUTIVO REVIT — Fase 6: anotações de texto das pranchas, no padrão do projeto de referência
// send_code_to_revit · transactionMode "none"
// O que a referência anota, por prancha, e o que este script gera a partir do modelo:
//   executivo  — "PROJEÇÃO COBERTURA" sobre a projeção do telhado
//   cobertura  — tipo da telha + inclinação ("TELHA METÁLICA  i = 10%") no centro de cada água; "PROJEÇÃO EDIFICAÇÃO"
//   corte      — chamadas com linha de chamada (vermelho) dos elementos cortados: telha, laje, forro, pingadeira (nome do
//                tipo); em cada ambiente cortado: "REVEST. <acabamento da parede>" e "PISO <acabamento do piso>"
//   fachada    — chamadas dos materiais de acabamento externo das paredes vistas e do telhado
//   forro      — "FORRO= 2,80m" sob o nome de cada ambiente (altura do forro sobre o piso)
//   tomadas / iluminacao — rótulo com linha de chamada nas peças que fogem do padrão da legenda: altura diferente de
//                0,30 / 1,10 / 2,20 m ("Tomada h= 70cm") ou com Comentários preenchidos ("Ponto p/ geladeira h= 1,10m")
//   bancada    — vista de montagem da bancada (DET): chamadas das louças/metais (família + tipo + fabricante) e do material
//                da bancada; nota "Medidas livres, não considerado 2cm embutimento." (padrão da referência)
// Fonte dos textos: nome do tipo (sem o prefixo "VZ-"/"Acabamento"), parâmetros "Acabamento do piso/da parede" do
// ambiente, camada de acabamento da parede e Comentários das peças. O que não está no modelo vira aviso, não texto inventado.
// parameters[0] = vistas separadas por ";" ("BANCADAS" = elevações de todas as montagens de bancada)
// parameters[1] = modo (padrão "auto": pelo nome da vista/modelo de vista)
// Chamadas de corte/fachada ficam numa coluna à direita do desenho, escalonadas de 4 mm (à esquerda ficam os níveis).
// parameters[2] = "refazer" apaga antes os textos do tipo de chamada da skill (vermelho) e os de "FORRO=" nessa vista
// Tipos de texto: chamada = tipo vermelho de ~1,8 mm ("ARIAL 1,8 mm VERMELHO"); nota = preto de ~1,8 mm.
// Uma transação "Skill: textos" (Ctrl+Z desfaz). Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var nomes = P(0).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
if (nomes.Count == 0) return "ERRO: informe as vistas em parameters[0].";
var modoPedido = P(1) == "" ? "auto" : P(1).ToLower(); bool refazer = P(2).ToLower() == "refazer";
Func<string, string> SemAcento = s => new string((s ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
// tipos de texto
var tiposT = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
Func<TextNoteType, double> Tam = tt => (tt.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0) * 304.8;
Func<TextNoteType, int> Cor = tt => tt.get_Parameter(BuiltInParameter.LINE_COLOR)?.AsInteger() ?? 0;
Func<TextNoteType, bool> Vermelho = tt => { int c = Cor(tt); return (c & 255) > 120 && ((c >> 8) & 255) < 60 && ((c >> 16) & 255) < 60; };
var tChamada = tiposT.Where(Vermelho).Where(tt => Tam(tt) >= 1.5).OrderBy(tt => Math.Abs(Tam(tt) - 1.8)).FirstOrDefault() ?? tiposT.OrderBy(tt => Math.Abs(Tam(tt) - 1.8)).First();
var tNota = tiposT.Where(tt => !Vermelho(tt) && Tam(tt) >= 1.5).OrderBy(tt => Math.Abs(Tam(tt) - 1.8)).ThenBy(tt => SemAcento(tt.Name).Contains("PRETO") ? 0 : 1).FirstOrDefault() ?? tChamada;
// nome limpo de um tipo: sem prefixo do escritório, sem "Acabamento", em maiúsculas
Func<string, string> Limpo = n => { var s = n ?? ""; s = System.Text.RegularExpressions.Regex.Replace(s, @"^(VZ|CBC)[-_ ]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    s = System.Text.RegularExpressions.Regex.Replace(s, @"^Acabamento\s+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase); return s.Trim().ToUpper(); };
Func<string, string> NomeLaje = n => SemAcento(n).Contains("LAJE") ? "LAJE DE CONCRETO ARMADO" : Limpo(n);
Func<View, string> Modo = v => {
    if (modoPedido != "auto") return modoPedido;
    var tn = v.ViewTemplateId != ElementId.InvalidElementId ? SemAcento(document.GetElement(v.ViewTemplateId).Name) : ""; var n = SemAcento(v.Name) + " " + tn;
    if (v.IsAssemblyView && v.ViewType != ViewType.ThreeD && Math.Abs(v.ViewDirection.Z) < 0.1 && document.GetElement(v.AssociatedAssemblyInstanceId) is AssemblyInstance ai && ai.GetMemberIds().Any(i => document.GetElement(i)?.Category?.Id.Value == (long)BuiltInCategory.OST_Casework)) return "bancada";
    if (v.ViewType == ViewType.Section) return "corte";
    if (v.ViewType == ViewType.Elevation && n.Contains("FACHADA")) return "fachada";
    if (n.Contains("TOMADA") || n.Contains("PONTOS ELETRICOS")) return "tomadas";
    if (n.Contains("ILUMINA") || n.Contains("LUMINOTEC")) return "iluminacao";
    if (v.ViewType == ViewType.CeilingPlan || n.Contains("FORRO")) return "forro";
    if (n.Contains("COBERTURA")) return "cobertura";
    if (n.Contains("EXECUTIVO")) return "executivo";
    return "";
};
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
Func<Autodesk.Revit.DB.Architecture.Room, string> NomeAmb = r => r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
var pisos = new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>().ToList();
var forros = new FilteredElementCollector(document).OfClass(typeof(Ceiling)).Cast<Ceiling>().ToList();
var paredes = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().ToList();
// elemento de uma lista cuja caixa contém o ponto (em planta), opcionalmente numa faixa de Z
Func<IEnumerable<Element>, XYZ, double, double, Element> Sob = (els, p, z0, z1) => els.FirstOrDefault(e => { var b = e.get_BoundingBox(null); return b != null && p.X >= b.Min.X && p.X <= b.Max.X && p.Y >= b.Min.Y && p.Y <= b.Max.Y && b.Max.Z >= z0 && b.Min.Z <= z1; });
// acabamento do piso/parede de um ambiente: parâmetro do ambiente; senão o piso de acabamento sob ele / a camada de acabamento da parede
Func<Autodesk.Revit.DB.Architecture.Room, string> AcabPiso = r => {
    var s = r.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.AsString(); if (!string.IsNullOrWhiteSpace(s)) return s.ToUpper();
    var lp = r.Location as LocationPoint; if (lp == null) return null; var b = r.get_BoundingBox(null);
    var f = Sob(pisos.Where(x => !SemAcento(document.GetElement(x.GetTypeId())?.Name).Contains("LAJE")), lp.Point, b.Min.Z - F(0.3), b.Min.Z + F(0.3));
    return f == null ? null : Limpo(document.GetElement(f.GetTypeId()).Name);
};
Func<Autodesk.Revit.DB.Architecture.Room, string> AcabParede = r => {
    var s = r.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.AsString(); if (!string.IsNullOrWhiteSpace(s)) return s.ToUpper();
    var segs = r.GetBoundarySegments(new SpatialElementBoundaryOptions()); if (segs == null) return null;
    foreach (var lp in segs) foreach (var sg in lp) { var w = document.GetElement(sg.ElementId) as Wall; var cs = w?.WallType.GetCompoundStructure(); if (cs == null) continue;
        var cam = cs.GetLayers(); if (cam.Count < 2) continue;
        foreach (var c in new[] { cam.First(), cam.Last() }) if (c.Function == MaterialFunctionAssignment.Finish1 || c.Function == MaterialFunctionAssignment.Finish2) { var m = document.GetElement(c.MaterialId) as Material; if (m != null) return Limpo(m.Name); } }
    return null;
};

using (var t = new Transaction(document, "Skill: textos")) {
    t.Start();
    // "BANCADAS" = todas as elevações das montagens de bancada (os nomes das vistas de montagem se repetem)
    var todasV = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
    var alvosV = new List<Tuple<string, View>>();
    foreach (var nm in nomes) {
        if (nm.ToUpper() == "BANCADAS") { foreach (var v in todasV.Where(x => x.IsAssemblyView && Modo(x) == "bancada")) alvosV.Add(Tuple.Create((document.GetElement(v.AssociatedAssemblyInstanceId) as AssemblyInstance)?.AssemblyTypeName + " / " + v.Name, v)); }
        else alvosV.Add(Tuple.Create(nm, todasV.FirstOrDefault(v => v.Name == nm)));
    }
    foreach (var alvoV in alvosV) {
        var nome = alvoV.Item1; var vista = alvoV.Item2;
        if (vista == null) { log.AppendLine(nome + ": vista não encontrada"); continue; }
        var modo = Modo(vista); if (modo == "") { log.AppendLine(nome + ": modo não reconhecido (informe em parameters[1])"); continue; }
        double esc = vista.Scale; Func<double, double> Pap = mm => F(mm / 1000.0 * esc);   // mm no papel → pés no modelo
        var existentes = new FilteredElementCollector(document, vista.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(x => x.GetTypeId() == tChamada.Id || x.Text.StartsWith("FORRO=") || x.Text.StartsWith("PROJEÇÃO ") || x.Text.StartsWith("Medidas livres")
            || (modo == "corte" && x.GetTypeId() == tNota.Id && (x.Text.StartsWith("REVEST. ") || x.Text.StartsWith("PISO "))) || (modo == "cobertura" && x.GetTypeId() == tNota.Id && x.Text.Contains("i = "))).ToList();
        if (existentes.Count > 0) { if (refazer) document.Delete(existentes.Select(x => x.Id).ToList()); else { log.AppendLine(nome + " [" + modo + "]: já tem " + existentes.Count + " anotações da skill — pulada (use \"refazer\")"); continue; } }
        int n = 0; var avisos = new List<string>();
        var optE = new TextNoteOptions(tChamada.Id) { HorizontalAlignment = HorizontalTextAlignment.Left };
        var optC = new TextNoteOptions(tNota.Id) { HorizontalAlignment = HorizontalTextAlignment.Center };
        // texto com linha de chamada até o ponto "alvo"
        Action<string, XYZ, XYZ> Chamada = (txt, pos, alvo) => { try { var tn = TextNote.Create(document, vista.Id, pos, txt, optE); var ld = tn.AddLeader(TextNoteLeaderTypes.TNLT_STRAIGHT_L); ld.End = alvo; n++; } catch (Exception ex) { avisos.Add("\"" + txt + "\": " + ex.Message); } };
        Action<string, XYZ, TextNoteOptions> Texto = (txt, pos, op) => { try { TextNote.Create(document, vista.Id, pos, txt, op); n++; } catch (Exception ex) { avisos.Add("\"" + txt + "\": " + ex.Message); } };
        var nivel = (vista as ViewPlan)?.GenLevel; double z = nivel != null ? nivel.Elevation : 0;
        var noNivel = rooms.Where(r => nivel != null && r.LevelId == nivel.Id).ToList();

        if (modo == "executivo" || modo == "cobertura") {
            var tel = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType().ToElements();
            var casa = paredes.Where(w => { var b = w.get_BoundingBox(null); return b != null && b.Max.Z - b.Min.Z > F(3.0); }).Select(w => w.get_BoundingBox(null)).ToList();
            if (modo == "executivo") {
                foreach (var r in tel) { var b = r.get_BoundingBox(null); if (b == null) continue;
                    Texto("PROJEÇÃO COBERTURA", new XYZ((b.Min.X + b.Max.X) / 2, b.Max.Y - Pap(1.5), z), optC); }
                if (tel.Count == 0) avisos.Add("sem telhado: \"PROJEÇÃO COBERTURA\" não colocada");
            } else {
                foreach (var r in tel) { var b = r.get_BoundingBox(null); if (b == null) continue;
                    var fp = r as FootPrintRoof; string incl = "";
                    if (fp != null) { foreach (ModelCurve mc in fp.GetProfiles().get_Item(0)) { if (fp.get_DefinesSlope(mc)) { var sl = fp.get_SlopeAngle(mc); incl = "  i = " + Math.Round(sl * 100) + "%"; break; } } }
                    Texto(Limpo(document.GetElement(r.GetTypeId()).Name) + incl, new XYZ((b.Min.X + b.Max.X) / 2, (b.Min.Y + b.Max.Y) / 2, b.Max.Z), optC); }
                if (casa.Count > 0) { double x0 = casa.Min(b => b.Min.X), x1 = casa.Max(b => b.Max.X), y1 = casa.Max(b => b.Max.Y);
                    Texto("PROJEÇÃO EDIFICAÇÃO", new XYZ((x0 + x1) / 2, y1 - Pap(1.5), z), optC); }
            }
        }
        else if (modo == "forro") {
            foreach (var r in noNivel) {
                var lp = r.Location as LocationPoint; if (lp == null) continue; var b = r.get_BoundingBox(null);
                var c = Sob(forros.Cast<Element>(), lp.Point, b.Min.Z + F(1.0), b.Max.Z + F(1.0));
                if (c == null) { avisos.Add(NomeAmb(r) + ": sem forro"); continue; }
                double h = (c.get_BoundingBox(null).Min.Z - b.Min.Z) * 0.3048;
                Texto("FORRO= " + h.ToString("0.00", new System.Globalization.CultureInfo("pt-BR")) + "m", new XYZ(lp.Point.X, lp.Point.Y - Pap(6), z), optC);
            }
        }
        else if (modo == "tomadas" || modo == "iluminacao") {
            var cats = modo == "tomadas" ? new[] { BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_LightingDevices, BuiltInCategory.OST_ElectricalEquipment } : new[] { BuiltInCategory.OST_LightingFixtures };
            var padrao = new[] { 0.30, 1.10, 2.20 }; var jaRot = new HashSet<string>();
            foreach (var c in cats) foreach (var fi in new FilteredElementCollector(document, vista.Id).OfCategory(c).WhereElementIsNotElementType().OfType<FamilyInstance>()) {
                var lp = fi.Location as LocationPoint; if (lp == null) continue;
                var com = fi.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString(); if (string.IsNullOrWhiteSpace(com)) com = fi.Symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS)?.AsString();
                // altura real do ponto sobre o nível (o parâmetro de elevação de família hospedada em face vem 0)
                double h = nivel != null ? Math.Round((lp.Point.Z - nivel.Elevation) * 0.3048, 2) : -1;
                var chave = Math.Round(lp.Point.X, 2) + "|" + Math.Round(lp.Point.Y, 2); if (!jaRot.Add(chave)) continue;   // tomada dupla: um rótulo só
                bool foraPadrao = modo == "tomadas" && h >= 0 && !padrao.Any(pd => Math.Abs(pd - h) < 0.02);
                if (string.IsNullOrWhiteSpace(com) && !foraPadrao) continue;
                string hTxt = h < 0 ? "" : (h < 1.0 ? Math.Round(h * 100) + "cm" : h.ToString("0.00", new System.Globalization.CultureInfo("pt-BR")) + "m");
                string txt = !string.IsNullOrWhiteSpace(com) ? com.Trim() + (modo == "tomadas" && hTxt != "" && !com.Contains("h=") ? " h= " + hTxt : "") : "Tomada h= " + hTxt;
                var f = new XYZ(fi.FacingOrientation.X, fi.FacingOrientation.Y, 0); if (f.GetLength() < 0.5) f = XYZ.BasisY; f = f.Normalize();
                var pos = lp.Point + f.Multiply(Pap(9)) + new XYZ(Pap(2), 0, 0); Chamada(txt, new XYZ(pos.X, pos.Y, z), new XYZ(lp.Point.X, lp.Point.Y, z));
            }
            if (n == 0) avisos.Add("nenhuma peça fora do padrão ou com Comentários — nada a anotar");
        }
        else if (modo == "corte" || modo == "fachada") {
            var vs = vista as ViewSection; var dirH = vista.RightDirection; var dirV = vista.ViewDirection;
            var orig = vs != null ? vs.CropBox.Transform.Origin : (vista.Origin ?? vista.CropBox.Transform.Origin); double prof = orig.DotProduct(dirV);
            Func<XYZ, XYZ> NoPlano = p => p - dirV.Multiply(p.DotProduct(dirV) - prof);
            var visiveis = new Func<BuiltInCategory, List<Element>>(c => new FilteredElementCollector(document, vista.Id).OfCategory(c).WhereElementIsNotElementType().ToList());
            var paredesV = visiveis(BuiltInCategory.OST_Walls);
            if (paredesV.Count == 0) { log.AppendLine(nome + ": sem paredes visíveis"); continue; }
            double hMin = paredesV.Select(w => w.get_BoundingBox(null)).Where(b => b != null).Min(b => Math.Min(b.Min.DotProduct(dirH), b.Max.DotProduct(dirH)));
            // coluna de chamadas à DIREITA do desenho (à esquerda ficam as cabeças de nível), 12 mm no papel além das paredes
            double hMax = paredesV.Select(w => w.get_BoundingBox(null)).Where(b => b != null).Max(b => Math.Max(b.Min.DotProduct(dirH), b.Max.DotProduct(dirH)));
            double xTxt = hMax + Pap(12);
            // chamadas: (texto, ponto-alvo) — elementos horizontais cortados/vistos
            var alvos = new List<Tuple<string, XYZ>>();
            Func<Element, XYZ> Centro = e => { var b = e.get_BoundingBox(null); return b == null ? null : NoPlano((b.Min + b.Max) / 2); };
            foreach (var r in visiveis(BuiltInCategory.OST_Roofs)) { var c = Centro(r); if (c != null) alvos.Add(Tuple.Create(Limpo(document.GetElement(r.GetTypeId()).Name), c)); }
            foreach (var f in visiveis(BuiltInCategory.OST_Floors).Where(x => SemAcento(document.GetElement(x.GetTypeId())?.Name).Contains("LAJE"))) { var c = Centro(f); if (c != null) alvos.Add(Tuple.Create(NomeLaje(document.GetElement(f.GetTypeId()).Name), c)); }
            if (modo == "corte") foreach (var c0 in visiveis(BuiltInCategory.OST_Ceilings)) { var c = Centro(c0); if (c != null) alvos.Add(Tuple.Create(Limpo(document.GetElement(c0.GetTypeId()).Name), c)); }
            foreach (var g in visiveis(BuiltInCategory.OST_GenericModel).Concat(visiveis(BuiltInCategory.OST_Gutter)).Concat(visiveis(BuiltInCategory.OST_Fascia))) {
                var nm = SemAcento(document.GetElement(g.GetTypeId())?.Name + " " + (g as FamilyInstance)?.Symbol.FamilyName);
                if (nm.Contains("PINGADEIRA") || nm.Contains("CALHA") || nm.Contains("RUFO")) { var c = Centro(g); if (c != null) alvos.Add(Tuple.Create(Limpo(document.GetElement(g.GetTypeId()).Name), c)); } }
            if (modo == "fachada") {
                // acabamento externo das paredes vistas: material da 1ª camada (externa) do tipo
                foreach (var grp in paredesV.OfType<Wall>().GroupBy(w => { var cs = w.WallType.GetCompoundStructure(); var cam = cs?.GetLayers(); return cam != null && cam.Count > 0 ? cam.First().MaterialId : ElementId.InvalidElementId; })) {
                    var m = document.GetElement(grp.Key) as Material; if (m == null) continue;
                    var w0 = grp.OrderByDescending(w => (w.Location as LocationCurve)?.Curve.Length ?? 0).First(); var c = Centro(w0); if (c == null) continue;
                    alvos.Add(Tuple.Create(Limpo(m.Name), c));
                }
            }
            // uma chamada por texto (o elemento mais alto de cada nome), escalonadas a no mínimo 4 mm no papel
            var unicos = alvos.GroupBy(a => a.Item1).Select(g => g.OrderByDescending(a => a.Item2.Z).First()).OrderByDescending(a => a.Item2.Z).ToList();
            double zAnt = double.MaxValue;
            foreach (var a in unicos) {
                double zt = Math.Min(a.Item2.Z, zAnt - Pap(4)); zAnt = zt;
                var p = dirH.Multiply(xTxt) + dirV.Multiply(prof); var pos = new XYZ(p.X, p.Y, zt + Pap(1));
                Chamada(a.Item1, pos, a.Item2);
            }
            // corte: textos de revestimento e piso no meio de cada ambiente cortado
            if (modo == "corte") {
                foreach (var r in rooms) {
                    var bb = r.get_BoundingBox(null); if (bb == null) continue;
                    double hA = Math.Min(bb.Min.DotProduct(dirH), bb.Max.DotProduct(dirH)), hB = Math.Max(bb.Min.DotProduct(dirH), bb.Max.DotProduct(dirH));
                    XYZ pIn = null; for (double h = hA + F(0.2); h < hB; h += F(0.2)) { var q = dirH.Multiply(h) + dirV.Multiply(prof); var q3 = new XYZ(q.X, q.Y, bb.Min.Z + F(1.0)); if (r.IsPointInRoom(q3)) { if (pIn == null) pIn = q3; } }
                    if (pIn == null) continue;
                    // centro do trecho cortado do ambiente
                    double hs = pIn.DotProduct(dirH), he = hs; for (double h = hs; h < hB; h += F(0.1)) { var q = dirH.Multiply(h) + dirV.Multiply(prof); if (r.IsPointInRoom(new XYZ(q.X, q.Y, bb.Min.Z + F(1.0)))) he = h; else break; }
                    if ((he - hs) < F(1.2)) continue;   // trecho cortado estreito demais para o texto
                    var mid = dirH.Multiply((hs + he) / 2) + dirV.Multiply(prof);
                    var ap = AcabParede(r); var pp = AcabPiso(r);
                    if (ap != null) Texto("REVEST. " + ap, new XYZ(mid.X, mid.Y, bb.Min.Z + F(1.6)), optC); else avisos.Add(NomeAmb(r) + ": sem acabamento de parede (preencha \"Acabamento da parede\" no ambiente)");
                    if (pp != null) Texto((pp.StartsWith("PISO") ? "" : "PISO ") + pp, new XYZ(mid.X, mid.Y, bb.Min.Z + Pap(3)), optC); else avisos.Add(NomeAmb(r) + ": sem acabamento de piso");
                }
            }
        }
        else if (modo == "bancada") {
            // vista de montagem (DET): chamadas das louças/metais (nome do tipo) e do material da bancada + nota padrão da referência
            var asm = document.GetElement(vista.AssociatedAssemblyInstanceId) as AssemblyInstance;
            if (asm == null) { log.AppendLine(nome + ": não é vista de montagem"); continue; }
            var dirH = vista.RightDirection; var dirV = vista.ViewDirection; double prof = (vista.Origin ?? vista.CropBox.Transform.Origin).DotProduct(dirV);
            Func<XYZ, XYZ> NoPlano = p => p - dirV.Multiply(p.DotProduct(dirV) - prof);
            var membros = asm.GetMemberIds().Select(i => document.GetElement(i)).Where(e => e != null && e.get_BoundingBox(null) != null).ToList();
            if (membros.Count == 0) { log.AppendLine(nome + ": montagem sem geometria"); continue; }
            // a caixa da montagem pode vir nula: usa a união das caixas dos membros
            var bbA = new BoundingBoxXYZ { Min = new XYZ(membros.Min(e => e.get_BoundingBox(null).Min.X), membros.Min(e => e.get_BoundingBox(null).Min.Y), membros.Min(e => e.get_BoundingBox(null).Min.Z)),
                                           Max = new XYZ(membros.Max(e => e.get_BoundingBox(null).Max.X), membros.Max(e => e.get_BoundingBox(null).Max.Y), membros.Max(e => e.get_BoundingBox(null).Max.Z)) };
            double hMax = Math.Max(bbA.Min.DotProduct(dirH), bbA.Max.DotProduct(dirH)), zTopo = bbA.Max.Z;
            var alvos = new List<Tuple<string, XYZ>>();
            foreach (var e in membros) { var b = e.get_BoundingBox(null); if (b == null) continue; var c = NoPlano((b.Min + b.Max) / 2);
                if (e.Category != null && e.Category.Id.Value == (long)BuiltInCategory.OST_PlumbingFixtures) {
                    var fi = e as FamilyInstance; var fab = fi?.Symbol.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER)?.AsString();
                    // família + tipo, sem repetir (família "Cuba Cozinha Paramétrica", tipo "Cuba cozinha paramétrica" → um só)
                    var famL = fi != null ? Limpo(fi.Symbol.FamilyName) : Limpo(e.Name); var tipL = fi != null ? Limpo(fi.Name) : "";
                    var txt = (tipL == "" || SemAcento(famL).Contains(SemAcento(tipL)) ? famL : SemAcento(tipL).Contains(SemAcento(famL)) ? tipL : famL + " " + tipL).Replace(" PARAMÉTRICA", "").Replace(" PARAMETRICA", "").Trim();
                    if (!string.IsNullOrWhiteSpace(fab) && !SemAcento(txt).Contains(SemAcento(fab))) txt += " - " + fab.ToUpper();
                    alvos.Add(Tuple.Create(txt, c)); }
                else { var mats = e.GetMaterialIds(false).Select(i => document.GetElement(i) as Material).Where(m => m != null).ToList();
                    if (mats.Count > 0) alvos.Add(Tuple.Create(Limpo(mats.OrderByDescending(m => e.GetMaterialArea(m.Id, false)).First().Name), new XYZ(c.X, c.Y, b.Max.Z - F(0.02)))); else avisos.Add(e.Name + ": sem material — preencha o material da bancada"); } }
            double zAnt = double.MaxValue;
            foreach (var a2 in alvos.GroupBy(x => x.Item1).Select(g => g.First()).OrderByDescending(x => x.Item2.Z)) {
                double zt = Math.Min(Math.Max(a2.Item2.Z, zTopo), zAnt - Pap(4)); zAnt = zt; var p = dirH.Multiply(hMax + Pap(10)) + dirV.Multiply(prof);
                Chamada(a2.Item1, new XYZ(p.X, p.Y, zt + Pap(1)), a2.Item2); }
            var pb = dirH.Multiply((Math.Min(bbA.Min.DotProduct(dirH), bbA.Max.DotProduct(dirH)) + hMax) / 2) + dirV.Multiply(prof);
            Texto("Medidas livres, não considerado 2cm embutimento.", new XYZ(pb.X, pb.Y, bbA.Min.Z - Pap(6)), optC);
        }
        else { log.AppendLine(nome + ": modo \"" + modo + "\" desconhecido"); continue; }
        log.AppendLine(nome + " [" + modo + "]: " + n + " textos" + (avisos.Count > 0 ? "\n  " + string.Join("\n  ", avisos.Distinct().Take(12)) : ""));
    }
    t.Commit();
}
return "Tipos: chamada \"" + tChamada.Name + "\", nota \"" + tNota.Name + "\"\n" + log.ToString();
