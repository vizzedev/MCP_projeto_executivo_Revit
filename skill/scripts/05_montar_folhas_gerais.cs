// SKILL EXECUTIVO REVIT — Fase 5.5: montagem das folhas gerais do caderno ARQ (1/7, 2/7, 5/7)
// send_code_to_revit · transactionMode "none"
// parameters[0] = folha: "1/7" (planta executiva + implantação + tabelas de esquadrias),
//                 "2/7" (cobertura + situação) ou "5/7" (iluminação e tomadas + tabela luminotécnica)
// parameters[1] = número da folha a criar/usar (ex.: "1/7"; em teste "SKILL 1/7")
// parameters[2] = prefixo das vistas criadas pela skill ("" no uso real; "SKILL " em teste)
// parameters[3] = pavimento das plantas (padrão "TERREO")
//
// Regra (extraída do projeto de referência): blocos empilhados de cima para baixo; cada bloco = planta + legendas à direita
// (se não couberem à direita, descem para baixo da planta, à esquerda); tabelas numa linha na base, da esquerda para a direita.
// A planta que passa por cima do carimbo sobe até a base dela ficar acima dele. Legendas e tabelas podem repetir em várias folhas.
// Relatório: o que faltou, tabelas sem itens (modelo ainda sem portas/janelas/luminárias) e conferência de área útil.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var gab = P(0); var numero = P(1); var prefixo = parameters.Length > 2 ? ((parameters[2] as string) ?? "") : ""; var pav = P(3) != "" ? P(3).ToUpper() : "TERREO";
Func<double, double> MM = v => v / 304.8; Func<double, double> ToMM = v => v * 304.8;
var log = new System.Text.StringBuilder();
// blocos: (vista principal, legendas); tabelas; conteúdo do carimbo; nome da folha
var blocos = new List<Tuple<string, string[]>>(); var tabelas = new string[0]; string conteudo, nomeFolha;
if (gab == "1/7") {
    blocos.Add(Tuple.Create("PAVIMENTO " + pav + " EXECUTIVO", new[] { "PLANTA BAIXA EXECUTIVO" }));
    blocos.Add(Tuple.Create("IMPLANTAÇÃO", new[] { "IMPLANTAÇÃO" }));
    tabelas = new[] { "TABELA DE JANELAS", "TABELA DE PORTAS" }; conteudo = "PLANTA BAIXA EXECUTIVO, IMPLANTAÇÃO E TABELAS DE ESQUADRIAS"; nomeFolha = "PRANCHA 01";
} else if (gab == "2/7") {
    blocos.Add(Tuple.Create("COBERTURA EXECUTIVO", new[] { "PLANTA DE COBERTURA EXECUTIVO" }));
    blocos.Add(Tuple.Create("PLANTA DE SITUAÇÃO", new string[0]));
    conteudo = "PLANTA DE COBERTURA E PLANTA DE SITUAÇÃO"; nomeFolha = "PRANCHA 02";
} else if (gab == "5/7") {
    blocos.Add(Tuple.Create("PAVIMENTO " + pav + " ILUMINAÇÃO E INTERRUPTORES", new[] { "LEGENDA LUMINOTÉCNICO", "LEGENDA DE TOMADAS" }));
    blocos.Add(Tuple.Create("PAVIMENTO " + pav + " PONTOS DE TOMADA", new string[0]));
    tabelas = new[] { "TABELA LUMINOTÉCNICO" }; conteudo = "PLANTA DE ILUMINAÇÃO, INTERRUPTORES E PONTOS DE TOMADA"; nomeFolha = "PRANCHA 05";
} else return "Folha não suportada: " + gab + " (1/7, 2/7, 5/7)";

var vistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
Func<string, View> Vista = n => vistas.FirstOrDefault(v => !(v is ViewSchedule) && v.Name == prefixo + n) ?? vistas.FirstOrDefault(v => !(v is ViewSchedule) && v.Name == n);
var emFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(vp => vp.ViewId));
var tiposVp = new FilteredElementCollector(document).OfClass(typeof(ElementType)).Cast<ElementType>().Where(t => t.get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL) != null).ToList();
var comTitulo = tiposVp.FirstOrDefault(t => t.Name == "Title w Line"); var semTitulo = tiposVp.FirstOrDefault(t => t.Name == "SEM TITULO");
var carimbo = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(f => f.FamilyName.Contains("Executivo Arquitetônico"));
if (carimbo == null) return "ERRO: carimbo Executivo Arquitetônico não encontrado";

using (var t = new Transaction(document, "Skill: montar folha " + numero)) {
    t.Start();
    var folha = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().FirstOrDefault(s => s.SheetNumber == numero);
    if (folha == null) { folha = ViewSheet.Create(document, carimbo.Id); folha.SheetNumber = numero; folha.Name = nomeFolha; log.AppendLine("Folha criada: " + numero); }
    else log.AppendLine("Folha existente usada: " + numero);
    var pc = folha.LookupParameter("CONTEÚDO DA PRANCHA"); if (pc != null && !pc.IsReadOnly && string.IsNullOrEmpty(pc.AsString())) pc.Set(conteudo);
    var tb = new FilteredElementCollector(document, folha.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
    var o = tb != null ? tb.GetTransform().Origin : XYZ.Zero;
    var colocados = new List<Viewport>();
    Func<View, ElementType, Viewport> Colocar = (v, tipo) => {
        if (v == null) return null;
        if (v.ViewType != ViewType.Legend && emFolha.Contains(v.Id)) { log.AppendLine("  já está numa folha, pulada: " + v.Name); return null; }
        var vp = Viewport.Create(document, folha.Id, v.Id, o); if (tipo != null) vp.ChangeTypeId(tipo.Id); colocados.Add(vp); return vp;
    };
    Func<Viewport, double> H = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.Y - b.MinimumPoint.Y); };
    Func<Viewport, double> W = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.X - b.MinimumPoint.X); };
    Action<Viewport, double, double> PosCantoSup = (vp, xEsq, yTopo) => vp.SetBoxCenter(new XYZ(o.X + MM(xEsq + W(vp) / 2), o.Y + MM(yTopo - H(vp) / 2), 0));

    // 1) coloca tudo e mede
    var itens = new List<Tuple<Viewport, List<Viewport>>>();
    foreach (var bl in blocos) {
        var vp = Colocar(Vista(bl.Item1), comTitulo);
        if (vp == null) { if (Vista(bl.Item1) == null) log.AppendLine("  ⚠ FALTA a vista: " + prefixo + bl.Item1 + " (rode 05_plantas.cs / 05_situacao.cs)"); continue; }
        var legs = bl.Item2.Select(n => { var lv = vistas.FirstOrDefault(v => v.ViewType == ViewType.Legend && v.Name == n); if (lv == null) log.AppendLine("  ⚠ FALTA a legenda: " + n); return Colocar(lv, semTitulo); }).Where(x => x != null).ToList();
        itens.Add(Tuple.Create(vp, legs));
    }
    var tabs = new List<Tuple<ScheduleSheetInstance, ViewSchedule, int>>();
    foreach (var n in tabelas) {
        var vs = new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().FirstOrDefault(s => s.Name == prefixo + n) ?? new FilteredElementCollector(document).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>().FirstOrDefault(s => s.Name == n);
        if (vs == null) { log.AppendLine("  ⚠ FALTA a tabela: " + n); continue; }
        int qtd = new FilteredElementCollector(document, vs.Id).WhereElementIsNotElementType().GetElementCount();
        tabs.Add(Tuple.Create(ScheduleSheetInstance.Create(document, folha.Id, vs.Id, o), vs, qtd));
    }
    document.Regenerate();
    Func<ScheduleSheetInstance, BoundingBoxXYZ> BB = si => si.get_BoundingBox(folha);
    double hTab = tabs.Count > 0 ? tabs.Max(x => ToMM(BB(x.Item1).Max.Y - BB(x.Item1).Min.Y)) : 0;
    double baseLivre = tabs.Count > 0 ? 10 + hTab : 10;      // os blocos descem até aqui (tabelas ancoradas na base)

    // 2) decide legendas à direita (pode entrar até 20 mm na margem vazia da planta; não invade o carimbo) ou embaixo
    var alturas = new List<double>(); var aDireita = new List<bool>(); var xLegs = new List<double>();
    foreach (var it in itens) {
        double w = W(it.Item1), h = H(it.Item1), somaL = it.Item2.Sum(W) + 8 * Math.Max(0, it.Item2.Count - 1);
        double hL = it.Item2.Count > 0 ? it.Item2.Max(H) : 0;
        double xL = 25 + w + 10;
        // limite: 832 acima do carimbo; 652 se a legenda puder descer à faixa do carimbo (y < 297) — decidido de forma conservadora
        double lim = 832; if (xL + somaL > lim) xL = lim - somaL;
        bool dir = it.Item2.Count == 0 || (25 + w) - xL <= 20;
        aDireita.Add(dir); xLegs.Add(xL);
        alturas.Add(dir ? Math.Max(h, hL + 8) : h + 22 + hL);
    }
    // 3) topo inicial (sobe a 1ª planta se ela passar por cima do carimbo) e espaço entre blocos
    double topo = 584;
    if (itens.Count > 0) { double w0 = W(itens[0].Item1), h0 = H(itens[0].Item1); if (25 + w0 > 656 && topo - h0 < 302) topo = Math.Min(610, 302 + h0); }
    int vaos = itens.Count - 1 + (tabs.Count > 0 ? 1 : 0);
    double livre = topo - baseLivre - alturas.Sum(), vao = vaos > 0 ? Math.Min(22, livre / vaos) : 22;
    if (vao < 6) { log.AppendLine("  ⚠ O conteúdo não cabe na altura da folha (faltam " + Math.Round(6 * vaos - livre) + " mm) — reduza a escala de uma planta ou divida a folha."); vao = 6; }
    else if (vao < 22) log.AppendLine("  espaço entre blocos reduzido para " + Math.Round(vao) + " mm para caber (como no projeto de referência, os títulos ficam na margem vazia das vistas)");
    for (int k = 0; k < itens.Count; k++) {
        var it = itens[k]; double h = H(it.Item1);
        PosCantoSup(it.Item1, 25, topo);
        if (it.Item2.Count > 0) {
            if (aDireita[k]) {
                double xL = xLegs[k], topoL = Math.Min(topo - 8, 579);
                // se a legenda cair na faixa do carimbo, encosta à esquerda dele
                double somaL = it.Item2.Sum(W) + 8 * Math.Max(0, it.Item2.Count - 1);
                if (topoL - it.Item2.Max(H) < 302 && xL + somaL > 652) xL = Math.Max(25 + W(it.Item1) - 20, 652 - somaL);
                foreach (var lg in it.Item2) { PosCantoSup(lg, xL, topoL); xL += W(lg) + 8; }
            } else { double x = 35; foreach (var lg in it.Item2) { PosCantoSup(lg, x, topo - h - 22); x += W(lg) + 8; } }
        }
        topo -= alturas[k] + vao;
    }
    // 4) tabelas na base, da esquerda para a direita, alinhadas pelo topo
    double xt = 35, topoT = baseLivre;
    foreach (var tt in tabs) {
        var bb = BB(tt.Item1); double wT = ToMM(bb.Max.X - bb.Min.X), hT = ToMM(bb.Max.Y - bb.Min.Y);
        ElementTransformUtils.MoveElement(document, tt.Item1.Id, new XYZ(o.X + MM(xt) - bb.Min.X, o.Y + MM(topoT) - bb.Max.Y, 0));
        log.AppendLine((tt.Item3 == 0 ? "  ⚠ " : "  ") + "tabela " + tt.Item2.Name + ": " + tt.Item3 + " itens" + (tt.Item3 == 0 ? " → VAZIA: modelar antes de emitir" : "") + " | " + Math.Round(wT) + " × " + Math.Round(hT) + " mm, x " + Math.Round(xt) + ".." + Math.Round(xt + wT) + " y " + Math.Round(topoT - hT) + ".." + Math.Round(topoT) + (xt + wT > 652 ? "  ⚠ INVADE O CARIMBO" : ""));
        xt += wT + 12;
    }
    document.Regenerate();
    var caixas = colocados.Select(vp => { var b = vp.GetBoxOutline(); var v = (View)document.GetElement(vp.ViewId);
        return new { v.Name, x0 = ToMM(b.MinimumPoint.X - o.X), y0 = ToMM(b.MinimumPoint.Y - o.Y), x1 = ToMM(b.MaximumPoint.X - o.X), y1 = ToMM(b.MaximumPoint.Y - o.Y) }; }).ToList();
    foreach (var c in caixas) {
        var aviso = (c.x0 < 20 || c.x1 > 837 || c.y0 < 5 || c.y1 > 612 || (c.x1 > 661 && c.y0 < 297)) ? "  ⚠ FORA DA ÁREA ÚTIL" : "";
        log.AppendLine("  " + c.Name + " → " + Math.Round(c.x1 - c.x0) + " × " + Math.Round(c.y1 - c.y0) + " mm, x " + Math.Round(c.x0) + ".." + Math.Round(c.x1) + " y " + Math.Round(c.y0) + ".." + Math.Round(c.y1) + aviso);
    }
    for (int i = 0; i < caixas.Count; i++) for (int j = i + 1; j < caixas.Count; j++) {
        var a = caixas[i]; var b = caixas[j];
        double sx = Math.Min(a.x1, b.x1) - Math.Max(a.x0, b.x0), sy = Math.Min(a.y1, b.y1) - Math.Max(a.y0, b.y0);
        if (sx > 15 && sy > 15) log.AppendLine("  ⚠ SOBREPOSIÇÃO: " + a.Name + " × " + b.Name + " (" + Math.Round(sx) + " × " + Math.Round(sy) + " mm)");
    }
    t.Commit();
}
return log.ToString();
