// SKILL EXECUTIVO REVIT — Fase 5: planta de situação vetorial (vista de desenho)
// send_code_to_revit · transactionMode "none"
// parameters[0]  = nome da vista (padrão "PLANTA DE SITUAÇÃO"; em teste "SKILL PLANTA DE SITUAÇÃO")
// parameters[1]  = número do lote (ex.: "27")
// parameters[2]  = quadra (ex.: "L")
// parameters[3]  = rua da frente (ex.: "RUA NELSON MAIA")
// parameters[4]  = distância da divisa do lote até a esquina mais próxima, em m (ex.: "105")
// parameters[5]  = rua da esquina (ex.: "AVENIDA DAS ACÁCIAS")
// parameters[6]  = lado da esquina olhando o lote da rua: "D" (direita) ou "E" (esquerda)
// parameters[7]  = lotes vizinhos à esquerda, do mais próximo ao mais distante, separados por ";" (ex.: "28;29") — opcional
// parameters[8]  = lotes vizinhos à direita (ex.: "26;25") — opcional
// parameters[9]  = ponto cardeal para onde a frente do lote está voltada: N, NE, L, SE, S, SO, O, NO (ex.: "S")
// parameters[10] = largura das ruas em m (padrão 12)
// parameters[11] = testada e profundidade "15;40" — opcional; vazio = lê das linhas de propriedade (ou das paredes) do modelo
//
// Desenha: lote da obra com hachura cruzada vermelha, contorno grosso, LOTE/QUADRA e medidas; lotes vizinhos com número;
// alinhamento da quadra até a esquina com a distância; rua da frente e rua da esquina com nome; símbolo de norte girado.
// Escala escolhida para caber em ~400 × 220 mm (1:250, 1:500, 1:750, 1:1000). Recria a vista se já existir com o mesmo nome.
// Se o escritório tiver a planta do loteamento (imagem/PDF/DWG), use a alternativa: inserir o arquivo nesta vista.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var nomeVista = P(0) != "" ? P(0) : "PLANTA DE SITUAÇÃO";
var lote = P(1); var quadra = P(2); var ruaFrente = P(3).ToUpper();
var ci = System.Globalization.CultureInfo.InvariantCulture;
Func<string, double, double> Num = (s, padrao) => { double v; return double.TryParse(s.Replace(",", "."), System.Globalization.NumberStyles.Any, ci, out v) ? v : padrao; };
double distEsquina = Num(P(4), 0); var ruaEsquina = P(5).ToUpper(); bool esquinaDireita = P(6).ToUpper() != "E";
var vizEsq = P(7).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var vizDir = P(8).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var cardeal = P(9).ToUpper().Replace("W", "O").Replace("E", "L");
double larguraRua = Num(P(10), 12);
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();

// --- testada (T) e profundidade (Pr) do lote
double T = 0, Pr = 0;
if (P(11).Contains(";")) { var tp = P(11).Split(';'); T = Num(tp[0], 0); Pr = Num(tp[1], 0); log.AppendLine("Lote informado: " + T + " × " + Pr + " m"); }
else {
    var bbs = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
    string fonte = "linhas de propriedade";
    if (bbs.Count == 0) { bbs = new FilteredElementCollector(document).OfClass(typeof(Wall)).ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList(); fonte = "paredes (sem linhas de propriedade)"; }
    if (bbs.Count == 0) return "ERRO: sem linhas de propriedade nem paredes — informe testada e profundidade em parameters[11] (ex.: \"15;40\").";
    double x0 = bbs.Min(b => b.Min.X), x1 = bbs.Max(b => b.Max.X), y0 = bbs.Min(b => b.Min.Y), y1 = bbs.Max(b => b.Max.Y);
    // frente pela posição da CALÇADA (como nas fachadas); a testada é a medida paralela à rua
    var calcada = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
        .FirstOrDefault(r => r.Location != null && r.Area > 0 && (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper().Contains("CALÇADA"));
    bool frenteEmX = true;
    if (calcada != null) { var p = ((LocationPoint)calcada.Location).Point; double dx = (p.X - (x0 + x1) / 2) / (x1 - x0), dy = (p.Y - (y0 + y1) / 2) / (y1 - y0); frenteEmX = Math.Abs(dx) > Math.Abs(dy); }
    else frenteEmX = (x1 - x0) > (y1 - y0);   // sem calçada: frente no lado menor
    T = (frenteEmX ? (y1 - y0) : (x1 - x0)) * 0.3048; Pr = (frenteEmX ? (x1 - x0) : (y1 - y0)) * 0.3048;
    T = Math.Round(T, 2); Pr = Math.Round(Pr, 2);   // ao centímetro: as medidas têm de bater com as cotas do lote na implantação
    log.AppendLine("Lote lido das " + fonte + ": testada " + T.ToString("0.00") + " m × profundidade " + Pr.ToString("0.00") + " m");
}
if (T <= 0 || Pr <= 0) return "ERRO: medidas do lote inválidas";

// --- geometria do desenho (m): rua da frente embaixo (y < 0), lote de x = 0 a T, y = 0 a Pr
int nEsq = Math.Max(vizEsq.Count, 2), nDir = Math.Max(vizDir.Count, 2);
double xEsqLotes = -nEsq * T, xDirLotes = T + nDir * T;
double xEsquina = distEsquina > 0 ? (esquinaDireita ? T + distEsquina : -distEsquina) : double.NaN;
double xMin = Math.Min(xEsqLotes, double.IsNaN(xEsquina) ? xEsqLotes : xEsquina - larguraRua) - 5;
double xMax = Math.Max(xDirLotes, double.IsNaN(xEsquina) ? xDirLotes : xEsquina + larguraRua) + 5;
double yMin = -larguraRua - 6, yMax = Pr + 12;   // reavaliado abaixo com o espaço do norte
// escala para caber em ~400 × 220 mm
int escala = new[] { 250, 500, 750, 1000, 1500, 2000 }.FirstOrDefault(e => (xMax - xMin) * 1000 / e <= 400 && (yMax - yMin) * 1000 / e <= 220);
if (escala == 0) escala = 2000;
double mmM = escala / 1000.0;                       // 1 mm no papel, em metros no desenho
double xNorte = xMax + 12 * mmM, yNorte = Pr + 14 * mmM; // norte à direita e acima do desenho
xMax = xNorte + 10 * mmM; yMax = Math.Max(yMax, yNorte + 10 * mmM);

// --- recursos do template
var tipoDesenho = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t => t.ViewFamily == ViewFamily.Drafting).OrderBy(t => t.Name.ToUpper().Contains("LOGO") ? 1 : 0).First();
var tiposTexto = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
var txt = tiposTexto.FirstOrDefault(t => t.Name == "VZ-Arial Preta 2.50mm") ?? tiposTexto.First();
var txtRua = tiposTexto.FirstOrDefault(t => t.Name == "ARIAL 3,5mm CINZA") ?? tiposTexto.FirstOrDefault(t => t.Name.Contains("3,5") || t.Name.Contains("3.5")) ?? txt;
var txtPeq = tiposTexto.FirstOrDefault(t => t.Name == "VZ-Arial Preta 2.00mm") ?? txt;
var catLinhas = document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
Func<string, GraphicsStyle> Estilo = n => { var c = catLinhas.SubCategories.Cast<Category>().FirstOrDefault(x => x.Name == n); return c != null ? c.GetGraphicsStyle(GraphicsStyleType.Projection) : null; };
var gGrossa = Estilo("<Linhas grossas>"); var gMedia = Estilo("<Linhas médias>"); var gFina = Estilo("<Linhas finas>");
var simboloNorte = new FilteredElementCollector(document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(f => f.FamilyName.ToUpper().Contains("NORTE"));

using (var t = new Transaction(document, "Skill: planta de situação")) {
    t.Start();
    var antiga = new FilteredElementCollector(document).OfClass(typeof(ViewDrafting)).Cast<View>().FirstOrDefault(v => v.Name == nomeVista);
    if (antiga != null) { document.Delete(antiga.Id); log.AppendLine("Vista anterior '" + nomeVista + "' substituída."); }
    var vista = ViewDrafting.Create(document, tipoDesenho.Id); vista.Name = nomeVista; vista.Scale = escala;

    // tipo de hachura do lote: cruzada vermelha (cria se não existir)
    var tiposRegiao = new FilteredElementCollector(document).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().ToList();
    // hachura do lote: cruzada a 45°, vermelha (padrão e tipo próprios da skill; cria se não existirem)
    var padrao = new FilteredElementCollector(document).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().FirstOrDefault(f => f.Name == "VZ-Hachura Cruzada 45");
    if (padrao == null) padrao = FillPatternElement.Create(document, new FillPattern("VZ-Hachura Cruzada 45", FillPatternTarget.Drafting, FillPatternHostOrientation.ToView, Math.PI / 4, F(0.0025), F(0.0025)));
    var regiaoLote = tiposRegiao.FirstOrDefault(r => r.Name == "VZ-Lote do Projeto");
    if (regiaoLote == null) { regiaoLote = tiposRegiao.First().Duplicate("VZ-Lote do Projeto") as FilledRegionType; log.AppendLine("Criado o tipo de região 'VZ-Lote do Projeto'."); }
    regiaoLote.ForegroundPatternId = padrao.Id; regiaoLote.ForegroundPatternColor = new Color(220, 0, 0);
    regiaoLote.BackgroundPatternId = ElementId.InvalidElementId;
    Action<double, double, double, double, GraphicsStyle> L = (ax, ay, bx, by, g) => {
        var dc = document.Create.NewDetailCurve(vista, Line.CreateBound(new XYZ(F(ax), F(ay), 0), new XYZ(F(bx), F(by), 0)));
        if (g != null) dc.LineStyle = g;
    };
    Action<double, double, string, TextNoteType, double> Tx = (x, y, s, tipo, rot) => {
        var op = new TextNoteOptions(tipo.Id) { HorizontalAlignment = HorizontalTextAlignment.Center, VerticalAlignment = VerticalTextAlignment.Middle, Rotation = rot };
        TextNote.Create(document, vista.Id, new XYZ(F(x), F(y), 0), s, op);
    };
    Func<double, string> Mf = m => m.ToString("0.00", new System.Globalization.CultureInfo("pt-BR")) + " m";

    // lote da obra: hachura + contorno grosso + textos
    var laco = new CurveLoop();
    var cantos = new[] { new XYZ(0, 0, 0), new XYZ(F(T), 0, 0), new XYZ(F(T), F(Pr), 0), new XYZ(0, F(Pr), 0) };
    for (int i = 0; i < 4; i++) laco.Append(Line.CreateBound(cantos[i], cantos[(i + 1) % 4]));
    FilledRegion.Create(document, regiaoLote.Id, vista.Id, new List<CurveLoop> { laco });
    L(0, 0, T, 0, gGrossa); L(T, 0, T, Pr, gGrossa); L(T, Pr, 0, Pr, gGrossa); L(0, Pr, 0, 0, gGrossa);
    Tx(T / 2, Pr * 0.62, "LOTE " + lote, txt, 0); if (quadra != "") Tx(T / 2, Pr * 0.62 - Math.Min(3.5 * escala / 250.0, Pr * 0.17), "QUADRA " + quadra, txtPeq, 0);   // lote pequeno: quadra mais perto do lote, sem encostar na testada
    Tx(T / 2, 1.8 * escala / 250.0, Mf(T), txtPeq, 0);                         // testada
    Tx(T - 1.6 * escala / 250.0, Pr * 0.3, Mf(Pr), txtPeq, Math.PI / 2);       // profundidade

    // lotes vizinhos
    for (int i = 1; i <= nEsq; i++) { double a = -i * T; L(a, 0, a, Pr, gFina); Tx(a + T / 2, Pr * 0.62, "LOTE " + (i <= vizEsq.Count ? vizEsq[i - 1] : "—"), txtPeq, 0); Tx(a + T / 2, 1.8 * escala / 250.0, Mf(T), txtPeq, 0); }
    for (int i = 1; i <= nDir; i++) { double a = T + i * T; L(a, 0, a, Pr, gFina); Tx(a - T / 2, Pr * 0.62, "LOTE " + (i <= vizDir.Count ? vizDir[i - 1] : "—"), txtPeq, 0); Tx(a - T / 2, 1.8 * escala / 250.0, Mf(T), txtPeq, 0); }

    // quadra: alinhamento da frente e fundo dos lotes até a esquina (ou até o último vizinho)
    double qIni = esquinaDireita || double.IsNaN(xEsquina) ? xEsqLotes : xEsquina, qFim = !esquinaDireita || double.IsNaN(xEsquina) ? xDirLotes : xEsquina;
    L(qIni, 0, qFim, 0, gMedia); L(qIni, Pr, qFim, Pr, gFina);
    if (!double.IsNaN(xEsquina)) {
        L(xEsquina, 0, xEsquina, Pr, gMedia);   // divisa da quadra na esquina
        // rua da esquina
        double xr = esquinaDireita ? xEsquina + larguraRua : xEsquina - larguraRua;
        L(xEsquina, -larguraRua, xEsquina, 0, gFina); L(xr, -larguraRua - 4, xr, Pr + 6, gFina);
        L(xEsquina, Pr, xEsquina, Pr + 6, gFina);
        Tx((xEsquina + xr) / 2, Pr / 2, ruaEsquina, txtRua, Math.PI / 2);
        // distância até a esquina (linha de cota simples atrás da quadra)
        double xa = esquinaDireita ? T : 0, yc = Pr + 3.5 * escala / 250.0;
        L(xa, yc, xEsquina, yc, gFina);
        foreach (var xx in new[] { xa, xEsquina }) L(xx - 0.6 * escala / 250.0, yc - 0.6 * escala / 250.0, xx + 0.6 * escala / 250.0, yc + 0.6 * escala / 250.0, gFina);
        Tx((xa + xEsquina) / 2, yc + 1.6 * escala / 250.0, Mf(distEsquina) + " ATÉ A ESQUINA", txtPeq, 0);
    }
    // rua da frente
    double r0 = Math.Min(qIni, double.IsNaN(xEsquina) ? qIni : Math.Min(xEsquina, xEsquina - larguraRua)) - 4, r1 = Math.Max(qFim, double.IsNaN(xEsquina) ? qFim : xEsquina + larguraRua) + 4;
    L(r0, -larguraRua, r1, -larguraRua, gFina);
    Tx(T / 2, -larguraRua / 2, ruaFrente, txtRua, 0);

    // norte: o símbolo aponta para cima; gira para o norte real. Embaixo do desenho fica a frente, voltada para "cardeal".
    var rumos = new Dictionary<string, double> { { "N", 0 }, { "NL", 45 }, { "NE", 45 }, { "L", 90 }, { "SL", 135 }, { "SE", 135 }, { "S", 180 }, { "SO", 225 }, { "O", 270 }, { "NO", 315 } };
    double rumo;
    if (simboloNorte != null && rumos.TryGetValue(cardeal, out rumo)) {
        if (!simboloNorte.IsActive) simboloNorte.Activate();
        var pN = new XYZ(F(xNorte), F(yNorte), 0);
        var norte = document.Create.NewFamilyInstance(pN, simboloNorte, vista);
        double giro = (rumo + 180) % 360;
        if (giro != 0) ElementTransformUtils.RotateElement(document, norte.Id, Line.CreateBound(pN, pN + XYZ.BasisZ), giro * Math.PI / 180);
        log.AppendLine("Norte girado " + giro + "° (frente voltada para " + cardeal + ").");
    } else log.AppendLine("⚠ Norte não colocado: " + (simboloNorte == null ? "símbolo de norte não encontrado no template" : "informe o ponto cardeal da frente (N, NE, L, SE, S, SO, O, NO)"));
    t.Commit();
    log.AppendLine("Vista '" + nomeVista + "' criada em 1:" + escala + " — desenho " + Math.Round((xMax - xMin) * 1000 / escala) + " × " + Math.Round((yMax - yMin) * 1000 / escala) + " mm.");
}
return log.ToString();
