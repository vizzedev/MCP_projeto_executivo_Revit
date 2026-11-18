// SKILL EXECUTIVO REVIT — Fase 6: meio-fio, sarjeta e rebaixo de acesso de veículos na calçada
// send_code_to_revit · transactionMode "none"
// Regra do escritório: a calçada leva meio-fio modelado como componente genérico, com o rebaixo para carros feito no meio-fio
//   e no perfil da calçada. Rebaixo de veículos com no mínimo 3,00 m de trecho pleno. Padrão (família do template):
//   guia 10 cm de largura, 15 cm de espelho para a sarjeta, 5 cm enterrada; sarjeta 30 cm; no rebaixo a guia fica a 2 cm da
//   sarjeta; rampas laterais (abas) TRIANGULARES de 0,456 m na guia (a rampa fica em trapézio); rampa da calçada com 1,00 m de
//   profundidade junto à guia, com o material da calçada sem hachura e branco 255,255,254 (igual à calçada na planta).
// A família do template "Calçada, Meio-Fio, Rua" NÃO acompanha os parâmetros (meio-fio e sarjeta fixos) — por isso a skill gera a
// família "VZ-Meio-Fio com Rebaixo" com as medidas do lote (sólidos de forma livre), carrega e coloca. Rodar de novo refaz tudo.
// Também refaz a laje da calçada com o recorte da rampa e o asfalto 15 cm abaixo, começando depois da sarjeta.
// parameters[0] = rebaixos, separados por ";": "<distância da divisa esquerda até o início do rebaixo>:<trecho pleno>" em metros,
//                 olhando da rua para o lote (ex.: "0,80:3,00"). A distância inclui a aba. Vazio = sem rebaixo (só meio-fio).
// parameters[1] = opções "chave=valor;…" (opcional): guia=0,10 espelho=0,15 enterrada=0,05 sarjeta=0,30 aba=0,456 rampa=1,00
//                 sobra=0,02 (altura da guia rebaixada sobre a sarjeta)
// Frente do lote = lado da linha de propriedade encostado no ambiente "CALÇADA"; testada e largura da calçada vêm daí.
// Lote e frente ortogonais aos eixos. Uma transação por etapa ("Skill: meio-fio …"). Não salva o projeto.
// --- projeto pelo caminho (regra 5): se o 1º parâmetro for "DOC:<caminho do .rvt>", trabalha nesse arquivo aberto.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
var cult = System.Globalization.CultureInfo.InvariantCulture;
Func<int, string> P = i => parameters != null && parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
Func<string, double> Num = s => double.Parse(s.Trim().Replace(",", "."), cult);
var opc = new Dictionary<string, double> { { "guia", 0.10 }, { "espelho", 0.15 }, { "enterrada", 0.05 }, { "sarjeta", 0.30 }, { "aba", 0.456 }, { "rampa", 1.00 }, { "sobra", 0.02 }, { "laje", 0.10 } };
foreach (var kv in P(1).Split(';').Where(s => s.Contains("="))) { var a = kv.Split('='); opc[a[0].Trim().ToLower()] = Num(a[1]); }
var log = new System.Text.StringBuilder();
// --- lote, calçada e frente
var props = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).OfClass(typeof(PropertyLine)).ToElements()
    .Where(e => !((e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "").ToUpper().Contains("RECUO"))).ToList();
if (props.Count == 0) return "ERRO: sem linha de propriedade do lote.";
var lb = props.OrderByDescending(e => e.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0).First().get_BoundingBox(null);
var calcR = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
    .FirstOrDefault(r => r.Area > 0 && (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper().Replace("Ç", "C").Contains("CALCADA"));
if (calcR == null) return "ERRO: sem ambiente CALÇADA (crie-o na fase 3).";
var cb = calcR.get_BoundingBox(null); double tol = F(0.05);
XYZ n; XYZ origem; double L, C;   // n = normal para a rua; origem = canto da divisa esquerda (olhando da rua) na frente
if (cb.Min.X >= lb.Max.X - tol) { n = XYZ.BasisX; origem = new XYZ(lb.Max.X, lb.Min.Y, 0); L = lb.Max.Y - lb.Min.Y; C = cb.Max.X - lb.Max.X; }
else if (cb.Max.X <= lb.Min.X + tol) { n = -XYZ.BasisX; origem = new XYZ(lb.Min.X, lb.Max.Y, 0); L = lb.Max.Y - lb.Min.Y; C = lb.Min.X - cb.Min.X; }
else if (cb.Min.Y >= lb.Max.Y - tol) { n = XYZ.BasisY; origem = new XYZ(lb.Max.X, lb.Max.Y, 0); L = lb.Max.X - lb.Min.X; C = cb.Max.Y - lb.Max.Y; }
else if (cb.Max.Y <= lb.Min.Y + tol) { n = -XYZ.BasisY; origem = new XYZ(lb.Min.X, lb.Min.Y, 0); L = lb.Max.X - lb.Min.X; C = lb.Min.Y - cb.Min.Y; }
else return "ERRO: a CALÇADA não encosta num lado do lote.";
var uAx = new XYZ(-n.Y, n.X, 0);   // ao longo da testada, da divisa esquerda para a direita (olhando da rua)
// confere o sentido: olhando da rua (direção −n), a esquerda é (n.Y, −n.X)… a origem tem de estar nesse lado
{ var esqDir = new XYZ(n.Y, -n.X, 0); var centro = new XYZ((lb.Min.X + lb.Max.X) / 2, (lb.Min.Y + lb.Max.Y) / 2, 0); if ((origem - centro).DotProduct(esqDir) < 0) uAx = -uAx; }
double Lm = L * 0.3048, Cm = C * 0.3048;
var nivCalc = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => l.Name.ToUpper().Replace("Ç", "C").Contains("CALCADA"));
if (nivCalc == null) return "ERRO: sem nível CALÇADA (rode 05_terra_cortes.cs com a cota da calçada).";
// --- rebaixos em u (metros)
var reb = new List<double[]>();   // a0 (início da aba), a1, a2, a3 (fim da aba)
foreach (var r in P(0).Split(';').Where(s => s.Contains(":"))) { var a = r.Split(':'); double ini = Num(a[0]), pleno = Num(a[1]);
    if (pleno < 3.0) log.AppendLine("⚠ rebaixo de " + pleno.ToString("0.00") + " m: abaixo do mínimo de 3,00 m para veículos");
    double a0 = ini, a1 = ini + opc["aba"], a2 = a1 + pleno, a3 = a2 + opc["aba"];
    if (a0 < 0 || a3 > Lm + 0.001) return "ERRO: rebaixo " + r + " sai da testada (" + Lm.ToString("0.00") + " m).";
    reb.Add(new[] { a0, a1, a2, a3 }); }
double g = opc["guia"], esp = opc["espelho"], ent = opc["enterrada"], sj = opc["sarjeta"], rp = opc["rampa"], sob = opc["sobra"], lj = opc["laje"];
double zGuiaBase = -(esp + ent), zSarj = -esp, zBaixa = -esp + sob;   // em relação ao topo da calçada (0)
double wG0 = Cm - g, wG1 = Cm, wR0 = Cm - g - rp;
// --- sólidos em (u, w, z) metros → família: X = u, Y = −w (rua para −Y), Z = z
var tris = new List<Tuple<XYZ[], string>>();   // triângulos já no sistema da família, com material
Func<double, double, double, XYZ> V = (u, w, z) => new XYZ(F(u), -F(w), F(z));
Action<double[][], string> Prisma = (pts, mat) => {   // pts: 8 vértices (u,w,z): base b0..b3 e topo t0..t3, anti-horário em (u,w) visto de cima
    var p = pts.Select(q => V(q[0], q[1], q[2])).ToArray(); var b = new[] { p[0], p[1], p[2], p[3] }; var t = new[] { p[4], p[5], p[6], p[7] };
    var faces = new List<XYZ[]> { new[] { t[0], t[1], t[2] }, new[] { t[0], t[2], t[3] }, new[] { b[0], b[3], b[2] }, new[] { b[0], b[2], b[1] } };
    for (int i = 0; i < 4; i++) { int j = (i + 1) % 4; faces.Add(new[] { b[i], b[j], t[j] }); faces.Add(new[] { b[i], t[j], t[i] }); }
    foreach (var f in faces) tris.Add(Tuple.Create(new[] { f[0], f[2], f[1] }, mat + "|12"));   // inverte: Y = −w espelha o sentido; 12 triângulos por peça
};
Action<double[][], string> Prisma3 = (pts, mat) => {   // pts: 6 vértices (u,w,z): base b0..b2 e topo t0..t2, anti-horário em (u,w)
    var p = pts.Select(q => V(q[0], q[1], q[2])).ToArray(); var b = new[] { p[0], p[1], p[2] }; var t = new[] { p[3], p[4], p[5] };
    var faces = new List<XYZ[]> { new[] { t[0], t[1], t[2] }, new[] { b[0], b[2], b[1] } };
    for (int i = 0; i < 3; i++) { int j = (i + 1) % 3; faces.Add(new[] { b[i], b[j], t[j] }); faces.Add(new[] { b[i], t[j], t[i] }); }
    foreach (var f in faces) tris.Add(Tuple.Create(new[] { f[0], f[2], f[1] }, mat + "|8"));   // 8 triângulos por peça
};
Func<double, double, double, double, double, double, double, double, double[][]> Caixa = (u0, u1, w0, w1, zb0, zb1, zt0, zt1) => new[] {   // zb/zt: em u0 e u1
    new[] { u0, w0, zb0 }, new[] { u1, w0, zb1 }, new[] { u1, w1, zb1 }, new[] { u0, w1, zb0 }, new[] { u0, w0, zt0 }, new[] { u1, w0, zt1 }, new[] { u1, w1, zt1 }, new[] { u0, w1, zt0 } };
const string MG = "CONCRETO MEIO FIO";
// rampa com o material da calçada (camada de cima da laje), sem hachura e no branco do escritório — igual à calçada na planta
Floor calc0 = new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>().Where(fl => { var b = fl.get_BoundingBox(null); if (b == null) return false;
    var c = (b.Min + b.Max) / 2; return (c - origem).DotProduct(n) > 0 && (c - origem).DotProduct(n) < C && Math.Abs(b.Max.Z - nivCalc.Elevation) < F(0.05); }).FirstOrDefault();
Material mCalc = null; try { var cs = (document.GetElement(calc0.GetTypeId()) as FloorType).GetCompoundStructure(); mCalc = document.GetElement(cs.GetLayers().First().MaterialId) as Material; } catch { }
string MC = (mCalc != null ? mCalc.Name : "VZ-Calçada") + " - REBAIXO";
if (new FilteredElementCollector(document).OfClass(typeof(Material)).FirstOrDefault(x => x.Name == MC) == null)
    using (var t = new Transaction(document, "Skill: material da rampa do rebaixo")) { t.Start();
        var mr = mCalc != null ? mCalc.Duplicate(MC) : document.GetElement(Material.Create(document, MC)) as Material;
        mr.Color = new Color(255, 255, 254); mr.SurfaceForegroundPatternId = ElementId.InvalidElementId; mr.SurfaceBackgroundPatternId = ElementId.InvalidElementId;
        t.Commit(); }
// guia: trechos normais, abas (topo inclinado em u) e rebaixada
var cortes = new List<double> { 0, Lm }; foreach (var r in reb) cortes.AddRange(r); cortes = cortes.Distinct().OrderBy(x => x).ToList();
for (int i = 0; i < cortes.Count - 1; i++) { double u0 = cortes[i], u1 = cortes[i + 1]; if (u1 - u0 < 0.001) continue;
    double z0 = 0, z1 = 0;
    foreach (var r in reb) { if (u0 >= r[0] - 1e-6 && u1 <= r[1] + 1e-6) { z0 = 0; z1 = zBaixa; } else if (u0 >= r[1] - 1e-6 && u1 <= r[2] + 1e-6) { z0 = zBaixa; z1 = zBaixa; } else if (u0 >= r[2] - 1e-6 && u1 <= r[3] + 1e-6) { z0 = zBaixa; z1 = 0; } }
    Prisma(Caixa(u0, u1, wG0, wG1, zGuiaBase, zGuiaBase, z0, z1), MG); }
// sarjeta
Prisma(Caixa(0, Lm, wG1, wG1 + sj, zGuiaBase, zGuiaBase, zSarj, zSarj), MG);
// rampas da calçada: pleno (topo inclinado em w) e abas (topo torcido); espessura da laje constante
foreach (var r in reb) {
    // pleno: perfil em w, prisma de u = a1 a a2
    var pl = new[] { new[] { r[1], wR0, -lj }, new[] { r[2], wR0, -lj }, new[] { r[2], wG0, zBaixa - lj }, new[] { r[1], wG0, zBaixa - lj },
                     new[] { r[1], wR0, 0.0 }, new[] { r[2], wR0, 0.0 }, new[] { r[2], wG0, zBaixa }, new[] { r[1], wG0, zBaixa } };
    Prisma(pl, MC);
    // abas triangulares (regra do escritório): na guia vão de a0 a a1 (e de a2 a a3); no fundo da rampa terminam num ponto
    // aba esquerda: (a0, guia, 0) → (a1, fundo, 0) → (a1, guia, rebaixada) — topo plano
    Prisma3(new[] { new[] { r[0], wG0, -lj }, new[] { r[1], wR0, -lj }, new[] { r[1], wG0, zBaixa - lj },
                    new[] { r[0], wG0, 0.0 }, new[] { r[1], wR0, 0.0 }, new[] { r[1], wG0, zBaixa } }, MC);
    // aba direita: (a2, fundo, 0) → (a3, guia, 0) → (a2, guia, rebaixada)
    Prisma3(new[] { new[] { r[2], wR0, -lj }, new[] { r[3], wG0, -lj }, new[] { r[2], wG0, zBaixa - lj },
                    new[] { r[2], wR0, 0.0 }, new[] { r[3], wG0, 0.0 }, new[] { r[2], wG0, zBaixa } }, MC);
}
// --- família
string nomeFam = "VZ-Meio-Fio com Rebaixo";
string rft = System.IO.Directory.GetFiles(@"C:\ProgramData\Autodesk\RVT " + document.Application.VersionNumber + @"\Family Templates", "*.rft", System.IO.SearchOption.AllDirectories)
    .Where(f => { var nm = System.IO.Path.GetFileName(f).ToLower(); return (nm == "metric generic model.rft" || nm == "modelo genérico métrico.rft" || nm == "modelo generico metrico.rft"); }).FirstOrDefault();
if (rft == null) return "ERRO: template de família 'Metric Generic Model.rft' não encontrado.";
string pasta = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(document.PathName), "_familias_skill"); System.IO.Directory.CreateDirectory(pasta);
string arq = System.IO.Path.Combine(pasta, nomeFam + ".rfa");
var fd = document.Application.NewFamilyDocument(rft);
try {
    using (var t = new Transaction(fd, "Skill: geometria do meio-fio")) {
        t.Start();
        var mats = new Dictionary<string, ElementId>();
        foreach (var nm in new[] { MG, MC }) { var m = new FilteredElementCollector(fd).OfClass(typeof(Material)).FirstOrDefault(x => x.Name == nm); mats[nm] = m != null ? m.Id : Material.Create(fd, nm); }
        int k = 0;
        while (k < tris.Count) {   // cada peça = bloco consecutivo de 12 (prisma) ou 8 (prisma triangular) triângulos
            var chave = tris[k].Item2.Split('|'); string matNome = chave[0]; int nTri = int.Parse(chave[1]);
            {
                var tsb = new TessellatedShapeBuilder(); tsb.OpenConnectedFaceSet(true);
                for (int q = k; q < k + nTri; q++) tsb.AddFace(new TessellatedFace(tris[q].Item1.ToList(), ElementId.InvalidElementId));
                k += nTri;
                tsb.CloseConnectedFaceSet(); tsb.Target = TessellatedShapeBuilderTarget.Solid; tsb.Fallback = TessellatedShapeBuilderFallback.Abort; tsb.Build();
                var so = tsb.GetBuildResult().GetGeometricalObjects().OfType<Solid>().FirstOrDefault(); if (so == null) continue;
                var ff = FreeFormElement.Create(fd, so); ff.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM)?.Set(mats[matNome]);
            }
        }
        // planos de referência nomeados (para cotar no projeto): ao longo da testada (MF_U_<u>) e transversais (MF_W_<peça>_<w>), em metros
        var vPlano = new FilteredElementCollector(fd).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().First(x => !x.IsTemplate);
        Action<string, XYZ, XYZ> RP = (nm, a1, b1) => { var rp = fd.FamilyCreate.NewReferencePlane(a1, b1, XYZ.BasisZ, vPlano); rp.Name = nm; };
        foreach (var u in cortes) RP("MF_U_" + u.ToString("0.000", cult), V(u, -1.0, 0), V(u, Cm + sj + 1.0, 0));
        foreach (var w in new[] { Tuple.Create("RAMPA", wR0), Tuple.Create("GUIAINT", wG0), Tuple.Create("GUIAEXT", wG1), Tuple.Create("SARJETA", wG1 + sj) })
            if (w.Item1 != "RAMPA" || reb.Count > 0) RP("MF_W_" + w.Item1 + "_" + w.Item2.ToString("0.000", cult), V(-1.0, w.Item2, 0), V(Lm + 1.0, w.Item2, 0));
        t.Commit();
    }
    fd.SaveAs(arq, new SaveAsOptions { OverwriteExistingFile = true });
} catch (Exception ex) { fd.Close(false); return "ERRO ao gerar a família: " + ex.Message; }
// apaga a família antiga (instâncias junto) e carrega a nova
var antiga = new FilteredElementCollector(document).OfClass(typeof(Family)).Cast<Family>().FirstOrDefault(f => f.Name == nomeFam);
if (antiga != null) using (var t = new Transaction(document, "Skill: meio-fio — remover versão anterior")) { t.Start(); document.Delete(antiga.Id); t.Commit(); }
Family fam = fd.LoadFamily(document); fd.Close(false);
var sym = document.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
document.Application.FailuresProcessing += __avisos;
try {
    using (var t = new Transaction(document, "Skill: meio-fio, rebaixo e pisos da calçada")) {
        t.Start();
        if (!sym.IsActive) sym.Activate();
        var pto = new XYZ(origem.X, origem.Y, nivCalc.Elevation);
        var fi = document.Create.NewFamilyInstance(pto, sym, nivCalc, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        var po = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM) ?? fi.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM); if (po != null && !po.IsReadOnly) po.Set(0.0);
        double ang = Math.Atan2(uAx.Y, uAx.X); if (Math.Abs(ang) > 1e-9) ElementTransformUtils.RotateElement(document, fi.Id, Line.CreateBound(pto, pto + XYZ.BasisZ), ang);
        log.AppendLine("meio-fio: testada " + Lm.ToString("0.00") + " m, calçada " + Cm.ToString("0.00") + " m, " + reb.Count + " rebaixo(s) [" + fi.Id + "]");
        foreach (var r in reb) log.AppendLine("  rebaixo: abas de " + r[0].ToString("0.00") + " a " + r[3].ToString("0.00") + " m da divisa esquerda; pleno " + (r[2] - r[1]).ToString("0.00") + " m (" + r[1].ToString("0.00") + "–" + r[2].ToString("0.00") + ")");
        // pisos: calçada (até a face interna da guia, com recorte das rampas) e asfalto (depois da sarjeta, 15 cm abaixo)
        Func<double, double, XYZ> W = (u, w) => origem + uAx.Multiply(F(u)) + n.Multiply(F(w));
        var pisosFora = new FilteredElementCollector(document).OfClass(typeof(Floor)).Cast<Floor>().Where(fl => { var b = fl.get_BoundingBox(null); if (b == null) return false;
            var c = (b.Min + b.Max) / 2; return (c - origem).DotProduct(n) > 0 && Math.Abs(b.Max.Z - nivCalc.Elevation) < F(0.3); }).ToList();
        var tCalc = pisosFora.OrderBy(fl => ((fl.get_BoundingBox(null).Min + fl.get_BoundingBox(null).Max) / 2 - origem).DotProduct(n)).FirstOrDefault();
        var tAsf = pisosFora.Where(fl => fl.Name.ToUpper().Contains("ASF")).FirstOrDefault() ?? pisosFora.Where(fl => fl != tCalc).FirstOrDefault();
        double wAsf1 = tAsf != null ? ((tAsf.get_BoundingBox(null).Max - origem).DotProduct(n) > ((tAsf.get_BoundingBox(null).Min - origem).DotProduct(n)) ? (tAsf.get_BoundingBox(null).Max - origem).DotProduct(n) : (tAsf.get_BoundingBox(null).Min - origem).DotProduct(n)) * 0.3048 : Cm + sj + 2.0;
        if (tCalc != null) {
            var pts = new List<XYZ> { W(0, 0), W(Lm, 0), W(Lm, wG0) };
            foreach (var r in reb.OrderByDescending(r => r[0])) { pts.Add(W(r[3], wG0)); pts.Add(W(r[2], wR0)); pts.Add(W(r[1], wR0)); pts.Add(W(r[0], wG0)); }   // recorte em trapézio
            pts.Add(W(0, wG0));
            var lp = new CurveLoop(); for (int i = 0; i < pts.Count; i++) lp.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Count]));
            if (lp.IsCounterclockwise(XYZ.BasisZ) == false) lp.Flip();
            var nf = Floor.Create(document, new List<CurveLoop> { lp }, tCalc.GetTypeId(), nivCalc.Id); nf.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0.0);
            // cotas de nível presas à laje antiga: guarda e recria na nova (mesma vista, ponto e tipo)
            var spots = new FilteredElementCollector(document).OfClass(typeof(SpotDimension)).Cast<SpotDimension>().Where(sd => { try { return sd.References.Size > 0 && sd.References.get_Item(0).ElementId == tCalc.Id; } catch { return false; } })
                .Select(sd => Tuple.Create(sd.OwnerViewId, sd.Origin, sd.GetTypeId())).ToList();
            log.AppendLine("calçada refeita até a guia, com recorte das rampas [" + nf.Id + "] (antiga " + tCalc.Id + " apagada)"); document.Delete(tCalc.Id);
            document.Regenerate(); var rTopo = HostObjectUtils.GetTopFaces(nf).First(); var fTopo = nf.GetGeometryObjectFromReference(rTopo) as Face; int nSp = 0;
            foreach (var sp in spots) { var pr = fTopo.Project(new XYZ(sp.Item2.X, sp.Item2.Y, sp.Item2.Z + F(1))); if (pr == null) { log.AppendLine("⚠ cota de nível da calçada caiu na rampa — recoloque à mão"); continue; }
                try { var nsp = document.Create.NewSpotElevation(document.GetElement(sp.Item1) as View, rTopo, pr.XYZPoint, pr.XYZPoint, pr.XYZPoint, pr.XYZPoint, false); nsp.ChangeTypeId(sp.Item3); nSp++; } catch { } }
            if (spots.Count > 0) log.AppendLine("cotas de nível da calçada recriadas: " + nSp + "/" + spots.Count);
        } else log.AppendLine("⚠ piso da calçada não encontrado — desenhe a laje da calçada até a guia");
        if (tAsf != null) {
            var pts = new[] { W(0, Cm + sj), W(Lm, Cm + sj), W(Lm, wAsf1), W(0, wAsf1) };
            var lp = new CurveLoop(); for (int i = 0; i < 4; i++) lp.Append(Line.CreateBound(pts[i], pts[(i + 1) % 4])); if (lp.IsCounterclockwise(XYZ.BasisZ) == false) lp.Flip();
            var nf = Floor.Create(document, new List<CurveLoop> { lp }, tAsf.GetTypeId(), nivCalc.Id); nf.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(F(-esp));
            log.AppendLine("asfalto refeito depois da sarjeta, " + (esp * 100).ToString("0") + " cm abaixo da calçada [" + nf.Id + "] (antigo " + tAsf.Id + " apagado)"); document.Delete(tAsf.Id);
        }
        t.Commit();
    }
} finally { document.Application.FailuresProcessing -= __avisos; }
log.AppendLine("família salva em " + arq);
log.AppendLine("afaste da rampa o símbolo de acesso e o nome da CALÇADA; confira em planta e num corte que passe pelo rebaixo.");
return log.ToString();
