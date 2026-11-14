// SKILL EXECUTIVO REVIT — Fase 5.4: perspectivas 3D (cortes e fachadas)
// send_code_to_revit · transactionMode "none"
// parameters[0] = prefixo dos nomes ("" no uso real; "SKILL " em teste) — também usado para achar cortes e fachadas
// parameters[1] = letras dos cortes que ganham perspectiva, separadas por ";" — vazio ou "auto" = automático:
//                 em cada par (A/B transversais, C/D longitudinais) usa o corte que deixa a MENOR parte da casa
//                 (regra observada no projeto de referência; é o que faz as duas perspectivas caberem na folha 3/7)
// parameters[2] = modelo de vista (padrão "VZ-MODELAGEM FACHADA")
// parameters[3] = escala (padrão 100)
//
// PERSPECTIVA CORTE X: isométrica com caixa de corte = edificação inteira cortada no plano do corte,
//   mantendo o lado que o corte enxerga; câmera olhando a face cortada, girada 45° (anti-horário) e 17° de cima.
// PERSPECTIVA LATERAL DIREITA / ESQUERDA: isométrica dos cantos da frente — olhando a fachada frontal,
//   girada 17° para o lado da lateral, 12° de cima; sem caixa de corte.
// A frente vem da elevação "<prefixo>FACHADA FRONTAL" criada por 05_criar_fachadas.cs.
// Validado no projeto de referência (direções iguais às do arquiteto com diferença de até ±5°).
var prefixo = parameters.Length > 0 ? (parameters[0] as string ?? "") : "";
var parLetras = parameters.Length > 1 ? (parameters[1] as string ?? "").Trim() : "";
var letras = (parLetras == "" || parLetras.ToLower() == "auto") ? new List<string>() : parLetras.Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
var nomeModelo = parameters.Length > 2 && !string.IsNullOrWhiteSpace(parameters[2] as string) ? (string)parameters[2] : "VZ-MODELAGEM FACHADA";
int escala = parameters.Length > 3 && !string.IsNullOrWhiteSpace(parameters[3] as string) ? int.Parse((string)parameters[3]) : 100;
Func<double, double> F = v => v / 0.3048;
var log = new System.Text.StringBuilder();
var modelo = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v => v.IsTemplate && v.Name == nomeModelo);
var tipo3D = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().First(t => t.ViewFamily == ViewFamily.ThreeDimensional);
var vistas = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();

// caixa da edificação: extensão em planta por paredes e telhados (pisos externos — rua, calçada, grama — ficam de fora);
// pisos só contam para a cota mais baixa
var cats = new List<BuiltInCategory> { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Roofs };
var cantos = new List<XYZ>();
foreach (var e in new FilteredElementCollector(document).WherePasses(new ElementMulticategoryFilter(cats)).WhereElementIsNotElementType().ToElements()) {
    var b = e.get_BoundingBox(null); if (b == null) continue; cantos.Add(b.Min); cantos.Add(b.Max);
}
double X0 = cantos.Min(p => p.X), X1 = cantos.Max(p => p.X), Y0 = cantos.Min(p => p.Y), Y1 = cantos.Max(p => p.Y), Z1 = cantos.Max(p => p.Z) + F(0.1);
var pisosDentro = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType().ToElements()
    .Select(e => e.get_BoundingBox(null)).Where(b => b != null && b.Min.X >= X0 - F(0.5) && b.Max.X <= X1 + F(0.5) && b.Min.Y >= Y0 - F(0.5) && b.Max.Y <= Y1 + F(0.5)).ToList();
double Z0 = Math.Min(cantos.Min(p => p.Z), pisosDentro.Count > 0 ? pisosDentro.Min(b => b.Min.Z) : double.MaxValue) - F(0.1);
var oito = new List<XYZ>(); foreach (var x in new[] { X0, X1 }) foreach (var y in new[] { Y0, Y1 }) foreach (var z in new[] { Z0, Z1 }) oito.Add(new XYZ(x, y, z));
var centro = new XYZ((X0 + X1) / 2, (Y0 + Y1) / 2, (Z0 + Z1) / 2);

Func<XYZ, double, double, XYZ> Frente = (u, giroGraus, inclGraus) => {   // direção horizontal u girada e inclinada para baixo
    double a = giroGraus * Math.PI / 180, i = inclGraus * Math.PI / 180;
    var h = new XYZ(u.X * Math.Cos(a) - u.Y * Math.Sin(a), u.X * Math.Sin(a) + u.Y * Math.Cos(a), 0).Normalize();
    return new XYZ(h.X * Math.Cos(i), h.Y * Math.Cos(i), -Math.Sin(i)).Normalize();
};
Func<string, XYZ, XYZ, BoundingBoxXYZ, View3D> Criar = (nome, fwd, alvo, caixa) => {
    var antiga = vistas.FirstOrDefault(v => v.Name == nome); if (antiga != null) { log.AppendLine("JÁ EXISTE, pulado: " + nome); return null; }
    var v3 = View3D.CreateIsometric(document, tipo3D.Id);
    v3.Name = nome;
    if (caixa != null) v3.SetSectionBox(caixa); else v3.IsSectionBoxActive = false;
    var up = XYZ.BasisZ.Subtract(fwd.Multiply(fwd.Z)).Normalize();
    v3.SetOrientation(new ViewOrientation3D(alvo - fwd.Multiply(F(80)), up, fwd));
    if (modelo != null) v3.ViewTemplateId = modelo.Id;
    v3.Scale = escala;
    // recorte ajustado ao conteúdo (caixa de corte, ou edificação inteira) + 0,2 m
    document.Regenerate();
    var pts = new List<XYZ>();
    if (caixa != null) { foreach (var x in new[] { caixa.Min.X, caixa.Max.X }) foreach (var y in new[] { caixa.Min.Y, caixa.Max.Y }) foreach (var z in new[] { caixa.Min.Z, caixa.Max.Z }) pts.Add(caixa.Transform.OfPoint(new XYZ(x, y, z))); }
    else pts.AddRange(oito);
    var cb = v3.CropBox; var invC = cb.Transform.Inverse; var loc = pts.Select(p => invC.OfPoint(p)).ToList();
    cb.Min = new XYZ(loc.Min(p => p.X) - F(0.2), loc.Min(p => p.Y) - F(0.2), cb.Min.Z);
    cb.Max = new XYZ(loc.Max(p => p.X) + F(0.2), loc.Max(p => p.Y) + F(0.2), cb.Max.Z);
    v3.CropBox = cb; v3.CropBoxActive = true; v3.CropBoxVisible = false;
    // níveis aparecem como linhas tracejadas em 3D recortado; o modelo de vista controla V/G, então oculta por elemento
    var niveis = new FilteredElementCollector(document, v3.Id).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElementIds();
    if (niveis.Count > 0) v3.HideElements(niveis);
    log.AppendLine("   tamanho na folha 1:" + escala + ": " + Math.Round((cb.Max.X - cb.Min.X) * 304.8 / escala) + " × " + Math.Round((cb.Max.Y - cb.Min.Y) * 304.8 / escala) + " mm");
    return v3;
};

Func<string, ViewSection> AcharCorte = L => vistas.OfType<ViewSection>().FirstOrDefault(v => v.ViewType == ViewType.Section && (v.Name == prefixo + "CORTE " + L || v.Name == prefixo + L))
                 ?? vistas.OfType<ViewSection>().FirstOrDefault(v => v.ViewType == ViewType.Section && v.Name.StartsWith(prefixo) && v.Name.EndsWith(" " + L));
Func<ViewSection, double> ParteVista = c => { var u = new XYZ(-c.ViewDirection.X, -c.ViewDirection.Y, 0).Normalize(); return oito.Max(p => u.DotProduct(p)) - u.DotProduct(c.Origin); };
if (letras.Count == 0) foreach (var par in new[] { new[] { "A", "B" }, new[] { "C", "D" } }) {
    var cand = par.Select(L => new { L, c = AcharCorte(L) }).Where(x => x.c != null).ToList();
    if (cand.Count == 0) continue;
    var esc = cand.OrderBy(x => ParteVista(x.c)).First(); letras.Add(esc.L);
    log.AppendLine("automático: corte " + esc.L + " (parte vista " + (ParteVista(esc.c) * 0.3048).ToString("0.0") + " m" + string.Concat(cand.Where(x => x.L != esc.L).Select(x => "; " + x.L + " = " + (ParteVista(x.c) * 0.3048).ToString("0.0") + " m")) + ")");
}
using (var t = new Transaction(document, "Skill: perspectivas 3D")) {
    t.Start();
    // cortes
    foreach (var L in letras) {
        var corte = AcharCorte(L);
        if (corte == null) { log.AppendLine("CORTE NÃO ENCONTRADO: " + L); continue; }
        var u = new XYZ(-corte.ViewDirection.X, -corte.ViewDirection.Y, 0).Normalize();   // para dentro da parte vista
        var tr = Transform.Identity; tr.BasisX = u; tr.BasisY = XYZ.BasisZ.CrossProduct(u); tr.BasisZ = XYZ.BasisZ;
        var inv = tr.Inverse; var loc = oito.Select(p => inv.OfPoint(p)).ToList();
        double plano = u.DotProduct(corte.Origin);
        var caixa = new BoundingBoxXYZ { Transform = tr, Min = new XYZ(plano, loc.Min(p => p.Y), Z0), Max = new XYZ(loc.Max(p => p.X), loc.Max(p => p.Y), Z1) };
        var alvo = tr.OfPoint(new XYZ((plano + loc.Max(p => p.X)) / 2, (loc.Min(p => p.Y) + loc.Max(p => p.Y)) / 2, (Z0 + Z1) / 2));
        var fwd = Frente(u, 45, 17);
        var v = Criar(prefixo + "PERSPECTIVA CORTE " + L, fwd, alvo, caixa);
        if (v != null) log.AppendLine(v.Name + " → olhando (" + fwd.X.ToString("0.00") + "," + fwd.Y.ToString("0.00") + "," + fwd.Z.ToString("0.00") + "), caixa a partir do plano do corte");
    }
    // fachadas
    var frontal = vistas.FirstOrDefault(v => v.ViewType == ViewType.Elevation && v.Name == prefixo + "FACHADA FRONTAL");
    var direita = vistas.FirstOrDefault(v => v.ViewType == ViewType.Elevation && v.Name == prefixo + "FACHADA LATERAL DIREITA");
    if (frontal == null || direita == null) log.AppendLine("FACHADAS NÃO ENCONTRADAS — rode 05_criar_fachadas.cs antes");
    else {
        var uF = new XYZ(-frontal.ViewDirection.X, -frontal.ViewDirection.Y, 0).Normalize();   // olhando a frente
        var dD = new XYZ(direita.ViewDirection.X, direita.ViewDirection.Y, 0).Normalize();     // para fora da lateral direita
        // giro positivo = anti-horário; escolher o sinal que traz a câmera para o lado da lateral
        double sinal = Math.Sign(uF.X * (-dD.Y) - uF.Y * (-dD.X));   // componente z de uF × (−dD)
        foreach (var lado in new[] { "DIREITA", "ESQUERDA" }) {
            var g = (lado == "DIREITA" ? 1 : -1) * sinal * 17;
            var fwd = Frente(uF, g, 12);
            var v = Criar(prefixo + "PERSPECTIVA LATERAL " + lado, fwd, centro, null);
            if (v != null) log.AppendLine(v.Name + " → olhando (" + fwd.X.ToString("0.00") + "," + fwd.Y.ToString("0.00") + "," + fwd.Z.ToString("0.00") + ")");
        }
    }
    t.Commit();
}
log.AppendLine("modelo: " + (modelo != null ? modelo.Name : "NÃO ENCONTRADO") + " | 1:" + escala);
return log.ToString();
