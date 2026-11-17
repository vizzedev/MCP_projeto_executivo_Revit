// SKILL EXECUTIVO REVIT — Fase 6: anotações da implantação e linhas de recuo (checklist executivo, etapa 1)
// send_code_to_revit · transactionMode "none"
// Escreve na implantação, a partir dos dados do questionário de abertura:
//   nome da rua + largura + pavimentação ("RUA DAS FLORES" / "LARGURA 12,00 m · RUA ASFALTADA") no leito da rua, em frente ao lote;
//   números dos lotes vizinhos ("LOTE 11") fora de cada divisa; numeração predial ("Nº 123") junto à frente; símbolo de norte
//   (família de anotação com "NORTE" no nome) no canto superior direito, se ainda não houver.
// Desenha as linhas de recuo (frente, laterais, fundo) em azul tracejado, estilo de linha "VZ-RECUO" (criado se não existir),
// na implantação e nas plantas listadas em "vistas_recuo".
// parameters[0] = dados "chave=valor" separados por ";":
//   rua=RUA DAS FLORES; largura=12; asfalto=sim|nao; numero=123; lote_esq=11; lote_dir=13; lote_fundo=25;
//   recuo_frente=4; recuo_lateral=1.5; recuo_esq=; recuo_dir=; recuo_fundo=2;
//   vista=IMPLANTAÇÃO (padrão); vistas_recuo=IMPLANTAÇÃO|PAVIMENTO TERREO EXECUTIVO (padrão: implantação + executivo do térreo)
//   (esquerda/direita olhando o lote da rua; recuo vazio ou 0 = sem linha naquele lado)
// parameters[1] = "refazer" apaga antes os textos e as linhas de recuo que a skill colocou nessas vistas
// Lote: linhas de propriedade; sem elas, as paredes de até 3 m (muros). Frente = lado mais próximo do ambiente CALÇADA.
// Uma transação "Skill: implantação" (Ctrl+Z desfaz). Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var dados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var par in P(0).Split(';')) { var k = par.IndexOf('='); if (k > 0) dados[par.Substring(0, k).Trim()] = par.Substring(k + 1).Trim(); }
Func<string, string> D = k => dados.ContainsKey(k) ? dados[k] : "";
Func<string, double> N = k => { double v; return double.TryParse(D(k).Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0; };
bool refazer = P(1).ToLower() == "refazer";
var log = new System.Text.StringBuilder();
var cult = new System.Globalization.CultureInfo("pt-BR");
var planos = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v => !v.IsTemplate).ToList();
var nomeImp = D("vista") != "" ? D("vista") : "IMPLANTAÇÃO";
var vImp = planos.FirstOrDefault(v => v.Name == nomeImp);
if (vImp == null) return "ERRO: vista \"" + nomeImp + "\" não encontrada.";
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
Func<Autodesk.Revit.DB.Architecture.Room, string> NomeAmb = r => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper();

// --- lote (retângulo alinhado aos eixos)
var props = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
var caixas = props;
string origemLote = "linhas de propriedade";
if (caixas.Count == 0) {
    caixas = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Select(w => w.get_BoundingBox(null)).Where(b => b != null && b.Max.Z - b.Min.Z <= F(3.0)).ToList();
    origemLote = "muros (paredes até 3 m)";
}
if (caixas.Count == 0) return "ERRO: sem linhas de propriedade nem muros para achar o lote.";
double x0 = caixas.Min(b => b.Min.X), x1 = caixas.Max(b => b.Max.X), y0 = caixas.Min(b => b.Min.Y), y1 = caixas.Max(b => b.Max.Y);
// frente: lado do lote mais próximo do centro da calçada
var calc = rooms.FirstOrDefault(r => NomeAmb(r).Contains("CALÇADA") || NomeAmb(r).Contains("CALCADA"));
string frente = "y0";
if (calc != null) { var bc = calc.get_BoundingBox(null); var c = (bc.Min + bc.Max) / 2;
    var dist = new Dictionary<string, double> { { "y0", Math.Abs(c.Y - y0) }, { "y1", Math.Abs(c.Y - y1) }, { "x0", Math.Abs(c.X - x0) }, { "x1", Math.Abs(c.X - x1) } };
    frente = dist.OrderBy(kv => kv.Value).First().Key; }
else log.AppendLine("⚠ sem ambiente CALÇADA: frente assumida no lado de menor Y");
// referencial: u ao longo da frente (esquerda → direita olhando da rua), v para dentro do lote; Pt(u, v) → ponto do modelo
double W = frente == "y0" || frente == "y1" ? x1 - x0 : y1 - y0, Dp = frente == "y0" || frente == "y1" ? y1 - y0 : x1 - x0;
Func<double, double, XYZ> Pt = (u, v) => {
    switch (frente) {
        case "y0": return new XYZ(x0 + u, y0 + v, 0);   // da rua (−Y) olhando para +Y: a esquerda é −X
        case "y1": return new XYZ(x1 - u, y1 - v, 0);   // da rua (+Y) olhando para −Y: a esquerda é +X
        case "x0": return new XYZ(x0 + v, y1 - u, 0);   // da rua (−X) olhando para +X: a esquerda é +Y
        default:   return new XYZ(x1 - v, y0 + u, 0);   // da rua (+X) olhando para −X: a esquerda é −Y
    } };
// largura da calçada (para pôr a rua além dela)
double cw = F(2.0);
if (calc != null) { var bc = calc.get_BoundingBox(null); cw = frente == "y0" ? y0 - bc.Min.Y : frente == "y1" ? bc.Max.Y - y1 : frente == "x0" ? x0 - bc.Min.X : bc.Max.X - x1; if (cw < F(0.5)) cw = F(2.0); }

var tiposT = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
Func<TextNoteType, double> Tam = tt => (tt.get_Parameter(BuiltInParameter.TEXT_SIZE)?.AsDouble() ?? 0) * 304.8;
Func<TextNoteType, bool> Vermelho = tt => { int c = tt.get_Parameter(BuiltInParameter.LINE_COLOR)?.AsInteger() ?? 0; return (c & 255) > 120 && ((c >> 8) & 255) < 60 && ((c >> 16) & 255) < 60; };
var tNota = tiposT.Where(tt => !Vermelho(tt) && Tam(tt) >= 1.5).OrderBy(tt => Math.Abs(Tam(tt) - 1.8)).FirstOrDefault() ?? tiposT.First();
var tRua = tiposT.Where(tt => !Vermelho(tt)).OrderBy(tt => Math.Abs(Tam(tt) - 3.0)).FirstOrDefault() ?? tNota;
string rua = D("rua").ToUpper();
string linhaRua2 = (N("largura") > 0 ? "LARGURA " + N("largura").ToString("0.00", cult) + " m" : "") +
    (D("asfalto") != "" ? (N("largura") > 0 ? " · " : "") + (D("asfalto").ToLower().StartsWith("s") ? "RUA ASFALTADA" : "RUA SEM PAVIMENTAÇÃO") : "");
var nossosTextos = new List<string>();
if (rua != "") nossosTextos.Add(rua); if (linhaRua2 != "") nossosTextos.Add(linhaRua2);
foreach (var k in new[] { "lote_esq", "lote_dir", "lote_fundo" }) if (D(k) != "") nossosTextos.Add("LOTE " + D(k).ToUpper());
if (D("numero") != "") nossosTextos.Add("Nº " + D("numero"));
var avisos = new List<string>();

// --- estilo de linha de recuo: azul, tracejado
using (var t = new Transaction(document, "Skill: implantação")) {
    t.Start();
    var catLinhas = document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
    Category sub = null; foreach (Category cs in catLinhas.SubCategories) if (cs.Name.ToUpper().Contains("RECUO")) { sub = cs; break; }
    if (sub == null) {
        sub = document.Settings.Categories.NewSubcategory(catLinhas, "VZ-RECUO");
        sub.LineColor = new Color(0, 0, 255); sub.SetLineWeight(1, GraphicsStyleType.Projection);
        var padrao = new FilteredElementCollector(document).OfClass(typeof(LinePatternElement)).Cast<LinePatternElement>()
            .OrderBy(lp => { var nn = lp.Name.ToUpper(); return nn.Contains("TRACEJ") ? 0 : nn.Contains("DASH") || nn.Contains("OCULT") || nn.Contains("HIDDEN") ? 1 : 9; }).FirstOrDefault();
        if (padrao != null) sub.SetLinePatternId(padrao.Id, GraphicsStyleType.Projection);
        log.AppendLine("estilo de linha \"VZ-RECUO\" criado (azul" + (padrao != null ? ", " + padrao.Name : "") + ")");
    }
    var estilo = sub.GetGraphicsStyle(GraphicsStyleType.Projection);

    // vistas que recebem as linhas de recuo
    var nomesRecuo = D("vistas_recuo") != "" ? D("vistas_recuo").Split('|').Select(x => x.Trim()).ToList()
        : new List<string> { nomeImp }.Concat(planos.Where(v => v.Name.ToUpper().Contains("EXECUTIVO") && v.GenLevel != null && v.Name.ToUpper().Contains("TERREO")).Select(v => v.Name).Take(1)).ToList();
    var vistasRecuo = nomesRecuo.Select(nm => planos.FirstOrDefault(v => v.Name == nm)).Where(v => v != null).ToList();
    foreach (var nm in nomesRecuo.Where(nm => !planos.Any(v => v.Name == nm))) avisos.Add("vista de recuo \"" + nm + "\" não encontrada");

    if (refazer) {
        var apagar = new List<ElementId>();
        apagar.AddRange(new FilteredElementCollector(document, vImp.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Where(tn => nossosTextos.Contains(tn.Text.Trim()) || tn.Text.StartsWith("LOTE ") || tn.Text.StartsWith("Nº ")).Select(tn => tn.Id));
        foreach (var v in vistasRecuo) apagar.AddRange(new FilteredElementCollector(document, v.Id).OfClass(typeof(CurveElement)).Cast<CurveElement>().Where(ce => { try { return ce.LineStyle != null && ce.LineStyle.Id == estilo.Id; } catch { return false; } }).Select(ce => ce.Id));
        if (apagar.Count > 0) document.Delete(apagar.Distinct().ToList());
        log.AppendLine("refazer: " + apagar.Count + " elementos anteriores apagados");
    } else if (new FilteredElementCollector(document, vImp.Id).OfClass(typeof(TextNote)).Cast<TextNote>().Any(tn => nossosTextos.Contains(tn.Text.Trim()))) {
        t.RollBack(); return "A implantação já tem as anotações da skill — use \"refazer\" para gerar de novo.";
    }

    double esc = vImp.Scale; Func<double, double> Pap = mm => F(mm / 1000.0 * esc);
    double z = vImp.GenLevel != null ? vImp.GenLevel.Elevation : 0;
    Func<XYZ, XYZ> Z = p => new XYZ(p.X, p.Y, z);
    var optC = new TextNoteOptions(tNota.Id) { HorizontalAlignment = HorizontalTextAlignment.Center };
    var optR = new TextNoteOptions(tRua.Id) { HorizontalAlignment = HorizontalTextAlignment.Center };
    // direção da frente: rotação do texto da rua ao longo da testada (frente em X: texto horizontal)
    double ang = (frente == "x0" || frente == "x1") ? Math.PI / 2 : 0;
    int n = 0;
    Action<string, XYZ, TextNoteOptions, double> Texto = (txt, p, op, a) => { try { var o = new TextNoteOptions(op.TypeId) { HorizontalAlignment = op.HorizontalAlignment, Rotation = a }; TextNote.Create(document, vImp.Id, Z(p), txt, o); n++; } catch (Exception ex) { avisos.Add("\"" + txt + "\": " + ex.Message); } };
    // rua: no leito, 3 m além da calçada
    if (rua != "") Texto(rua, Pt(W / 2, -cw - F(3.0)), optR, ang); else avisos.Add("sem nome da rua (rua=…)");
    if (linhaRua2 != "") Texto(linhaRua2, Pt(W / 2, -cw - F(3.0) - Pap(6)), optC, ang); else avisos.Add("sem largura/pavimentação da rua (largura=…; asfalto=…)");
    // lotes vizinhos: 2 m fora de cada divisa, no meio da profundidade
    if (D("lote_esq") != "") Texto("LOTE " + D("lote_esq").ToUpper(), Pt(-F(2.0), Dp / 2), optC, 0);
    if (D("lote_dir") != "") Texto("LOTE " + D("lote_dir").ToUpper(), Pt(W + F(2.0), Dp / 2), optC, 0);
    if (D("lote_fundo") != "") Texto("LOTE " + D("lote_fundo").ToUpper(), Pt(W / 2, Dp + F(2.0)), optC, 0);
    if (D("lote_esq") == "" && D("lote_dir") == "") avisos.Add("sem números dos lotes vizinhos (lote_esq=…; lote_dir=…)");
    // numeração predial: junto à frente, dentro do lote
    if (D("numero") != "") Texto("Nº " + D("numero"), Pt(W / 2, F(1.0)), optC, 0); else avisos.Add("sem numeração predial (numero=…)");
    // norte: família de anotação com NORTE no nome, no canto superior direito do lote
    var simbNorte = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_GenericAnnotation).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().FirstOrDefault(fs => (fs.FamilyName + " " + fs.Name).ToUpper().Contains("NORTE"));
    bool jaNorte = new FilteredElementCollector(document, vImp.Id).OfCategory(BuiltInCategory.OST_GenericAnnotation).WhereElementIsNotElementType().ToElements().Any(e => { var fs = document.GetElement(e.GetTypeId()) as FamilySymbol; return fs != null && (fs.FamilyName + " " + fs.Name).ToUpper().Contains("NORTE"); });
    if (!jaNorte) {
        if (simbNorte == null) avisos.Add("sem família de anotação com \"NORTE\" no nome: norte não colocado");
        else { if (!simbNorte.IsActive) simbNorte.Activate(); try { document.Create.NewFamilyInstance(new XYZ(x1 + Pap(25), y1 + Pap(15), z), simbNorte, vImp); n++; log.AppendLine("norte colocado (confira a rotação: norte verdadeiro × norte do projeto)"); } catch (Exception ex) { avisos.Add("norte: " + ex.Message); } }
    }
    // linhas de recuo
    double rf = N("recuo_frente"), rfu = N("recuo_fundo"), rl = N("recuo_lateral"), re = N("recuo_esq") > 0 ? N("recuo_esq") : rl, rd = N("recuo_dir") > 0 ? N("recuo_dir") : rl;
    var linhas = new List<Tuple<string, XYZ, XYZ>>();
    if (rf > 0) linhas.Add(Tuple.Create("frente " + rf.ToString("0.00", cult), Pt(0, F(rf)), Pt(W, F(rf))));
    if (rfu > 0) linhas.Add(Tuple.Create("fundo " + rfu.ToString("0.00", cult), Pt(0, Dp - F(rfu)), Pt(W, Dp - F(rfu))));
    if (re > 0) linhas.Add(Tuple.Create("esquerda " + re.ToString("0.00", cult), Pt(F(re), 0), Pt(F(re), Dp)));
    if (rd > 0) linhas.Add(Tuple.Create("direita " + rd.ToString("0.00", cult), Pt(W - F(rd), 0), Pt(W - F(rd), Dp)));
    if (linhas.Count == 0) avisos.Add("sem recuos informados (recuo_frente=…; recuo_lateral=…; recuo_fundo=…): linhas de recuo não desenhadas");
    int nl = 0;
    foreach (var v in vistasRecuo) { double zv = v.GenLevel != null ? v.GenLevel.Elevation : 0;
        foreach (var ln in linhas) { try { var dc = document.Create.NewDetailCurve(v, Line.CreateBound(new XYZ(ln.Item2.X, ln.Item2.Y, zv), new XYZ(ln.Item3.X, ln.Item3.Y, zv))); dc.LineStyle = estilo; nl++; } catch (Exception ex) { avisos.Add("recuo " + ln.Item1 + " em " + v.Name + ": " + ex.Message); } } }
    t.Commit();
    log.AppendLine("Implantação \"" + nomeImp + "\": lote " + (W * 0.3048).ToString("0.00", cult) + " × " + (Dp * 0.3048).ToString("0.00", cult) + " m (" + origemLote + "), frente " + frente
        + " | " + n + " anotações | " + nl + " linhas de recuo em " + vistasRecuo.Count + " vista(s)" + (linhas.Count > 0 ? " (" + string.Join(", ", linhas.Select(l => l.Item1)) + " m)" : ""));
}
if (avisos.Count > 0) log.AppendLine("Avisos:\n  " + string.Join("\n  ", avisos));
return log.ToString();
