// 02_desenhar_lote.cs — Fases 2 e 5: desenha o lote nas vistas de implantação quando o modelo não tem linhas de propriedade.
// As vistas de implantação do template escondem paredes, pisos e telhados: no projeto de referência o arquiteto desenhou
// as linhas de divisa, as cotas e os textos à mão. Sem linhas de propriedade (a API do Revit não cria linhas de
// propriedade), a vista sai vazia. Este script desenha, em cada vista pedida:
//   - o contorno do lote (das linhas de propriedade, se houver; senão, dos muros de divisa: paredes de até 3 m de altura
//     no perímetro; senão, do retângulo das paredes);
//   - a projeção da edificação (face externa das paredes da casa, as que passam de 3 m);
//   - cotas de testada e profundidade do lote;
//   - recorte da vista no lote + 2,5 m (sem recorte, câmeras e marcadores do template deixam a vista enorme).
// Parâmetros:
//   [0] vistas separadas por ";" (vazio = todas as plantas cujo nome contém "IMPLANTA")
//   [1] "refazer" para apagar o que o script desenhou antes nessas vistas (marcado no comentário "SKILL LOTE")
// Uma transação "Skill: desenhar lote". Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
var log = new System.Text.StringBuilder();
var nomes = P(0) == "" ? new List<string>() : P(0).Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var vistas = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v => !v.IsTemplate && (nomes.Count > 0 ? nomes.Contains(v.Name) : v.Name.ToUpper().Contains("IMPLANTA"))).ToList();
if (vistas.Count == 0) return "ERRO: nenhuma vista encontrada (" + P(0) + ").";
// --- contorno do lote
double x0, y0, x1, y1; string origem;
var props = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements();
var paredes = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.get_BoundingBox(null) != null).ToList();
Func<Wall, double> Alt = w => { var b = w.get_BoundingBox(null); return (b.Max.Z - b.Min.Z) * 0.3048; };
var muros = paredes.Where(w => Alt(w) <= 3.0).ToList(); var casa = paredes.Where(w => Alt(w) > 3.0).ToList();
if (props.Count > 0) {
    var bbs = props.Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
    x0 = bbs.Min(b => b.Min.X); y0 = bbs.Min(b => b.Min.Y); x1 = bbs.Max(b => b.Max.X); y1 = bbs.Max(b => b.Max.Y); origem = "linhas de propriedade";
} else if (muros.Count >= 2) {
    var bbs = muros.Select(w => w.get_BoundingBox(null)).ToList();
    x0 = bbs.Min(b => b.Min.X); y0 = bbs.Min(b => b.Min.Y); x1 = bbs.Max(b => b.Max.X); y1 = bbs.Max(b => b.Max.Y); origem = "muros de divisa";
    // lado aberto (sem muro, ex.: frente): estende até a face externa dos muros vizinhos — já coberto pela caixa
} else {
    var bbs = paredes.Select(w => w.get_BoundingBox(null)).ToList();
    x0 = bbs.Min(b => b.Min.X); y0 = bbs.Min(b => b.Min.Y); x1 = bbs.Max(b => b.Max.X); y1 = bbs.Max(b => b.Max.Y); origem = "retângulo das paredes (confira!)";
}
// a calçada (ambiente CALÇADA) fica fora do lote: se a caixa a incluir, corta pelo lado dela
var calc = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().FirstOrDefault(r => r.Area > 0 && (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper().Contains("CALÇADA"));
if (calc != null) { var cb = calc.get_BoundingBox(null); if (cb.Max.Y <= y0 + F(0.2) && cb.Max.Y > y0 - F(0.2)) y0 = cb.Max.Y; if (cb.Min.Y >= y1 - F(0.2) && cb.Min.Y < y1 + F(0.2)) y1 = cb.Min.Y; if (cb.Max.X <= x0 + F(0.2) && cb.Max.X > x0 - F(0.2)) x0 = cb.Max.X; if (cb.Min.X >= x1 - F(0.2) && cb.Min.X < x1 + F(0.2)) x1 = cb.Min.X; }
log.AppendLine("Lote (" + origem + "): " + Math.Round((x1 - x0) * 0.3048, 2) + " × " + Math.Round((y1 - y0) * 0.3048, 2) + " m");
// --- projeção da casa
double cx0 = 0, cy0 = 0, cx1 = 0, cy1 = 0; bool temCasa = casa.Count > 0;
if (temCasa) { var bbs = casa.Select(w => w.get_BoundingBox(null)).ToList(); cx0 = bbs.Min(b => b.Min.X); cy0 = bbs.Min(b => b.Min.Y); cx1 = bbs.Max(b => b.Max.X); cy1 = bbs.Max(b => b.Max.Y); }
var estilos = new FilteredElementCollector(document).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().Where(g => g.GraphicsStyleCategory != null && g.GraphicsStyleCategory.Parent != null && g.GraphicsStyleCategory.Parent.Id.Value == (long)BuiltInCategory.OST_Lines).ToList();
var estLote = estilos.FirstOrDefault(g => g.Name.ToUpper().Contains("DIVISA")) ?? estilos.FirstOrDefault(g => g.Name.ToUpper().Contains("LARGA") || g.Name.ToUpper().Contains("WIDE"));
var estCasa = estilos.FirstOrDefault(g => g.Name.ToUpper().Contains("PROJE")) ?? estilos.FirstOrDefault(g => g.Name.ToUpper().Contains("MÉDIA") || g.Name.ToUpper().Contains("MEDIUM"));
var tiposCota = new FilteredElementCollector(document).OfClass(typeof(DimensionType)).Cast<DimensionType>().Where(d => d.StyleType == DimensionStyleType.Linear).ToList();
using (var t = new Transaction(document, "Skill: desenhar lote")) {
    t.Start();
    foreach (var v in vistas) {
        var antigos = new FilteredElementCollector(document, v.Id).WhereElementIsNotElementType().Where(e => (e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "") == "SKILL LOTE").Select(e => e.Id).ToList();
        if (antigos.Count > 0) { if (P(1).ToLower() == "refazer") document.Delete(antigos); else { log.AppendLine(v.Name + ": já desenhado (use \"refazer\")"); continue; } }
        double z = v.GenLevel != null ? v.GenLevel.Elevation : 0;
        var tipoCota = tiposCota.FirstOrDefault(d => d.Name.Replace("/", "-").EndsWith("1-" + v.Scale)) ?? tiposCota.OrderByDescending(d => d.Name.ToUpper().Contains("VZ")).FirstOrDefault();
        Func<double, double, double, double, GraphicsStyle, DetailCurve> Linha = (ax, ay, bx, by, est) => {
            var dc = document.Create.NewDetailCurve(v, Line.CreateBound(new XYZ(ax, ay, z), new XYZ(bx, by, z)));
            if (est != null) dc.LineStyle = est; dc.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("SKILL LOTE"); return dc;
        };
        var frente = Linha(x0, y0, x1, y0, estLote); var dir = Linha(x1, y0, x1, y1, estLote); var fundo = Linha(x1, y1, x0, y1, estLote); var esq = Linha(x0, y1, x0, y0, estLote);
        if (temCasa) { Linha(cx0, cy0, cx1, cy0, estCasa); Linha(cx1, cy0, cx1, cy1, estCasa); Linha(cx1, cy1, cx0, cy1, estCasa); Linha(cx0, cy1, cx0, cy0, estCasa); }
        int cotas = 0;
        try {
            var ra = new ReferenceArray(); ra.Append(esq.GeometryCurve.Reference); ra.Append(dir.GeometryCurve.Reference);
            var d1 = tipoCota != null ? document.Create.NewDimension(v, Line.CreateBound(new XYZ(x0, y1 + F(1.0), z), new XYZ(x1, y1 + F(1.0), z)), ra, tipoCota) : document.Create.NewDimension(v, Line.CreateBound(new XYZ(x0, y1 + F(1.0), z), new XYZ(x1, y1 + F(1.0), z)), ra);
            d1.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("SKILL LOTE"); cotas++;
            var rb = new ReferenceArray(); rb.Append(frente.GeometryCurve.Reference); rb.Append(fundo.GeometryCurve.Reference);
            var d2 = tipoCota != null ? document.Create.NewDimension(v, Line.CreateBound(new XYZ(x0 - F(1.0), y0, z), new XYZ(x0 - F(1.0), y1, z)), rb, tipoCota) : document.Create.NewDimension(v, Line.CreateBound(new XYZ(x0 - F(1.0), y0, z), new XYZ(x0 - F(1.0), y1, z)), rb);
            d2.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set("SKILL LOTE"); cotas++;
        } catch (Exception ex) { log.AppendLine("  cotas: " + ex.Message); }
        if (!v.CropBoxActive) {   // sem recorte, a vista pega câmeras e marcadores espalhados no modelo e fica enorme na folha
            var cbv = v.CropBox; var tr = cbv.Transform; var invT = tr.Inverse; double mg = F(2.5);
            var c0 = invT.OfPoint(new XYZ(x0 - mg, y0 - mg, z)); var c1 = invT.OfPoint(new XYZ(x1 + mg, y1 + mg, z));
            cbv.Min = new XYZ(Math.Min(c0.X, c1.X), Math.Min(c0.Y, c1.Y), cbv.Min.Z); cbv.Max = new XYZ(Math.Max(c0.X, c1.X), Math.Max(c0.Y, c1.Y), cbv.Max.Z);
            v.CropBox = cbv; v.CropBoxActive = true; v.CropBoxVisible = false;
        }
        log.AppendLine(v.Name + ": lote" + (temCasa ? " + projeção da edificação" : "") + " + " + cotas + " cotas + recorte no lote");
    }
    t.Commit();
}
return log.ToString();
