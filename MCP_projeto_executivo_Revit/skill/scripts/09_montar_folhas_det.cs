// 09_montar_folhas_det.cs — Fase 9: monta as folhas A3 do caderno DET a partir das montagens.
// Parâmetros:
//   [0] grupos: "esquadrias;bancadas;genericos" (vazio = todos)
//   [1] prefixo do número da folha (teste: "SKILL DET "; produção: "")
//   [2] filtro de nomes de montagem separados por ";" (vazio = todas cujas vistas ainda não estão em folha)
//   [3] nome do tipo de carimbo A3 (vazio = primeiro tipo cujo nome contém "A3" e "Detalhamento")
// Layout (medido no projeto de referência, carimbo na coluna da direita):
//   - Esquadrias: uma coluna por montagem (elevação em cima, topo embaixo), em linhas da esquerda
//     para a direita; tabela de portas/janelas centralizada no rodapé da primeira folha de cada tipo.
//     Portas e janelas em folhas separadas.
//   - Bancadas e genéricos: bloco com título (nome + ambiente), 3D à esquerda e elevação/topo à direita.
//   - Se um bloco não cabe na largura útil, a escala das vistas dele dobra (1:25 -> 1:50).
// A vista de topo é girada 90° na folha quando a peça corre na vertical dela, para alinhar com a elevação.
// Vistas de montagem nascem sem recorte; o script liga o recorte ajustado à peça (+0,15 m) e o esconde.
// Folhas DET vazias (sem vistas nem tabelas) com o mesmo carimbo são removidas antes — vêm do template.
// Numeração: <prefixo>i/N (dois dígitos) contando as folhas DET existentes com o mesmo prefixo.
// Uma transação "Skill: folhas DET". Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var grupos = (P(0) == "" ? "esquadrias;bancadas;genericos" : P(0).ToLower()).Split(';').Select(s => s.Trim()).ToList();
var prefixo = parameters.Length > 1 ? ((parameters[1] as string) ?? "") : "";
var filtro = P(2) == "" ? new List<string>() : P(2).ToUpper().Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
Func<double, double> M = mm => mm / 304.8;
var log = new System.Text.StringBuilder();
var tbTipos = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType().Cast<FamilySymbol>().ToList();
var tb = P(3) != "" ? tbTipos.FirstOrDefault(x => x.Name == P(3) || x.FamilyName == P(3)) : tbTipos.FirstOrDefault(x => (x.FamilyName + x.Name).Contains("A3") && (x.FamilyName + x.Name).ToUpper().Contains("DETALHAMENTO"));
if (tb == null) return "ERRO: carimbo A3 de detalhamento não encontrado. Informe o nome no parâmetro [3].";
var naFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(v => v.ViewId));
var todasVistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate && v.AssociatedAssemblyInstanceId != ElementId.InvalidElementId).ToList();
var unidades = new List<Tuple<string, AssemblyInstance, List<View>>>();
foreach (var a in new FilteredElementCollector(document).OfClass(typeof(AssemblyInstance)).Cast<AssemblyInstance>().OrderBy(x => x.AssemblyTypeName)) {
    var vs = todasVistas.Where(v => v.AssociatedAssemblyInstanceId == a.Id).ToList();
    if (vs.Count == 0 || vs.Any(v => naFolha.Contains(v.Id))) continue;
    if (filtro.Count > 0 && !filtro.Contains(a.AssemblyTypeName.ToUpper())) continue;
    var cat = a.NamingCategoryId.Value;
    string g = cat == (long)BuiltInCategory.OST_Doors ? "portas" : cat == (long)BuiltInCategory.OST_Windows ? "janelas" : cat == (long)BuiltInCategory.OST_Casework ? "bancadas" : "genericos";
    if ((g == "portas" || g == "janelas") && !grupos.Contains("esquadrias")) continue;
    if ((g == "bancadas" || g == "genericos") && !grupos.Contains(g)) continue;
    unidades.Add(Tuple.Create(g, a, vs));
}
if (unidades.Count == 0) return "Nada a montar: todas as montagens já estão em folha (ou não há montagens — rode 09_montagens.cs).";
var vpTipo = new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(v => v.GetTypeId()).Distinct().Select(i => document.GetElement(i)).Where(e => e != null && (e.Name.ToUpper().Contains("SEM T") || e.Name.ToUpper().Contains("NO TITLE"))).Select(e => e.Id).FirstOrDefault() ?? ElementId.InvalidElementId;
Action<Viewport> SemTitulo = vp => { if (vpTipo != ElementId.InvalidElementId) try { vp.ChangeTypeId(vpTipo); } catch { } };
var modelo = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().FirstOrDefault(s => new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).Cast<FamilyInstance>().Any(x => x.Symbol.Id == tb.Id));
var tipoTexto = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).FirstElementId();
var valoresModelo = new Dictionary<string, string>();   // campos do carimbo copiados de uma folha DET existente (lidos antes de remover as vazias)
if (modelo != null) foreach (Parameter p in modelo.Parameters)
    if (!p.IsReadOnly && p.StorageType == StorageType.String && p.Id.Value != (long)BuiltInParameter.SHEET_NUMBER && p.Id.Value != (long)BuiltInParameter.SHEET_NAME && !string.IsNullOrEmpty(p.AsString()) && !valoresModelo.ContainsKey(p.Definition.Name)) valoresModelo[p.Definition.Name] = p.AsString();
var rot = new Dictionary<ElementId, ViewportRotation>();
var folhasNovas = new List<ViewSheet>(); var cab = new System.Text.StringBuilder();
double x0 = 0, x1 = 0, y0 = 0, y1 = 0;
using (var t = new Transaction(document, "Skill: folhas DET")) {
    t.Start();
    // folhas DET vazias que vieram do template (mesmo carimbo, sem vistas nem tabelas) são removidas — a skill numera as suas
    var vazias = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => s.SheetNumber.StartsWith(prefixo)
        && new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).Cast<FamilyInstance>().Any(x => x.Symbol.Id == tb.Id)
        && new FilteredElementCollector(document, s.Id).OfClass(typeof(Viewport)).GetElementCount() == 0
        && !new FilteredElementCollector(document, s.Id).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>().Any(x => !x.IsTitleblockRevisionSchedule)).ToList();
    if (vazias.Count > 0) { log.AppendLine("Folhas DET vazias do template removidas: " + string.Join(", ", vazias.Select(s => s.SheetNumber))); document.Delete(vazias.Select(v => v.Id).ToList()); }
    Func<string, ViewSheet> NovaFolha = nome => {
        var s = ViewSheet.Create(document, tb.Id); s.Name = nome; folhasNovas.Add(s);
        var pcont = s.LookupParameter("CONTEÚDO DA PRANCHA"); if (pcont != null && !pcont.IsReadOnly) pcont.Set(nome);
        foreach (var kv in valoresModelo) { var d = s.LookupParameter(kv.Key); if (d != null && !d.IsReadOnly && d.StorageType == StorageType.String) try { d.Set(kv.Value); } catch { } }
        if (valoresModelo.Count == 0) { var pv = s.LookupParameter("VISTAS"); if (pv != null && !pv.IsReadOnly) pv.Set("DETALHAMENTO"); }
        document.Regenerate();
        var ti = new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement();
        var bb = ti.get_BoundingBox(s); x0 = bb.Min.X + M(18); x1 = bb.Max.X - M(57); y0 = bb.Min.Y + M(13); y1 = bb.Max.Y - M(9);
        return s;
    };
    Action<View, AssemblyInstance> AjustarRecorte = (v, a) => {
        if (v.CropBoxActive) return;
        var cb = v.CropBox; var inv = cb.Transform.Inverse; double mnx = double.MaxValue, mny = double.MaxValue, mxx = double.MinValue, mxy = double.MinValue;
        foreach (var id in a.GetMemberIds()) {
            var bb = document.GetElement(id).get_BoundingBox(null); if (bb == null) continue;
            foreach (var x in new[] { bb.Min.X, bb.Max.X }) foreach (var y in new[] { bb.Min.Y, bb.Max.Y }) foreach (var z in new[] { bb.Min.Z, bb.Max.Z }) {
                var q = inv.OfPoint(new XYZ(x, y, z)); mnx = Math.Min(mnx, q.X); mny = Math.Min(mny, q.Y); mxx = Math.Max(mxx, q.X); mxy = Math.Max(mxy, q.Y); }
        }
        if (mnx > mxx) return; var mg = 0.15 / 0.3048;
        cb.Min = new XYZ(mnx - mg, mny - mg, cb.Min.Z); cb.Max = new XYZ(mxx + mg, mxy + mg, cb.Max.Z);
        try { v.CropBox = cb; v.CropBoxActive = true; v.CropBoxVisible = false; } catch { }
    };
    Func<ViewSheet, View, double, double, Viewport> Colocar = (s, v, xmin, ytopo) => {
        var vp = Viewport.Create(document, s.Id, v.Id, new XYZ((x0 + x1) / 2, (y0 + y1) / 2, 0));
        SemTitulo(vp); if (rot.ContainsKey(v.Id)) vp.Rotation = rot[v.Id];
        document.Regenerate(); var o = vp.GetBoxOutline();
        vp.SetBoxCenter(vp.GetBoxCenter() + new XYZ(xmin - o.MinimumPoint.X, ytopo - o.MaximumPoint.Y, 0)); return vp;
    };
    Func<View, XYZ> Tam = v => { var vp = Viewport.Create(document, folhasNovas.Last().Id, v.Id, XYZ.Zero); SemTitulo(vp); if (rot.ContainsKey(v.Id)) vp.Rotation = rot[v.Id]; document.Regenerate(); var o = vp.GetBoxOutline(); var r = o.MaximumPoint - o.MinimumPoint; document.Delete(vp.Id); return r; };
    var sequencias = new[] { Tuple.Create("portas", "DETALHAMENTO DE ESQUADRIAS", "PORTAS"), Tuple.Create("janelas", "DETALHAMENTO DE ESQUADRIAS", "JANELAS"), Tuple.Create("bancadas", "DETALHAMENTO DE BANCADAS", ""), Tuple.Create("genericos", "", "") };
    foreach (var sq in sequencias) {
        var lista = unidades.Where(u => u.Item1 == sq.Item1).ToList(); if (lista.Count == 0) continue;
        var nomeFolha = sq.Item2 != "" ? sq.Item2 : (string.Join(" E ", lista.Select(u => u.Item2.AssemblyTypeName.ToUpper()).Distinct()).Length <= 30 ? "DETALHAMENTO " + string.Join(" E ", lista.Select(u => u.Item2.AssemblyTypeName.ToUpper()).Distinct()) : "DETALHAMENTO DE ELEMENTOS");
        var folha = NovaFolha(nomeFolha); double piso = y0, cx = x0, topoLinha = y1, altLinha = 0; const double gap = 8;
        if (sq.Item3 != "") {
            var tab = new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().FirstOrDefault(v => !v.IsTemplate && !v.IsTitleblockRevisionSchedule && v.Name.ToUpper().Contains("TABELA DE " + sq.Item3));
            if (tab != null) {
                var si = ScheduleSheetInstance.Create(document, folha.Id, tab.Id, new XYZ(x0, y0 + M(100), 0)); document.Regenerate();
                var b = si.get_BoundingBox(folha); var w = b.Max.X - b.Min.X;
                si.Point = si.Point + new XYZ((x0 + x1) / 2 - w / 2 - b.Min.X, y0 - b.Min.Y, 0); piso = y0 + (b.Max.Y - b.Min.Y) + M(gap);
                log.AppendLine("  tabela \"" + tab.Name + "\" no rodapé");
            } else log.AppendLine("  AVISO: tabela de " + sq.Item3.ToLower() + " não encontrada");
        }
        foreach (var u in lista) {
            var topo = u.Item3.FirstOrDefault(v => v.ViewType == ViewType.Detail && v.ViewDirection.Z > 0.9);
            var elev = u.Item3.FirstOrDefault(v => v.ViewType == ViewType.Detail && Math.Abs(v.ViewDirection.Z) < 0.1);
            var v3 = u.Item3.FirstOrDefault(v => v is View3D);
            var esq = u.Item1 == "portas" || u.Item1 == "janelas";
            foreach (var v in u.Item3) AjustarRecorte(v, u.Item2); document.Regenerate();
            if (topo != null && elev != null) { var du = elev.RightDirection.DotProduct(topo.UpDirection); if (du < -0.7) rot[topo.Id] = ViewportRotation.Counterclockwise; else if (du > 0.7) rot[topo.Id] = ViewportRotation.Clockwise; }
            Func<double[]> Medir = () => {
                var te = elev != null ? Tam(elev) : XYZ.Zero; var tt = topo != null ? Tam(topo) : XYZ.Zero; var t3 = v3 != null ? Tam(v3) : XYZ.Zero;
                double colW = Math.Max(te.X, tt.X), colH = te.Y + tt.Y + (elev != null && topo != null ? M(3) : 0);
                return esq ? new[] { colW, colH, 0, te.X, tt.X } : new[] { t3.X + (v3 != null ? M(6) : 0) + colW, Math.Max(t3.Y, colH) + M(7), t3.X, te.X, tt.X };
            };
            var med = Medir();
            if (med[0] > x1 - x0) { foreach (var v in u.Item3) v.Scale = v.Scale * 2; med = Medir(); log.AppendLine("  " + u.Item2.AssemblyTypeName + ": escala dobrada para caber"); }
            if (cx + med[0] > x1 + M(0.5)) { cx = x0; topoLinha -= altLinha + M(gap); altLinha = 0; }
            if (topoLinha - med[1] < piso) { folha = NovaFolha(nomeFolha); cx = x0; topoLinha = y1; altLinha = 0; piso = y0; }
            double colX = esq ? cx : cx + med[2] + (v3 != null ? M(6) : 0), colW = Math.Max(med[3], med[4]), yCol = esq ? topoLinha : topoLinha - M(7);
            if (!esq) {
                var titulo = (u.Item1 == "bancadas" ? "BANCADA " : "") + u.Item2.AssemblyTypeName;
                var principalE = u.Item2.GetMemberIds().Select(i => document.GetElement(i)).FirstOrDefault(e => e.Category != null && e.Category.Id.Value != (long)BuiltInCategory.OST_PlumbingFixtures) ?? document.GetElement(u.Item2.GetMemberIds().First()); var bbE = principalE.get_BoundingBox(null); if (bbE != null) { var pE = (bbE.Min + bbE.Max) / 2; var room = document.GetRoomAtPoint(pE) ?? document.GetRoomAtPoint(new XYZ(pE.X, pE.Y, bbE.Min.Z + 0.5)); if (room != null) titulo += " - " + room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString(); }
                TextNote.Create(document, folha.Id, new XYZ(cx, topoLinha, 0), titulo, tipoTexto);
                if (v3 != null) Colocar(folha, v3, cx, yCol);
            }
            if (elev != null) { var vp = Colocar(folha, elev, colX + (colW - med[3]) / 2, yCol); yCol = vp.GetBoxOutline().MinimumPoint.Y - M(3); }
            if (topo != null) Colocar(folha, topo, colX + (colW - med[4]) / 2, yCol);
            cx += med[0] + M(gap); altLinha = Math.Max(altLinha, med[1]);
        }
    }
    var existentes = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => !folhasNovas.Any(n => n.Id == s.Id) && s.SheetNumber.StartsWith(prefixo) && System.Text.RegularExpressions.Regex.IsMatch(s.SheetNumber.Substring(prefixo.Length), @"^\d+/\d+$") && new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).Cast<FamilyInstance>().Any(x => x.Symbol.Id == tb.Id)).ToList();
    int total = existentes.Count + folhasNovas.Count, i = existentes.Count;
    foreach (var s in folhasNovas) {
        var n = prefixo + (++i).ToString("00") + "/" + total.ToString("00"); var vazia = new FilteredElementCollector(document, s.Id).OfClass(typeof(Viewport)).GetElementCount() == 0 && new FilteredElementCollector(document, s.Id).OfClass(typeof(ScheduleSheetInstance)).GetElementCount() == 0;
        s.SheetNumber = n; cab.AppendLine(n + " | " + s.Name + (vazia ? " (VAZIA)" : ""));
    }
    if (existentes.Count > 0) log.AppendLine("ATENÇÃO: " + existentes.Count + " folhas DET já existiam; atualize o total \"/N\" delas para /" + total.ToString("00") + " (fase 10 confere).");
    t.Commit();
}
return folhasNovas.Count + " folhas criadas\n" + cab.ToString() + log.ToString();
