// 09_montagens.cs — Fase 9 (Caderno DET): cria montagens (assemblies) e suas vistas de detalhe.
// Parâmetros:
//   [0] modo: "propor" (só lista) | "criar" | "refazer_elevacoes" (refaz a elevação das montagens do filtro [2]
//       que ainda não estão em folha — use quando a elevação saiu de lado)
//   [1] grupos: "portas;janelas;bancadas;genericos" (vazio = todos)
//   [2] filtro de marcas/nomes separados por ";" (vazio = tudo que ainda não tem montagem)
//   [3] palavras-chave dos modelos genéricos a detalhar (vazio = "PERGOL;CHURRASQ")
// Regras (elevação = a vista em que a geometria da peça é mais larga, medida nos vértices dos sólidos;
// entre frente e fundo, a do lado da FacingOrientation. Não usa só a FacingOrientation porque há famílias
// com eixos girados. NUNCA desfaça montagens por script: o Revit abre um diálogo modal e trava o MCP):
//   - Portas/janelas: uma montagem por marca de tipo (primeira instância), nome = marca.
//     Vistas 1:50: Elevação de topo + a elevação que olha a face da folha.
//   - Bancadas (Componentes de gabinete): uma por marca de tipo; sem marca = uma por instância,
//     nome B01, B02... livre. Junta as peças hidrossanitárias apoiadas na bancada.
//     Vistas 1:25: topo + elevação frontal + 3D ortogonal.
//   - Genéricos: modelos genéricos cujo nome de família/tipo contém as palavras-chave.
//     Vistas 1:50: topo + frontal + 3D.
//   - Elementos que já estão em montagem são pulados (idempotente).
// Uma transação por montagem dentro de um TransactionGroup "Skill: montagens DET". Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var criar = P(0).ToLower() == "criar";
var grupos = (P(1) == "" ? "portas;janelas;bancadas;genericos" : P(1).ToLower()).Split(';').Select(s => s.Trim()).ToList();
var filtro = P(2) == "" ? new List<string>() : P(2).ToUpper().Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var chaves = (P(3) == "" ? "PERGOL;CHURRASQ" : P(3).ToUpper()).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var montagens = new FilteredElementCollector(document).OfClass(typeof(AssemblyInstance)).Cast<AssemblyInstance>().ToList();
var emMont = new HashSet<ElementId>(montagens.SelectMany(a => a.GetMemberIds()));
var nomesUsados = new HashSet<string>(montagens.Select(a => a.AssemblyTypeName.ToUpper()));
Func<Element, string> Marca = e => { var tp = document.GetElement(e.GetTypeId()); var p = tp == null ? null : tp.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK); return ((p != null ? p.AsString() : "") ?? "").Trim(); };
// plano: nome, membros, categoria de nomeação, escala, com 3D, elemento principal
Func<Element, XYZ, double> Ext = (e, d) => {
    double mn = double.MaxValue, mx = double.MinValue; Action<GeometryElement> Walk = null;
    Walk = ge => { foreach (var go in ge) { if (go is Solid so && so.Volume > 0) { foreach (Edge ed in so.Edges) foreach (var pt in ed.Tessellate()) { var v = pt.DotProduct(d); if (v < mn) mn = v; if (v > mx) mx = v; } } else if (go is GeometryInstance gi) Walk(gi.GetInstanceGeometry()); } };
    var g = e.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine }); if (g != null) Walk(g); return mx > mn ? mx - mn : 0;
};
Func<AssemblyInstance, FamilyInstance, int, bool, string> CriarElevacao = (asm, fi, escala, comTag) => {
    var f = fi.FacingOrientation; var face = new XYZ(f.X, f.Y, 0); face = face.GetLength() > 0 ? face.Normalize() : XYZ.BasisY.Negate();
    var cands = new List<ViewSection>();
    foreach (var o in new[] { AssemblyDetailViewOrientation.ElevationFront, AssemblyDetailViewOrientation.ElevationBack, AssemblyDetailViewOrientation.ElevationLeft, AssemblyDetailViewOrientation.ElevationRight })
        cands.Add(AssemblyViewUtils.CreateDetailSection(document, asm.Id, o));
    document.Regenerate();
    var larg = cands.ToDictionary(v => v.Id, v => Ext(fi, v.RightDirection)); var maxL = larg.Values.Max();
    var elev = cands.Where(v => larg[v.Id] >= maxL * 0.95).OrderByDescending(v => v.ViewDirection.DotProduct(face)).First();
    foreach (var v in cands) if (v.Id != elev.Id) document.Delete(v.Id);
    elev.Scale = escala; var r = "elevação " + Math.Round(maxL * 304.8) + " mm de largura";
    if (comTag) try {
        var bb = fi.get_BoundingBox(null); var c = (bb.Min + bb.Max) / 2;
        var tc = fi.Category.Id.Value == (long)BuiltInCategory.OST_Doors ? BuiltInCategory.OST_DoorTags : BuiltInCategory.OST_WindowTags;
        var tipoTag = new FilteredElementCollector(document).OfCategory(tc).WhereElementIsElementType().FirstElementId();
        IndependentTag.Create(document, tipoTag, elev.Id, new Reference(fi), false, TagOrientation.Horizontal, new XYZ(c.X, c.Y, bb.Min.Z - F(0.35))); r += " + identificador";
    } catch { }
    return r;
};
if (P(0).ToLower() == "refazer_elevacoes") {
    if (filtro.Count == 0) return "Informe no parâmetro [2] as montagens a refazer (ex.: P100;J07).";
    var naFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(v => v.ViewId));
    using (var t = new Transaction(document, "Skill: refazer elevações DET")) {
        t.Start();
        foreach (var a in montagens.Where(x => filtro.Contains(x.AssemblyTypeName.ToUpper()))) {
            var velhas = new FilteredElementCollector(document).OfClass(typeof(ViewSection)).Cast<ViewSection>().Where(v => v.AssociatedAssemblyInstanceId == a.Id && Math.Abs(v.ViewDirection.Z) < 0.1).ToList();
            if (velhas.Any(v => naFolha.Contains(v.Id))) { log.AppendLine(a.AssemblyTypeName + ": elevação já está em folha — retire da folha antes."); continue; }
            var principal = a.GetMemberIds().Select(i => document.GetElement(i)).OfType<FamilyInstance>().OrderByDescending(x => x.Category.Id.Value == (long)BuiltInCategory.OST_PlumbingFixtures ? 0 : 1).First();
            var escala = velhas.Count > 0 ? velhas[0].Scale : 50;
            foreach (var v in velhas) document.Delete(v.Id);
            var cat = principal.Category.Id.Value;
            log.AppendLine(a.AssemblyTypeName + ": " + CriarElevacao(a, principal, escala, cat == (long)BuiltInCategory.OST_Doors || cat == (long)BuiltInCategory.OST_Windows));
        }
        t.Commit();
    }
    return log.ToString();
}
var plano = new List<Tuple<string, List<ElementId>, ElementId, int, bool, FamilyInstance>>();
foreach (var g in new[] { Tuple.Create("portas", BuiltInCategory.OST_Doors), Tuple.Create("janelas", BuiltInCategory.OST_Windows) }) {
    if (!grupos.Contains(g.Item1)) continue;
    var inst = new FilteredElementCollector(document).OfCategory(g.Item2).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
    foreach (var grp in inst.GroupBy(x => Marca(x)).OrderBy(x => x.Key)) {
        if (grp.Key == "") { log.AppendLine("AVISO: " + grp.Count() + " " + g.Item1 + " sem marca de tipo — preencha a marca (fase 3)."); continue; }
        if (grp.Any(i => emMont.Contains(i.Id)) || nomesUsados.Contains(grp.Key.ToUpper())) continue;
        if (filtro.Count > 0 && !filtro.Contains(grp.Key.ToUpper())) continue;
        var fi = grp.First();
        plano.Add(Tuple.Create(grp.Key, new List<ElementId> { fi.Id }, new ElementId(g.Item2), 50, false, fi));
    }
}
if (grupos.Contains("bancadas")) {
    var gab = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Casework).WhereElementIsNotElementType().Cast<FamilyInstance>().Where(i => !emMont.Contains(i.Id)).ToList();
    var hidro = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_PlumbingFixtures).WhereElementIsNotElementType().Where(i => !emMont.Contains(i.Id)).ToList();
    var usadas = new HashSet<ElementId>();
    int prox = 1; Func<string> NovoB = () => { string n; do { n = "B" + (prox++).ToString("00"); } while (nomesUsados.Contains(n)); return n; };
    var porMarca = gab.GroupBy(x => Marca(x)).ToList();
    var unidades = new List<Tuple<string, FamilyInstance>>();
    foreach (var grp in porMarca) {
        if (grp.Key != "") { if (!nomesUsados.Contains(grp.Key.ToUpper()) && !gab.Where(x => Marca(x) == grp.Key).Any(x => emMont.Contains(x.Id))) unidades.Add(Tuple.Create(grp.Key, grp.First())); }
        else foreach (var fi in grp) unidades.Add(Tuple.Create("", fi));
    }
    foreach (var u in unidades) {
        var nome = u.Item1 != "" ? u.Item1 : NovoB(); nomesUsados.Add(nome.ToUpper());
        if (filtro.Count > 0 && !filtro.Contains(nome.ToUpper())) continue;
        var bb = u.Item2.get_BoundingBox(null); if (bb == null) continue;
        var membros = new List<ElementId> { u.Item2.Id };
        foreach (var h in hidro) {
            if (usadas.Contains(h.Id)) continue; var hb = h.get_BoundingBox(null); if (hb == null) continue; var c = (hb.Min + hb.Max) / 2;
            if (c.X > bb.Min.X - F(0.1) && c.X < bb.Max.X + F(0.1) && c.Y > bb.Min.Y - F(0.1) && c.Y < bb.Max.Y + F(0.1) && c.Z > bb.Min.Z - F(0.3) && c.Z < bb.Max.Z + F(0.6)) { membros.Add(h.Id); usadas.Add(h.Id); }
        }
        plano.Add(Tuple.Create(nome, membros, new ElementId(BuiltInCategory.OST_Casework), 25, true, u.Item2));
    }
}
if (grupos.Contains("genericos")) {
    var gen = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsNotElementType().OfType<FamilyInstance>().Where(i => !emMont.Contains(i.Id)).ToList();
    foreach (var grp in gen.Where(i => chaves.Any(k => (i.Symbol.FamilyName + " " + i.Symbol.Name).ToUpper().Contains(k))).GroupBy(i => i.Symbol.Id)) {
        var fi = grp.First(); var nome = fi.Symbol.Name;
        if (nomesUsados.Contains(nome.ToUpper())) continue; if (filtro.Count > 0 && !filtro.Contains(nome.ToUpper())) continue;
        nomesUsados.Add(nome.ToUpper());
        plano.Add(Tuple.Create(nome, new List<ElementId> { fi.Id }, new ElementId(BuiltInCategory.OST_GenericModel), 50, true, fi));
    }
}
if (!criar) {
    log.Insert(0, "PROPOSTA — " + plano.Count + " montagens a criar:\n");
    foreach (var p in plano) log.AppendLine("  " + p.Item1 + " | " + string.Join(", ", p.Item2.Select(i => document.GetElement(i).Category.Name).GroupBy(x => x).Select(x => x.Key + (x.Count() > 1 ? " x" + x.Count() : ""))) + " | 1:" + p.Item4 + (p.Item5 ? " + 3D" : ""));
    return log.ToString();
}
int ok = 0;
using (var tg = new TransactionGroup(document, "Skill: montagens DET")) {
    tg.Start();
    foreach (var p in plano) {
        try {
            AssemblyInstance asm;
            using (var t1 = new Transaction(document, "montagem " + p.Item1)) { t1.Start(); asm = AssemblyInstance.Create(document, p.Item2, p.Item3); t1.Commit(); }
            using (var t2 = new Transaction(document, "vistas " + p.Item1)) {
                t2.Start();
                asm.AssemblyTypeName = p.Item1;
                var topo = AssemblyViewUtils.CreateDetailSection(document, asm.Id, AssemblyDetailViewOrientation.ElevationTop); topo.Scale = p.Item4;
                var esqd = p.Item3.Value == (long)BuiltInCategory.OST_Doors || p.Item3.Value == (long)BuiltInCategory.OST_Windows;
                string extra = " + " + CriarElevacao(asm, p.Item6, p.Item4, esqd);
                if (p.Item5) { var v3 = AssemblyViewUtils.Create3DOrthographic(document, asm.Id); v3.Scale = p.Item4; extra += " + 3D"; }
                t2.Commit();
                log.AppendLine(p.Item1 + ": topo" + extra + " (1:" + p.Item4 + ")"); ok++;
            }
        } catch (Exception ex) { log.AppendLine(p.Item1 + ": ERRO " + ex.Message); }
    }
    tg.Assimilate();
}
return ok + " montagens criadas\n" + log.ToString();
