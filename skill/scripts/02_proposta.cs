// SKILL EXECUTIVO REVIT — Fase 2: proposta de layout (uma folha por pavimento)
// send_code_to_revit · transactionMode "none"
// parameters[0] = prefixo ("" no uso real; "SKILL " em teste: duplica as vistas originais e cria folhas "SKILL i/N")
// parameters[1] = prefixo dos modelos de vista (padrão "VZ-")
//
// Regra (extraída do projeto de referência, carimbo de proposta = faixa na base; área útil x 25–832, y 58–584 mm):
//   folha i/N = planta de layout do pavimento i, no topo e centralizada;
//   folha 1/N também leva a implantação de layout (embaixo, à direita) e a legenda de implantação (à esquerda, centrada na altura dela).
// Pavimentos = níveis "PAV. X N.A" com ambientes. Plantas "PAV. X LAYOUT" e "IMPLANTAÇÃO LAYOUT" vêm do template; cria se faltar.
// As folhas de proposta (carimbo com "Proposta") são criadas na fase 1; aqui só se cria a que faltar.
var prefixo = parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "";
var pm = parameters.Length > 1 && !string.IsNullOrWhiteSpace(parameters[1] as string) ? (string)parameters[1] : "VZ-";
Func<double, double> MM = v => v / 304.8; Func<double, double> ToMM = v => v * 304.8;
Func<string, string> Norm = s => System.Text.RegularExpressions.Regex.Replace(new string((s ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper(), "\\s+", " ").Trim();
var log = new System.Text.StringBuilder();
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
var pavs = niveis.Where(l => l.Name.ToUpper().StartsWith("PAV.") && l.Name.ToUpper().EndsWith("N.A") && rooms.Any(r => r.LevelId == l.Id)).ToList();
if (pavs.Count == 0) return "ERRO: nenhum pavimento \"PAV. X N.A\" com ambientes — rode a fase 3.";
var vistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
var modelos = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate).ToList();
var tiposPlanta = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t => t.ViewFamily == ViewFamily.FloorPlan).ToList();
var emFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(vp => vp.ViewId));
var carimbo = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(f => f.FamilyName.ToUpper().Contains("PROPOSTA"));
if (carimbo == null) return "ERRO: carimbo de proposta não encontrado no template.";
var tiposVp = new FilteredElementCollector(document).OfClass(typeof(ElementType)).Cast<ElementType>().Where(t => t.get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL) != null).ToList();
var comTitulo = tiposVp.FirstOrDefault(t => t.Name == "Title w Line"); var semTitulo = tiposVp.FirstOrDefault(t => t.Name == "SEM TITULO");
int N = pavs.Count;

using (var t = new Transaction(document, "Skill: proposta de layout")) {
    t.Start();
    // garante uma planta: procura pelo nome (sem acento/espaços duplos); em teste duplica a original; senão cria
    Func<Func<string, bool>, string, Level, string, int, View> Planta = (casa, nomeNovo, nivel, trechoModelo, escala) => {
        var original = vistas.FirstOrDefault(v => v is ViewPlan && !v.Name.StartsWith("SKILL ") && casa(Norm(v.Name)));
        if (prefixo != "") {
            var teste = vistas.FirstOrDefault(v => v.Name == prefixo + (original != null ? original.Name : nomeNovo));
            if (teste != null) return teste;
            if (original != null) { var d = (View)document.GetElement(original.Duplicate(ViewDuplicateOption.WithDetailing)); d.Name = prefixo + original.Name; log.AppendLine("  (teste) duplicada: " + d.Name); return d; }
        } else if (original != null) return original;
        var tipo = tiposPlanta.OrderByDescending(x => Norm(x.Name).Contains("APRESENTA")).First();
        var v2 = ViewPlan.Create(document, tipo.Id, nivel.Id); v2.Name = prefixo + nomeNovo; v2.Scale = escala;
        var md = modelos.FirstOrDefault(m => Norm(m.Name) == Norm(pm + trechoModelo)); if (md != null) v2.ViewTemplateId = md.Id; else log.AppendLine("  ⚠ modelo '" + pm + trechoModelo + "' não encontrado");
        log.AppendLine("  criada: " + v2.Name); return v2;
    };
    Func<Viewport, double> H = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.Y - b.MinimumPoint.Y); };
    Func<Viewport, double> W = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.X - b.MinimumPoint.X); };
    for (int i = 0; i < N; i++) {
        var nv = pavs[i]; var pav = nv.Name.Substring(4, nv.Name.Length - 4 - 3).Trim().ToUpper();   // "PAV. TERREO N.A" → "TERREO"
        var numero = prefixo + (i + 1) + "/" + N;
        var folha = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().FirstOrDefault(s => s.SheetNumber == numero);
        if (folha == null) { folha = ViewSheet.Create(document, carimbo.Id); folha.SheetNumber = numero; folha.Name = "PRANCHA " + (i + 1).ToString("00"); log.AppendLine("Folha criada: " + numero); }
        else log.AppendLine("Folha " + numero);
        var tb = new FilteredElementCollector(document, folha.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
        var o = tb != null ? tb.GetTransform().Origin : XYZ.Zero;
        Action<Viewport, double, double> Pos = (vp, xEsq, yTopo) => vp.SetBoxCenter(new XYZ(o.X + MM(xEsq + W(vp) / 2), o.Y + MM(yTopo - H(vp) / 2), 0));
        var planta = Planta(n => n.StartsWith("PAV. " + Norm(pav)) && n.EndsWith("LAYOUT"), "PAV. " + pav + " LAYOUT", nv, "PLANTA BAIXA LAYOUT", 75);
        Viewport vpP = null;
        if (emFolha.Contains(planta.Id)) log.AppendLine("  já está numa folha: " + planta.Name);
        else { vpP = Viewport.Create(document, folha.Id, planta.Id, o); if (comTitulo != null) vpP.ChangeTypeId(comTitulo.Id); }
        document.Regenerate();
        double topoLivre = 584;
        if (vpP != null) {
            double w = W(vpP), h = H(vpP);
            Pos(vpP, 25 + Math.Max(0, (807 - w) / 2), 573);
            topoLivre = 573 - h - 22;
            int amb = rooms.Count(r => r.LevelId == nv.Id);
            log.AppendLine("  planta " + planta.Name + ": " + Math.Round(w) + " × " + Math.Round(h) + " mm, " + amb + " ambientes" + (w > 807 ? "  ⚠ MAIS LARGA QUE A FOLHA" : ""));
        }
        if (i == 0) {
            var impl = Planta(n => n.Contains("IMPLANTA") && n.Contains("LAYOUT"), "IMPLANTAÇÃO LAYOUT", nv, "IMPLANTAÇÃO LAYOUT", 100);
            var leg = vistas.FirstOrDefault(v => v.ViewType == ViewType.Legend && Norm(v.Name).Contains("IMPLANTA") && Norm(v.Name).Contains("LAYOUT"));
            Viewport vpI = null, vpL = null;
            if (emFolha.Contains(impl.Id)) log.AppendLine("  já está numa folha: " + impl.Name);
            else { vpI = Viewport.Create(document, folha.Id, impl.Id, o); if (comTitulo != null) vpI.ChangeTypeId(comTitulo.Id); }
            if (leg != null) { vpL = Viewport.Create(document, folha.Id, leg.Id, o); if (semTitulo != null) vpL.ChangeTypeId(semTitulo.Id); } else log.AppendLine("  ⚠ legenda de implantação de layout não encontrada");
            document.Regenerate();
            if (vpI != null) {
                double hI = H(vpI), wI = W(vpI);
                double topoI = Math.Min(topoLivre, 58 + 25 + hI + (topoLivre - 58 - 25 - hI) / 2);   // centraliza na altura que sobra, sem descer abaixo de 58 + título
                Pos(vpI, 802 - wI, topoI);
                if (topoI - hI < 58 + 20) log.AppendLine("  ⚠ implantação encosta no carimbo (sobram " + Math.Round(topoLivre - 58) + " mm de altura para " + Math.Round(hI) + " mm)");
                if (vpL != null) Pos(vpL, Math.Max(35, 802 - wI - 30 - W(vpL) - 0), topoI - hI / 2 + H(vpL) / 2);
                log.AppendLine("  implantação: " + Math.Round(wI) + " × " + Math.Round(hI) + " mm" + (vpL != null ? " + legenda " + Math.Round(W(vpL)) + " × " + Math.Round(H(vpL)) + " mm" : ""));
            }
        }
    }
    t.Commit();
}
return log.ToString();
