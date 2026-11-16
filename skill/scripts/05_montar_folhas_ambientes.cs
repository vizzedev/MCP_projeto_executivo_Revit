// SKILL EXECUTIVO REVIT — Fase 5.5: montagem das folhas de ambientes (6/7 iluminação + forro; 7/7 paginação)
// send_code_to_revit · transactionMode "none"
// parameters[0] = folha: "6/7" ou "7/7"
// parameters[1] = número da folha a criar/usar (ex.: "6/7"; em teste "SKILL 6/7")
// parameters[2] = prefixo dos nomes das vistas criadas pela skill ("" no uso real; "SKILL " em teste)
// parameters[3] = nome da planta geral (6/7: planta de forro, ex. "PAV. TERREO FORRO";
//                 7/7: planta de paginação geral, ex. "PAVIMENTO TERREO PAGINAÇÃO DE PISO")
// parameters[4] = nome da legenda (6/7: "LEGENDA FORROS"; 7/7: "LEGENDA PAGINAÇÃO") — legenda pode repetir em várias folhas
// parameters[5] = prefixo dos modelos de vista (padrão "VZ-")
//
// 6/7: linha 1 = vistas de iluminação dos ambientes secos (quartos, suítes...); linha 2 = áreas molhadas
//      (nome com WC, BANHEIRO, LAVABO, LAVATÓRIO, BANHO); embaixo, planta de forro com a legenda ao lado.
//      As vistas de iluminação são as elevações com o modelo "ELEVAÇÃO ILUMINAÇÃO" que ainda não estão em folha.
// 7/7: planta geral no topo com a legenda à direita; embaixo, uma linha por conjunto de paginação
//      (planta ampliada "PAGINAÇÃO DE PISO X" → isométricas "... X 01/02" → paredes "X 01..04").
// Tudo empilhado pela altura real das vistas, com espaço para os títulos; a linha que passar da largura quebra.
// Vários pavimentos: uma folha por pavimento (6/7 e 7/7 de cada um). Entram só as vistas do pavimento da planta geral
// (elevações de iluminação pela altura do recorte; plantas de paginação pelo nível). O número da folha é livre (o plano
// do caderno numera i/N); o nome vira "PRANCHA <i>" e o conteúdo ganha " - PAVIMENTO X" quando o modelo tem mais de um.
// Ao final confere área útil (moldura x 25–832, y 10–584, menos o carimbo x > 656 e y < 297) e sobreposições.
var gab = parameters[0] as string;
var numero = parameters[1] as string;
var prefixo = parameters.Length > 2 ? (parameters[2] as string ?? "") : "";
var nomePlanta = parameters.Length > 3 ? (parameters[3] as string ?? "") : "";
var nomeLegenda = parameters.Length > 4 ? (parameters[4] as string ?? "") : "";
var pm = parameters.Length > 5 && !string.IsNullOrWhiteSpace(parameters[5] as string) ? (string)parameters[5] : "VZ-";
Func<double, double> MM = v => v / 304.8; Func<double, double> ToMM = v => v * 304.8;
var log = new System.Text.StringBuilder();
if (gab != "6/7" && gab != "7/7") return "Folha não suportada: " + gab;

var vistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
var emFolha = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(Viewport)).Cast<Viewport>().Select(vp => vp.ViewId));
var tiposVp = new FilteredElementCollector(document).OfClass(typeof(ElementType)).Cast<ElementType>().Where(t => t.get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL) != null).ToList();
var comTitulo = tiposVp.FirstOrDefault(t => t.Name == "Title w Line"); var semTitulo = tiposVp.FirstOrDefault(t => t.Name == "SEM TITULO");
var carimbo = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(f => f.FamilyName.Contains("Executivo Arquitetônico"));
if (carimbo == null) return "ERRO: carimbo Executivo Arquitetônico não encontrado";
var molhados = new[] { "WC", "BANHEIRO", "LAVABO", "LAVATÓRIO", "LAVATORIO", "BANHO" };
// pavimento da planta geral: faixa de altura [nível, próximo nível com ambientes)
var plantaGeral = vistas.FirstOrDefault(v => v.Name == nomePlanta) as ViewPlan;
var nivelPav = plantaGeral != null ? plantaGeral.GenLevel : null;
var roomsA = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0 && r.Level != null).ToList();
var elevPavs = roomsA.Select(r => r.Level.Elevation).Distinct().OrderBy(z => z).ToList();
bool variosPav = elevPavs.Count > 1;
double zPav0 = nivelPav != null ? nivelPav.Elevation : double.MinValue;
double zPav1 = nivelPav != null && elevPavs.Any(z => z > zPav0 + 1.0 / 0.3048) ? elevPavs.First(z => z > zPav0 + 1.0 / 0.3048) : double.MaxValue;
Func<View, bool> DoPav = v => {
    if (nivelPav == null || !variosPav) return true;
    var vpl = v as ViewPlan; if (vpl != null) return vpl.GenLevel != null && vpl.GenLevel.Id == nivelPav.Id;
    try { var cb = v.CropBox; var c = cb.Transform.OfPoint((cb.Min + cb.Max) / 2); return c.Z >= zPav0 - 0.5 / 0.3048 && c.Z < zPav1 - 0.5 / 0.3048; } catch { return true; }
};
string sufixoPav = variosPav && nivelPav != null ? " - PAVIMENTO " + nivelPav.Name.ToUpper().Replace("PAV.", "").Replace("N.A", "").Trim() : "";
var mNum = System.Text.RegularExpressions.Regex.Match(numero ?? "", @"(\d+)\s*/");

using (var t = new Transaction(document, "Skill: montar folha " + numero)) {
    t.Start();
    var folha = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().FirstOrDefault(s => s.SheetNumber == numero);
    if (folha == null) { folha = ViewSheet.Create(document, carimbo.Id); folha.SheetNumber = numero; folha.Name = mNum.Success ? "PRANCHA " + mNum.Groups[1].Value.PadLeft(2, '0') : (gab == "6/7" ? "PRANCHA 06" : "PRANCHA 07"); log.AppendLine("Folha criada: " + numero); }
    else log.AppendLine("Folha existente usada: " + numero);
    var pc = folha.LookupParameter("CONTEÚDO DA PRANCHA");
    if (pc != null && !pc.IsReadOnly && string.IsNullOrEmpty(pc.AsString())) pc.Set((gab == "6/7" ? "PLANTA DE FORRO E VISTAS DE ILUMINAÇÃO" : "PAGINAÇÃO DE PISO") + sufixoPav);
    var tb = new FilteredElementCollector(document, folha.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance;
    var o = tb != null ? tb.GetTransform().Origin : XYZ.Zero;
    var colocados = new List<Viewport>();
    Func<View, ElementType, Viewport> Colocar = (v, tipo) => {
        if (v == null) return null;
        if (v.ViewType != ViewType.Legend && emFolha.Contains(v.Id)) { log.AppendLine("  já está numa folha, pulada: " + v.Name); return null; }
        var vp = Viewport.Create(document, folha.Id, v.Id, o); if (tipo != null) vp.ChangeTypeId(tipo.Id); colocados.Add(vp); return vp;
    };
    document.Regenerate();
    Func<Viewport, double> H = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.Y - b.MinimumPoint.Y); };
    Func<Viewport, double> W = vp => { var b = vp.GetBoxOutline(); return ToMM(b.MaximumPoint.X - b.MinimumPoint.X); };
    Action<Viewport, double, double> PosCantoSup = (vp, xEsq, yTopo) => vp.SetBoxCenter(new XYZ(o.X + MM(xEsq + W(vp) / 2), o.Y + MM(yTopo - H(vp) / 2), 0));
    // coloca uma linha da esquerda para a direita a partir do topo; quebra se passar do limite direito; devolve o novo topo
    Func<List<Viewport>, double, double, double, double, double> Linha = (vps, topo, xIni, xMax, titulo) => {
        double x = xIni, alt = 0;
        foreach (var vp in vps) {
            document.Regenerate();
            if (x + W(vp) > xMax && x > xIni) { topo -= alt + titulo; x = xIni; alt = 0; }
            PosCantoSup(vp, x, topo);
            // o título (número + nome) pode ser mais largo que a vista: reserva ~1,7 mm por letra + 14 mm do número
            var tituloVp = document.GetElement(vp.GetTypeId()).Name != "SEM TITULO" ? 14 + 1.7 * ((View)document.GetElement(vp.ViewId)).Name.Length : 0;
            x += Math.Max(W(vp), tituloVp) + 12; alt = Math.Max(alt, H(vp));
        }
        return topo - alt - titulo;
    };
    var planta = vistas.FirstOrDefault(v => v.Name == nomePlanta);
    var legenda = vistas.FirstOrDefault(v => v.ViewType == ViewType.Legend && v.Name == nomeLegenda);
    if (planta == null) log.AppendLine("  FALTA a planta geral: '" + nomePlanta + "'");
    if (legenda == null && nomeLegenda != "") log.AppendLine("  FALTA a legenda: '" + nomeLegenda + "'");

    if (gab == "6/7") {
        var ilum = vistas.OfType<ViewSection>().Where(v => v.ViewType == ViewType.Elevation && v.Name.StartsWith(prefixo) && !emFolha.Contains(v.Id)
            && v.ViewTemplateId != ElementId.InvalidElementId && document.GetElement(v.ViewTemplateId).Name == pm + "ELEVAÇÃO ILUMINAÇÃO" && DoPav(v)).OrderBy(v => v.Name).ToList();
        if (ilum.Count == 0) log.AppendLine("  nenhuma vista de iluminação livre com prefixo '" + prefixo + "'");
        var secos = ilum.Where(v => !molhados.Any(m => v.Name.ToUpper().Contains(m))).Select(v => Colocar(v, comTitulo)).Where(x => x != null).ToList();
        var molh = ilum.Where(v => molhados.Any(m => v.Name.ToUpper().Contains(m))).OrderBy(v => v.Name.ToUpper().Contains("LAVAT") ? 0 : 1).ThenBy(v => v.Name).Select(v => Colocar(v, comTitulo)).Where(x => x != null).ToList();
        double topo = 572;
        if (secos.Count > 0) topo = Linha(secos, topo, 35, 825, 22);
        if (molh.Count > 0) topo = Linha(molh, topo, 35, 825, 22);
        var vpP = Colocar(planta, comTitulo); var vpL = Colocar(legenda, semTitulo);
        document.Regenerate();
        // planta de forro na base (título embaixo, 20 mm), legenda à direita alinhada pelo topo, sem invadir o carimbo (x ≤ 652)
        if (vpP != null) {
            double topoP = Math.Min(topo, 30 + H(vpP)); PosCantoSup(vpP, 27, topoP);
            if (vpL != null) PosCantoSup(vpL, Math.Min(27 + W(vpP) + 10, 652 - W(vpL)), topoP);
            if (topoP - H(vpP) < 25) log.AppendLine("  ⚠ A planta de forro não cabe abaixo das vistas de iluminação — reduza a escala da planta ou divida as vistas em outra folha.");
        }
        else if (vpL != null) PosCantoSup(vpL, 27, topo);
    } else {
        var vpP = Colocar(planta, comTitulo); var vpL = Colocar(legenda, semTitulo);
        document.Regenerate();
        double topo = 584;
        if (vpP != null) {
            // se a planta passa por cima do carimbo (x > 656), sobe até a base dela ficar acima dele (y ≥ 302);
            // a caixa da vista tem margem vazia, então pode passar um pouco da moldura (o arquiteto faz o mesmo)
            if (25 + W(vpP) > 656 && topo - H(vpP) < 302) topo = Math.Min(610, 302 + H(vpP));
            PosCantoSup(vpP, 25, topo);
            if (vpL != null) PosCantoSup(vpL, Math.Min(832 - W(vpL), 25 + W(vpP) + 15), topo - H(vpP) + H(vpL) + 22);
            topo -= H(vpP) + 22;
        }
        else if (vpL != null) { PosCantoSup(vpL, 832 - W(vpL), topo); }
        // conjuntos de paginação: plantas ampliadas "PAGINAÇÃO DE PISO X" (com prefixo, livres, não a geral)
        var amps = vistas.OfType<ViewPlan>().Where(v => v.ViewType == ViewType.FloorPlan && v.Name.StartsWith(prefixo + "PAGINAÇÃO DE PISO ") && v.Name != nomePlanta && !emFolha.Contains(v.Id) && DoPav(v)).OrderBy(v => v.Name).ToList();
        if (amps.Count == 0) log.AppendLine("  nenhum conjunto de paginação livre com prefixo '" + prefixo + "'");
        var linhas = new List<List<Viewport>>();
        foreach (var amp in amps) {
            var X = amp.Name.Substring((prefixo + "PAGINAÇÃO DE PISO ").Length);
            var linha = new List<Viewport>();
            var a = Colocar(amp, comTitulo); if (a != null) linha.Add(a);
            foreach (var n in new[] { "01", "02" }) { var v3 = vistas.OfType<View3D>().FirstOrDefault(v => v.Name == prefixo + "PAGINAÇÃO DE PISO " + X + " " + n); var vp = Colocar(v3, semTitulo); if (vp != null) linha.Add(vp); }
            foreach (var n in new[] { "01", "02", "03", "04" }) { var el = vistas.FirstOrDefault(v => v.ViewType == ViewType.Elevation && v.Name == prefixo + X + " " + n); var vp = Colocar(el, comTitulo); if (vp != null) linha.Add(vp); }
            linhas.Add(linha); log.AppendLine("  conjunto " + X + ": " + linha.Count + " vistas");
        }
        // uma linha por conjunto; se não couber até a margem inferior, as isométricas passam para 1:75 e depois 1:100
        double topoLinhas = topo;
        foreach (var esc in new[] { 0, 75, 100 }) {
            if (esc > 0) { foreach (var vp in linhas.SelectMany(l => l).Where(z => document.GetElement(z.ViewId) is View3D)) ((View)document.GetElement(vp.ViewId)).Scale = esc; document.Regenerate(); }
            topo = topoLinhas; foreach (var linha in linhas) topo = Linha(linha, topo, 35, 645, 16);
            if (topo + 16 >= 8) { if (esc > 0) log.AppendLine("  isométricas reduzidas para 1:" + esc + " para caber na folha"); break; }
            if (esc == 100) log.AppendLine("  ⚠ Os conjuntos de paginação não cabem nesta folha — divida em duas folhas.");
        }
    }
    document.Regenerate();
    // conferência
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
