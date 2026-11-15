// SKILL EXECUTIVO REVIT — Fase 5.5: montagem automática das folhas do caderno ARQ
// send_code_to_revit · transactionMode "none"
// parameters[0] = folha do gabarito: "3/7" (cortes) ou "4/7" (fachadas)
// parameters[1] = número da folha a criar/usar (ex.: "3/7"; em teste "SKILL 3/7")
// parameters[2] = prefixo dos nomes das vistas ("" no uso real; "SKILL " em teste)
// parameters[3] = trecho do nome do carimbo (padrão "Executivo Arquitetônico")
// parameters[4] = tipo de título de viewport (padrão "Title w Line")
//
// Posições = centro da vista em mm a partir da origem do carimbo (references/gabarito-folhas.md).
// Se a folha já existe e está vazia, usa; se já tem vistas, só acrescenta as que faltam.
// Vista que já está em outra folha é pulada (o Revit não deixa a mesma vista em duas folhas).
// Ao final confere se alguma vista passou da área útil (moldura x 25–832, y 10–584 mm, menos o carimbo no canto x > 656, y < 297)
// ou se sobrepõe a outra em mais de 15 mm.
var gab = parameters[0] as string;
var numero = parameters[1] as string;
var prefixo = parameters.Length > 2 ? (parameters[2] as string ?? "") : "";
var trechoCarimbo = parameters.Length > 3 && !string.IsNullOrWhiteSpace(parameters[3] as string) ? (string)parameters[3] : "Executivo Arquitetônico";
var nomeTitulo = parameters.Length > 4 && !string.IsNullOrWhiteSpace(parameters[4] as string) ? (string)parameters[4] : "Title w Line";
Func<double, double> MM = v => v / 304.8; Func<double, double> ToMM = v => v * 304.8;
var log = new System.Text.StringBuilder();

// gabarito: (nome da vista sem prefixo, x, y, título na folha). "{LONG}"/"{TRANSV}" = perspectiva de corte longitudinal/transversal
var slots = new List<Tuple<string, double, double, string>>();
string conteudo, nomeFolha;
if (gab == "3/7") {
    conteudo = "CORTES"; nomeFolha = "PRANCHA 03";
    slots.Add(Tuple.Create("C", 369.0, 522.0, "CORTE C")); slots.Add(Tuple.Create("D", 370.0, 408.0, "CORTE D"));
    slots.Add(Tuple.Create("A", 191.0, 277.0, "CORTE A")); slots.Add(Tuple.Create("B", 510.0, 263.0, "CORTE B"));
    slots.Add(Tuple.Create("{LONG}", 224.0, 120.0, "")); slots.Add(Tuple.Create("{TRANSV}", 536.0, 91.0, ""));
} else if (gab == "4/7") {
    conteudo = "FACHADAS"; nomeFolha = "PRANCHA 04";
    slots.Add(Tuple.Create("FACHADA LATERAL DIREITA", 443.0, 531.0, "")); slots.Add(Tuple.Create("FACHADA LATERAL ESQUERDA", 367.0, 391.0, ""));
    slots.Add(Tuple.Create("FACHADA FRONTAL", 206.0, 245.0, "")); slots.Add(Tuple.Create("FACHADA POSTERIOR", 205.0, 112.0, ""));
    slots.Add(Tuple.Create("PERSPECTIVA LATERAL DIREITA", 517.0, 255.0, "")); slots.Add(Tuple.Create("PERSPECTIVA LATERAL ESQUERDA", 518.0, 102.0, ""));
} else return "Folha de gabarito não suportada ainda: " + gab + " (disponíveis: 3/7, 4/7)";

var vistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
Func<string, View> Achar = n => {
    if (n == "{LONG}" || n == "{TRANSV}") {
        var longs = new[] { "C", "D" };
        return vistas.OfType<View3D>().Where(v => v.Name.StartsWith(prefixo + "PERSPECTIVA CORTE ")).FirstOrDefault(v => {
            var L = v.Name.Substring((prefixo + "PERSPECTIVA CORTE ").Length).Trim(); return (n == "{LONG}") == longs.Contains(L); });
    }
    return vistas.FirstOrDefault(v => v.Name == prefixo + n) ?? vistas.FirstOrDefault(v => v.Name == prefixo + "CORTE " + n);
};
var carimbo = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
    .FirstOrDefault(f => f.FamilyName.Contains(trechoCarimbo));
if (carimbo == null) return "ERRO: carimbo com '" + trechoCarimbo + "' não encontrado";
var tipoTitulo = new FilteredElementCollector(document).OfClass(typeof(ElementType)).Cast<ElementType>()
    .FirstOrDefault(t => t.Name == nomeTitulo && t.get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL) != null);   // tipo de viewport (nome da família muda com o idioma)
var noutraFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(vp => vp.ViewId));

using (var t = new Transaction(document, "Skill: montar folha " + numero)) {
    t.Start();
    var folha = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().FirstOrDefault(s => s.SheetNumber == numero);
    if (folha == null) {
        folha = ViewSheet.Create(document, carimbo.Id); folha.SheetNumber = numero; folha.Name = nomeFolha;
        log.AppendLine("Folha criada: " + numero + " — " + nomeFolha + " (carimbo " + carimbo.FamilyName + ")");
    } else log.AppendLine("Folha existente usada: " + numero);
    var pc = folha.LookupParameter("CONTEÚDO DA PRANCHA"); if (pc != null && !pc.IsReadOnly && string.IsNullOrEmpty(pc.AsString())) pc.Set(conteudo);
    var tb = new FilteredElementCollector(document, folha.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
    var o = tb != null ? tb.GetTransform().Origin : XYZ.Zero;
    var colocados = new List<Viewport>();
    foreach (var s in slots) {
        var v = Achar(s.Item1);
        if (v == null) { log.AppendLine("  FALTA a vista: " + s.Item1); continue; }
        if (noutraFolha.Contains(v.Id)) { log.AppendLine("  já está numa folha, pulada: " + v.Name); continue; }
        var centro = new XYZ(o.X + MM(s.Item2), o.Y + MM(s.Item3), 0);
        var vp = Viewport.Create(document, folha.Id, v.Id, centro);
        if (tipoTitulo != null) vp.ChangeTypeId(tipoTitulo.Id);
        vp.SetBoxCenter(centro);
        if (s.Item4 != "") { var pd = v.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION); if (pd != null && !pd.IsReadOnly) pd.Set(s.Item4); }
        colocados.Add(vp);
    }
    document.Regenerate();
    // 3/7: empilha de cima para baixo pela altura real — C, D, (A | B), perspectivas — com 20 mm para o título de cada vista.
    // As perspectivas ficam com o espaço que sobra; se não couberem, a escala delas sobe (1:125, 1:150, 1:200).
    if (gab == "3/7") {
        Func<Viewport, double> H = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.Y - b.MinimumPoint.Y); };
        Func<Viewport, double> W = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.X - b.MinimumPoint.X); };
        Func<string, Viewport> VP = n => colocados.FirstOrDefault(vp => ((View)document.GetElement(vp.ViewId)).Name == prefixo + n || ((View)document.GetElement(vp.ViewId)).Name == prefixo + "CORTE " + n);
        Action<Viewport, double> Y = (vp, yc) => { var c = vp.GetBoxCenter(); vp.SetBoxCenter(new XYZ(c.X, o.Y + MM(yc), 0)); };
        double topo = 584, titulo = 20;
        foreach (var linhaCortes in new[] { new[] { "C" }, new[] { "D" }, new[] { "A", "B" } }) {
            var vps = linhaCortes.Select(VP).Where(x => x != null).ToList(); if (vps.Count == 0) continue;
            double h = vps.Max(H); foreach (var vp in vps) Y(vp, topo - h / 2); topo -= h + titulo;
        }
        var linha = colocados.Where(vp => document.GetElement(vp.ViewId) is View3D).ToList();
        if (linha.Count > 0) {
            double alturaLivre = topo - 10 - titulo;
            foreach (var esc in new[] { 0, 125, 150, 200 }) {
                if (esc > 0) { foreach (var vp in linha) ((View)document.GetElement(vp.ViewId)).Scale = esc; document.Regenerate(); }
                if (linha.Max(H) <= alturaLivre && linha.Sum(W) + 10 <= 620) { if (esc > 0) log.AppendLine("  perspectivas reduzidas para 1:" + esc + " para caber na folha"); break; }
            }
            double soma = linha.Sum(W), gap = Math.Max(5, (620 - soma) / (linha.Count + 1)), x = 20 + gap;
            double yc = 10 + titulo + alturaLivre / 2;
            foreach (var q in linha.OrderBy(z => z.GetBoxCenter().X)) { var w = W(q); q.SetBoxCenter(new XYZ(o.X + MM(x + w / 2), o.Y + MM(yc), 0)); x += w + gap; }
        }
    }
    // 4/7: fachadas longas empilhadas no topo; embaixo, coluna da esquerda com frontal e posterior e
    // coluna da direita com as perspectivas (escala delas sobe para 1:125/1:150/1:200 se a coluna não couber)
    if (gab == "4/7") {
        Func<Viewport, double> H = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.Y - b.MinimumPoint.Y); };
        Func<Viewport, double> W = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.X - b.MinimumPoint.X); };
        Func<string, Viewport> VP = n => colocados.FirstOrDefault(vp => ((View)document.GetElement(vp.ViewId)).Name == prefixo + n);
        Action<Viewport, double, double> Pos = (vp, xc, yc) => vp.SetBoxCenter(new XYZ(o.X + MM(xc), o.Y + MM(yc), 0));
        double topo = 584, titulo = 20;
        foreach (var n in new[] { "FACHADA LATERAL DIREITA", "FACHADA LATERAL ESQUERDA" }) {
            var vp = VP(n); if (vp == null) continue; double h = H(vp), w = W(vp);
            Pos(vp, Math.Max(25 + w / 2, Math.Min(832 - w / 2, 25 + w / 2 + 20)), topo - h / 2); topo -= h + titulo;
        }
        double xEsq = 25; double larguraEsq = 0, t2 = topo;
        foreach (var n in new[] { "FACHADA FRONTAL", "FACHADA POSTERIOR" }) {
            var vp = VP(n); if (vp == null) continue; double h = H(vp), w = W(vp); larguraEsq = Math.Max(larguraEsq, w);
            Pos(vp, xEsq + 20 + w / 2, t2 - h / 2); t2 -= h + titulo;
        }
        if (t2 < 10) log.AppendLine("  ⚠ Frontal e posterior não cabem na altura que sobra (" + Math.Round(topo - 10) + " mm) — reduza a escala das fachadas.");
        var col = new[] { "PERSPECTIVA LATERAL DIREITA", "PERSPECTIVA LATERAL ESQUERDA" }.Select(VP).Where(x => x != null).ToList();
        if (col.Count > 0) {
            double x0c = xEsq + 20 + larguraEsq + 20, x1c = 652, livre = topo - 10;
            foreach (var esc in new[] { 0, 125, 150, 200 }) {
                if (esc > 0) { foreach (var vp in col) ((View)document.GetElement(vp.ViewId)).Scale = esc; document.Regenerate(); }
                if (col.Sum(H) + titulo * col.Count <= livre && col.Max(W) <= x1c - x0c) { if (esc > 0) log.AppendLine("  perspectivas reduzidas para 1:" + esc + " para caber na folha"); break; }
            }
            double t3 = topo;
            foreach (var vp in col) { double h = H(vp), w = W(vp); Pos(vp, (x0c + x1c) / 2, t3 - h / 2); t3 -= h + titulo; }
        }
    }
    // conferência de área útil e sobreposição
    var caixas = colocados.Select(vp => { var b = vp.GetBoxOutline(); var v = (View)document.GetElement(vp.ViewId);
        return new { v.Name, x0 = ToMM(b.MinimumPoint.X - o.X), y0 = ToMM(b.MinimumPoint.Y - o.Y), x1 = ToMM(b.MaximumPoint.X - o.X), y1 = ToMM(b.MaximumPoint.Y - o.Y) }; }).ToList();
    foreach (var c in caixas) {
        var aviso = (c.x0 < 20 || c.x1 > 837 || c.y0 < 5 || c.y1 > 589 || (c.x1 > 661 && c.y0 < 302)) ? "  ⚠ FORA DA ÁREA ÚTIL" : "";   // moldura x 25–832, y 10–584; carimbo no canto x > 656, y < 297 (5 mm de tolerância)
        log.AppendLine("  " + c.Name + " → " + Math.Round(c.x1 - c.x0) + " × " + Math.Round(c.y1 - c.y0) + " mm, centro (" + Math.Round((c.x0 + c.x1) / 2) + ", " + Math.Round((c.y0 + c.y1) / 2) + ")" + aviso);
    }
    for (int i = 0; i < caixas.Count; i++) for (int j = i + 1; j < caixas.Count; j++) {
        var a = caixas[i]; var b = caixas[j];
        // a caixa inclui a margem de anotação; só avisa se a sobreposição passar de 15 mm nos dois sentidos
        double sx = Math.Min(a.x1, b.x1) - Math.Max(a.x0, b.x0), sy = Math.Min(a.y1, b.y1) - Math.Max(a.y0, b.y0);
        if (sx > 15 && sy > 15) log.AppendLine("  ⚠ SOBREPOSIÇÃO: " + a.Name + " × " + b.Name + " (" + Math.Round(sx) + " × " + Math.Round(sy) + " mm)");
    }
    t.Commit();
}
return log.ToString();
