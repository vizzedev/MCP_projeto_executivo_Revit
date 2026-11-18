// SKILL EXECUTIVO REVIT — Fases 2 e 6: cotas gerais do lote em TODOS os lados (frente, laterais e fundos)
// send_code_to_revit · transactionMode "none"
// Regra do escritório: cota tradicional de cada lado do lote, por fora da divisa e próxima dos muros —
//   0,40 m da divisa em vistas até 1:50; 0,50 m em 1:100 ou maior. Tipo de cota da escala da vista ("VZ-Cota 1-50",
//   "VZ-Cota Executivo 1-100"…). Vale para as plantas de layout e para a implantação (layout e executiva).
// parameters[0] = vistas separadas por ";" (vazio = todas as plantas com "LAYOUT" ou "IMPLANTA" no nome)
// parameters[1] = "refazer" apaga antes as cotas de lote que a skill fez nessas vistas (mesma linha da divisa, por fora)
// Lote: linha de propriedade cujo comentário não tem "RECUO" (a maior, se houver mais de uma). Lote retangular
//   (caixa da linha); lote irregular → avise e cote à mão.
// As referências são linhas de detalhe invisíveis sobre cada lado do lote. Uma transação "Skill: cotas do lote". Não salva.
// --- projeto pelo caminho (regra 5): se o 1º parâmetro for "DOC:<caminho do .rvt>", trabalha nesse arquivo aberto.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
Func<int, string> P = i => parameters != null && parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
document.Application.FailuresProcessing += __avisos;
try {
    var props = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).OfClass(typeof(PropertyLine)).ToElements()
        .Where(e => !((e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "").ToUpper().Contains("RECUO"))).ToList();
    if (props.Count == 0) return "ERRO: sem linha de propriedade do lote (rode 02_desenhar_lote.cs ou desenhe a divisa).";
    var lote = props.OrderByDescending(e => e.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0).First();
    var bb = lote.get_BoundingBox(null); double x0 = bb.Min.X, y0 = bb.Min.Y, x1 = bb.Max.X, y1 = bb.Max.Y;
    double aLote = (lote.get_Parameter(BuiltInParameter.PROPERTY_AREA)?.AsDouble() ?? 0), aCaixa = (x1 - x0) * (y1 - y0);
    if (aLote > 0 && Math.Abs(aLote - aCaixa) / aCaixa > 0.01) log.AppendLine("⚠ lote não retangular (área " + (aLote * 0.092903).ToString("0.00") + " × caixa " + (aCaixa * 0.092903).ToString("0.00") + " m²) — confira e cote os lados inclinados à mão");
    var nomes = P(0) == "" ? new List<string>() : P(0).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
    var vistas = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v => !v.IsTemplate &&
        (nomes.Count > 0 ? nomes.Contains(v.Name) : (v.Name.ToUpper().Contains("LAYOUT") || v.Name.ToUpper().Contains("IMPLANTA")))).ToList();
    if (vistas.Count == 0) return "ERRO: nenhuma vista encontrada.";
    var tiposL = new FilteredElementCollector(document).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(d => d.StyleType == DimensionStyleType.Linear && d.Name.StartsWith("VZ") && !d.Name.ToUpper().Contains("INTERNA") && !d.Name.ToUpper().Contains("AZUL")).ToList();
    Func<DimensionType, int> Esc = d => { var m = System.Text.RegularExpressions.Regex.Match(d.Name.Replace("/", "-"), @"1-(\d+)\s*$"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; };
    var invis = new FilteredElementCollector(document).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().FirstOrDefault(g => g.Name == "<Linhas invisíveis>" || g.Name == "<Invisible lines>");
    using (var t = new Transaction(document, "Skill: cotas do lote")) {
        t.Start();
        foreach (var v in vistas) {
            double z = v.GenLevel != null ? v.GenLevel.Elevation : 0;
            double off = F(v.Scale <= 50 ? 0.40 : 0.50), tol = F(0.02);
            // cotas de lote já existentes: paralelas a um lado, por fora, a até 2 m, com o comprimento do lado
            var lados = new[] { Tuple.Create(true, y1, x1 - x0, +1.0), Tuple.Create(true, y0, x1 - x0, -1.0), Tuple.Create(false, x1, y1 - y0, +1.0), Tuple.Create(false, x0, y1 - y0, -1.0) };
            var existentes = new FilteredElementCollector(document, v.Id).OfClass(typeof(Dimension)).Cast<Dimension>().Where(d => !(d is SpotDimension) && d.NumberOfSegments == 0 && d.Value.HasValue && d.Curve is Line).ToList();
            var minhas = existentes.Where(d => { var dir = ((Line)d.Curve).Direction; var o = d.Origin;
                return lados.Any(l => l.Item1 ? (Math.Abs(dir.Y) < 0.01 && Math.Abs(d.Value.Value - l.Item3) < tol && (o.Y - l.Item2) * l.Item4 > 0 && Math.Abs(o.Y - l.Item2) < F(2.0))
                                             : (Math.Abs(dir.X) < 0.01 && Math.Abs(d.Value.Value - l.Item3) < tol && (o.X - l.Item2) * l.Item4 > 0 && Math.Abs(o.X - l.Item2) < F(2.0))); }).ToList();
            if (minhas.Count > 0) {
                if (P(1).ToLower() != "refazer") { log.AppendLine(v.Name + ": já tem " + minhas.Count + " cota(s) de lote — use \"refazer\""); continue; }
                document.Delete(minhas.Select(d => d.Id).ToList());
            }
            var tipo = tiposL.Where(d => Esc(d) > 0).OrderBy(d => Math.Abs(Esc(d) - v.Scale)).ThenByDescending(d => d.Name.ToUpper().Contains("EXECUTIVO") ? 0 : 1).FirstOrDefault();
            // linhas invisíveis sobre os quatro lados (referências)
            Func<double, double, double, double, DetailCurve> L = (ax, ay, bx, by) => { var dc = document.Create.NewDetailCurve(v, Line.CreateBound(new XYZ(ax, ay, z), new XYZ(bx, by, z))); if (invis != null) dc.LineStyle = invis; return dc; };
            var ladoFrenteY0 = L(x0, y0, x1, y0); var ladoY1 = L(x0, y1, x1, y1); var ladoX0 = L(x0, y0, x0, y1); var ladoX1 = L(x1, y0, x1, y1);
            Action<DetailCurve, DetailCurve, Line> Cota = (a, b, linha) => { var ra = new ReferenceArray(); ra.Append(a.GeometryCurve.Reference); ra.Append(b.GeometryCurve.Reference); if (tipo != null) document.Create.NewDimension(v, linha, ra, tipo); else document.Create.NewDimension(v, linha, ra); };
            Cota(ladoX0, ladoX1, Line.CreateBound(new XYZ(x0, y1 + off, z), new XYZ(x1, y1 + off, z)));   // lado em y1
            Cota(ladoX0, ladoX1, Line.CreateBound(new XYZ(x0, y0 - off, z), new XYZ(x1, y0 - off, z)));   // lado em y0
            Cota(ladoFrenteY0, ladoY1, Line.CreateBound(new XYZ(x0 - off, y0, z), new XYZ(x0 - off, y1, z))); // lado em x0
            Cota(ladoFrenteY0, ladoY1, Line.CreateBound(new XYZ(x1 + off, y0, z), new XYZ(x1 + off, y1, z))); // lado em x1
            // recorte: garante que as 4 cotas cabem na vista
            if (v.CropBoxActive) {
                var cb = v.CropBox; var tr = cb.Transform; var inv = tr.Inverse; double mg = off + F(0.5);
                var a = inv.OfPoint(new XYZ(x0 - mg, y0 - mg, z)); var b = inv.OfPoint(new XYZ(x1 + mg, y1 + mg, z));
                cb.Min = new XYZ(Math.Min(cb.Min.X, Math.Min(a.X, b.X)), Math.Min(cb.Min.Y, Math.Min(a.Y, b.Y)), cb.Min.Z);
                cb.Max = new XYZ(Math.Max(cb.Max.X, Math.Max(a.X, b.X)), Math.Max(cb.Max.Y, Math.Max(a.Y, b.Y)), cb.Max.Z); v.CropBox = cb;
            }
            log.AppendLine(v.Name + ": 4 cotas do lote (" + ((x1 - x0) * 0.3048).ToString("0.00") + " × " + ((y1 - y0) * 0.3048).ToString("0.00") + " m) a " + (off * 0.3048).ToString("0.00") + " m da divisa, tipo " + (tipo == null ? "padrão" : tipo.Name) + (minhas.Count > 0 ? " (refeitas)" : ""));
        }
        t.Commit();
    }
    log.AppendLine("Depois: reposicione as vistas na folha (os recortes podem ter crescido) e confira na imagem exportada.");
    return log.ToString();
} finally { document.Application.FailuresProcessing -= __avisos; }
