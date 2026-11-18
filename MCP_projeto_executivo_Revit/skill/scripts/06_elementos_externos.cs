// SKILL EXECUTIVO REVIT — Fase 6: elementos externos do executivo (checklist, etapa 1) — PROPOSTA para aprovação
// send_code_to_revit · transactionMode "none"
// Insere as famílias do template numa posição proposta, marcadas com Comentários = "SKILL: proposta":
//   abrigo     — abrigo de medidores / padrão de energia: na divisa da frente, a 0,60 m da lateral oposta à garagem
//   gas        — abrigo/casa de gás: ao lado do abrigo de medidores, na frente
//   captacao   — caixa de captação: a 1,50 m da lateral e da frente (mínimo do checklist), do lado do abrigo
//   lixeira    — na calçada, a 0,50 m da divisa, em frente ao abrigo
//   rebaixo    — rebaixo do meio-fio: no meio-fio, em frente à garagem (profundidade 50 cm é da família)
//   ducha      — ducha externa: a 0,80 m do canto da piscina (só se houver piscina)
//   piso_tatil — faixa de piso tátil (tipo de piso com "TÁTIL") ao longo da calçada, a 0,50 m do meio-fio, 0,25 m de largura
// Famílias procuradas pelo nome (família ou tipo): MEDIDOR/PADRÃO DE ENERGIA/ABRIGO; GÁS; CAPTAÇÃO; LIXEIRA; REBAIXO/MEIO-FIO;
// DUCHA. Família que não estiver carregada no modelo vira aviso (carregue e rode de novo).
// parameters[0] = "propor" (padrão) | "aprovar" (tira a marca de proposta) | "desfazer" (apaga o que ainda está em proposta)
// parameters[1] = itens separados por ";" (padrão: todos)
// parameters[2] = "garagem=esquerda|direita" (olhando o lote da rua; padrão: pelo ambiente GARAGEM)
// Depois de "propor", mostre a implantação ao arquiteto: ele aprova, pede ajuste de posição (mova no Revit) ou "desfazer".
// Uma transação "Skill: elementos externos" (Ctrl+Z desfaz). Não salva.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
Func<double, double> F = m => m / 0.3048;
string acao = P(0) == "" ? "propor" : P(0).ToLower();
var todos = new[] { "abrigo", "gas", "captacao", "lixeira", "rebaixo", "ducha", "piso_tatil" };
var itens = P(1) == "" ? todos.ToList() : P(1).ToLower().Split(';').Select(x => x.Trim()).Where(x => x != "").ToList();
const string MARCA = "SKILL: proposta";
var log = new System.Text.StringBuilder(); var avisos = new List<string>();
Func<Element, bool> EmProposta = e => { var c = e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS); return c != null && (c.AsString() ?? "") == MARCA; };

if (acao == "aprovar" || acao == "desfazer") {
    var marcados = new FilteredElementCollector(document).WhereElementIsNotElementType().ToElements().Where(EmProposta).ToList();
    using (var t = new Transaction(document, "Skill: elementos externos")) { t.Start();
        if (acao == "desfazer") document.Delete(marcados.Select(e => e.Id).ToList());
        else foreach (var e in marcados) e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("");
        t.Commit(); }
    return (acao == "desfazer" ? "Apagados " : "Aprovados ") + marcados.Count + " elementos da proposta.";
}

var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
Func<Autodesk.Revit.DB.Architecture.Room, string> NomeAmb = r => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper();
Func<string, string> SemAc = s => new string((s ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
// nível do térreo: o mais baixo com ambientes
var nivel = rooms.Where(r => r.Level != null).Select(r => r.Level).OrderBy(l => l.Elevation).FirstOrDefault();
if (nivel == null) return "ERRO: nenhum ambiente colocado (o térreo é achado pelos ambientes).";
// lote e frente (mesma regra da implantação)
var caixas = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
if (caixas.Count == 0) caixas = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Select(w => w.get_BoundingBox(null)).Where(b => b != null && b.Max.Z - b.Min.Z <= F(3.0)).ToList();
if (caixas.Count == 0) return "ERRO: sem linhas de propriedade nem muros para achar o lote.";
double x0 = caixas.Min(b => b.Min.X), x1 = caixas.Max(b => b.Max.X), y0 = caixas.Min(b => b.Min.Y), y1 = caixas.Max(b => b.Max.Y);
var calc = rooms.FirstOrDefault(r => SemAc(NomeAmb(r)).Contains("CALCADA"));
string frente = "y0";
if (calc != null) { var bc = calc.get_BoundingBox(null); var cc = (bc.Min + bc.Max) / 2;
    frente = new Dictionary<string, double> { { "y0", Math.Abs(cc.Y - y0) }, { "y1", Math.Abs(cc.Y - y1) }, { "x0", Math.Abs(cc.X - x0) }, { "x1", Math.Abs(cc.X - x1) } }.OrderBy(kv => kv.Value).First().Key; }
else avisos.Add("sem ambiente CALÇADA: frente assumida no lado de menor Y");
double W = frente == "y0" || frente == "y1" ? x1 - x0 : y1 - y0, Dp = frente == "y0" || frente == "y1" ? y1 - y0 : x1 - x0;
double z = nivel.Elevation;
Func<double, double, XYZ> Pt = (u, v) => {
    switch (frente) {
        case "y0": return new XYZ(x0 + u, y0 + v, z);
        case "y1": return new XYZ(x1 - u, y1 - v, z);
        case "x0": return new XYZ(x0 + v, y1 - u, z);
        default:   return new XYZ(x1 - v, y0 + u, z);
    } };
Func<XYZ, double> U = p => frente == "y0" ? p.X - x0 : frente == "y1" ? x1 - p.X : frente == "x0" ? y1 - p.Y : p.Y - y0;
double cw = F(2.0);
if (calc != null) { var bc = calc.get_BoundingBox(null); cw = frente == "y0" ? y0 - bc.Min.Y : frente == "y1" ? bc.Max.Y - y1 : frente == "x0" ? x0 - bc.Min.X : bc.Max.X - x1; if (cw < F(0.5)) cw = F(2.0); }
// lado da garagem (u pequeno = esquerda olhando da rua)
var gar = rooms.FirstOrDefault(r => SemAc(NomeAmb(r)).Contains("GARAGEM"));
string ladoGar = P(2).ToLower().Contains("direita") ? "direita" : P(2).ToLower().Contains("esquerda") ? "esquerda" : "";
double uGar = W / 2;
if (gar != null) { var bg = gar.get_BoundingBox(null); uGar = U((bg.Min + bg.Max) / 2); if (ladoGar == "") ladoGar = uGar < W / 2 ? "esquerda" : "direita"; }
if (ladoGar == "") { ladoGar = "esquerda"; avisos.Add("sem ambiente GARAGEM: garagem assumida à esquerda (informe garagem=…)"); }
else if (gar == null) uGar = ladoGar == "esquerda" ? F(2.0) : W - F(2.0);
// lado do abrigo = oposto à garagem; uA(d) = distância d a partir dessa lateral
Func<double, double> UA = d => ladoGar == "esquerda" ? W - d : d;
// famílias carregadas
var simbolos = new FilteredElementCollector(document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().Where(fs => fs.Category != null && fs.Category.CategoryType == CategoryType.Model).ToList();
Func<string[], string[], FamilySymbol> Familia = (chaves, excluir) => simbolos.FirstOrDefault(fs => { var nn = SemAc(fs.FamilyName + " " + fs.Name); return chaves.Any(k => nn.Contains(k)) && !excluir.Any(k => nn.Contains(k)); });
// ângulo para a família "olhar" para a rua (frente dela voltada para fora do lote)
double angRua = frente == "y0" ? Math.PI : frente == "y1" ? 0 : frente == "x0" ? Math.PI / 2 : -Math.PI / 2;
var criados = new List<string>();
var cult = new System.Globalization.CultureInfo("pt-BR");
using (var t = new Transaction(document, "Skill: elementos externos")) {
    t.Start();
    Action<string, FamilySymbol, XYZ> Colocar = (rot, fs, p) => {
        if (fs == null) { avisos.Add(rot + ": família não encontrada no modelo (carregue a família e rode de novo)"); return; }
        try { if (!fs.IsActive) fs.Activate();
            var fi = document.Create.NewFamilyInstance(p, fs, nivel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            try { ElementTransformUtils.RotateElement(document, fi.Id, Line.CreateBound(p, p + XYZ.BasisZ), angRua); } catch { }
            fi.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(MARCA);
            criados.Add(rot + " → " + fs.FamilyName + " : " + fs.Name + " (u=" + (U(p) * 0.3048).ToString("0.00", cult) + " m da divisa esquerda) [" + fi.Id.Value + "]");
        } catch (Exception ex) { avisos.Add(rot + ": " + ex.Message + " (família hospedada? coloque à mão)"); } };
    if (itens.Contains("abrigo")) Colocar("abrigo de medidores", Familia(new[] { "MEDIDOR", "PADRAO DE ENERGIA", "PADRAO ENERGIA", "ABRIGO" }, new[] { "GAS", "LIXEIRA" }), Pt(UA(F(0.60)), F(0.15)));
    if (itens.Contains("gas")) Colocar("abrigo de gás", Familia(new[] { "GAS" }, new[] { "CHURRASQ", "FOGAO", "COOKTOP", "AQUECEDOR" }), Pt(UA(F(1.90)), F(0.15)));
    if (itens.Contains("captacao")) Colocar("caixa de captação", Familia(new[] { "CAPTACAO" }, new string[0]), Pt(UA(F(1.80)), F(1.80)));
    if (itens.Contains("lixeira")) Colocar("lixeira", Familia(new[] { "LIXEIRA" }, new string[0]), Pt(UA(F(1.00)), -F(0.50)));
    if (itens.Contains("rebaixo")) Colocar("rebaixo do meio-fio", Familia(new[] { "REBAIXO", "MEIO-FIO", "MEIO FIO", "MEIOFIO" }, new string[0]), Pt(uGar, -cw + F(0.25)));
    if (itens.Contains("ducha")) {
        var pisc = rooms.FirstOrDefault(r => SemAc(NomeAmb(r)).Contains("PISCINA"));
        BoundingBoxXYZ bp = pisc != null ? pisc.get_BoundingBox(null) : null;
        if (bp == null) { var ep = new FilteredElementCollector(document).WhereElementIsNotElementType().ToElements().FirstOrDefault(e => { var tp = document.GetElement(e.GetTypeId()) as ElementType; return tp != null && SemAc(((tp as FamilySymbol)?.FamilyName ?? "") + " " + tp.Name).Contains("PISCINA"); }); if (ep != null) bp = ep.get_BoundingBox(null); }
        if (bp == null) avisos.Add("ducha: sem piscina no modelo — não colocada");
        else Colocar("ducha externa", Familia(new[] { "DUCHA" }, new string[0]), new XYZ(bp.Max.X + F(0.80), bp.Max.Y + F(0.80), z));
    }
    if (itens.Contains("piso_tatil")) {
        var tipoTatil = new FilteredElementCollector(document).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault(ft => SemAc(ft.Name).Contains("TATIL"));
        if (tipoTatil == null) avisos.Add("piso tátil: nenhum tipo de piso com \"TÁTIL\" no nome — crie o tipo e rode de novo");
        else try {
            double va = -cw + F(0.50), vb = va + F(0.25);
            var a = Pt(0, va); var b = Pt(W, va); var c = Pt(W, vb); var d = Pt(0, vb);
            var laco = CurveLoop.Create(new List<Curve> { Line.CreateBound(a, b), Line.CreateBound(b, c), Line.CreateBound(c, d), Line.CreateBound(d, a) });
            var fl = Floor.Create(document, new List<CurveLoop> { laco }, tipoTatil.Id, nivel.Id);
            fl.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(MARCA);
            criados.Add("piso tátil → " + tipoTatil.Name + ": faixa de " + (W * 0.3048).ToString("0.00", cult) + " m a 0,50 m do meio-fio [" + fl.Id.Value + "]");
        } catch (Exception ex) { avisos.Add("piso tátil: " + ex.Message); }
    }
    t.Commit();
}
log.AppendLine("PROPOSTA de elementos externos — frente " + frente + ", garagem à " + ladoGar + ", calçada " + (cw * 0.3048).ToString("0.00", cult) + " m");
foreach (var c in criados) log.AppendLine("  • " + c);
if (avisos.Count > 0) log.AppendLine("Avisos:\n  " + string.Join("\n  ", avisos));
log.AppendLine("Mostre a implantação ao arquiteto. Aprovado → \"aprovar\"; ajuste → mover no Revit e \"aprovar\"; recusado → \"desfazer\".");
return log.ToString();
