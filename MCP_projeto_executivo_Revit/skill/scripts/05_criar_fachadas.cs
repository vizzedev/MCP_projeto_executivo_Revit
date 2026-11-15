// SKILL EXECUTIVO REVIT — Fase 5: criar as 4 fachadas
// send_code_to_revit · transactionMode "none"
// parameters[0] = prefixo do nome ("" no uso real; "SKILL " em teste)
// parameters[1] = modelo de vista (ex.: "VZ-ELEVAÇÃO FACHADA")
// parameters[2] = nível da planta onde os marcadores ficam (ex.: "PAV. TERREO N.A")
// parameters[3] = escala (padrão 75)
// parameters[4] = lado da frente, se quiser forçar: "N" (+Y), "S" (−Y), "L" (+X), "O" (−X). Vazio = detecta pela posição do ambiente CALÇADA.
// Enquadramento: na horizontal, paredes + telhados com 1 m de margem; na vertical, do nível mais baixo − 0,5 m ao topo + 0,5 m.
// Muro na frente: em cada fachada, oculta na vista os muros (paredes baixas, até 3 m) paralelos à fachada que ficam
// entre o marcador e a edificação e sobem mais de 1 m acima do piso — como o arquiteto faz no projeto de referência.
var prefixo = (parameters != null && parameters.Length > 0) ? (parameters[0] as string ?? "") : "";
var nomeModelo = (parameters != null && parameters.Length > 1) ? parameters[1] as string : "VZ-ELEVAÇÃO FACHADA";
var nomeNivel = (parameters != null && parameters.Length > 2) ? parameters[2] as string : null;
int escala = (parameters != null && parameters.Length > 3 && !string.IsNullOrWhiteSpace(parameters[3] as string)) ? int.Parse((string)parameters[3]) : 75;
var frenteForcada = (parameters != null && parameters.Length > 4) ? (parameters[4] as string ?? "").Trim().ToUpper() : "";
var log = new System.Text.StringBuilder();
Func<double,double> M = v => v * 0.3048; Func<double,double> F = v => v / 0.3048;

// limites do modelo
// extensão em planta por paredes e telhados (pisos externos — rua, calçada pública — ficam de fora); pisos só para a cota mais baixa
var filtro = new LogicalOrFilter(new List<ElementFilter>{ new ElementClassFilter(typeof(Wall)), new ElementClassFilter(typeof(RoofBase)) });
var bbs = new FilteredElementCollector(document).WherePasses(filtro).WhereElementIsNotElementType().ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
double x0 = bbs.Min(b => b.Min.X), x1 = bbs.Max(b => b.Max.X), y0 = bbs.Min(b => b.Min.Y), y1 = bbs.Max(b => b.Max.Y), z1 = bbs.Max(b => b.Max.Z);
var pisos = new FilteredElementCollector(document).OfClass(typeof(Floor)).ToElements().Select(e => e.get_BoundingBox(null))
    .Where(b => b != null && b.Min.X >= x0 - F(0.5) && b.Max.X <= x1 + F(0.5) && b.Min.Y >= y0 - F(0.5) && b.Max.Y <= y1 + F(0.5)).ToList();
double z0 = Math.Min(bbs.Min(b => b.Min.Z), pisos.Count > 0 ? pisos.Min(b => b.Min.Z) : double.MaxValue);
double cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, mg = F(1.0), afast = F(3.0);
double zNivelMin = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Min(l => l.Elevation);
log.AppendLine("Limites do modelo: X " + M(x0).ToString("0.0") + "→" + M(x1).ToString("0.0") + " | Y " + M(y0).ToString("0.0") + "→" + M(y1).ToString("0.0") + " | Z " + M(z0).ToString("0.0") + "→" + M(z1).ToString("0.0") + " m");

// lado da frente
string frente = frenteForcada;
if (string.IsNullOrEmpty(frente)) {
    var calcada = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
        .FirstOrDefault(r => r.Location != null && r.Area > 0 && r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString().ToUpper().Contains("CALÇADA"));
    if (calcada == null) return "ERRO: não achei o ambiente CALÇADA para saber a frente. Rode de novo informando o lado da frente (N, S, L ou O) em parameters[4].";
    var p = ((LocationPoint)calcada.Location).Point; double dx = (p.X - cx) / (x1 - x0), dy = (p.Y - cy) / (y1 - y0);
    frente = Math.Abs(dx) > Math.Abs(dy) ? (dx > 0 ? "L" : "O") : (dy > 0 ? "N" : "S");
    log.AppendLine("Frente detectada pela CALÇADA: lado " + frente);
}
// direção "para fora" de cada lado
var fora = new Dictionary<string, XYZ> { { "N", XYZ.BasisY }, { "S", -XYZ.BasisY }, { "L", XYZ.BasisX }, { "O", -XYZ.BasisX } };
var dirFrente = fora[frente];
var dirDireita = new XYZ(dirFrente.Y, -dirFrente.X, 0);   // quem olha a fachada frontal de fora: a direita dele
Func<XYZ,string> Lado = d => fora.First(kv => kv.Value.IsAlmostEqualTo(d)).Key;
var nomes = new Dictionary<string,string> { { Lado(dirFrente), "FACHADA FRONTAL" }, { Lado(-dirFrente), "FACHADA POSTERIOR" }, { Lado(-dirDireita), "FACHADA LATERAL DIREITA" }, { Lado(dirDireita), "FACHADA LATERAL ESQUERDA" } };

var planta = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan && v.GenLevel != null && (nomeNivel == null || v.GenLevel.Name == nomeNivel));
if (planta == null) return "ERRO: nenhuma planta do nível " + nomeNivel + " para hospedar os marcadores.";
var tipos = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t => t.ViewFamily == ViewFamily.Elevation).ToList();
var tipoElev = tipos.FirstOrDefault(t => t.Name.ToUpper().Contains("FACHADA")) ?? tipos.First();
var modelo = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v => v.IsTemplate && v.Name == nomeModelo);
var criadas = new List<string>();
// edificação = ambientes internos (sem áreas externas), para saber o que está "na frente" dela
var externos = new[]{"EXTERNA","CALÇADA","CALCADA","PISCINA","SOLARIUM","PERGOLADO","PRAINHA","HIDRO","JARDIM","DECK","ESTACIONAMENTO","DESCOBERTA","PRAIA"};
var bbAmb = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
    .Where(r => r.Area > 0 && !externos.Any(e => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper().Contains(e))).Select(r => r.get_BoundingBox(null)).Where(b => b != null).ToList();
var paredes = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Select(w => new { w, b = w.get_BoundingBox(null) }).Where(x => x.b != null).ToList();
using (var t = new Transaction(document, "Skill: criar fachadas")) {
    t.Start();
    foreach (var kv in fora) {
        var d = kv.Value;
        var pos = new XYZ(d.X > 0 ? x1 + afast : d.X < 0 ? x0 - afast : cx, d.Y > 0 ? y1 + afast : d.Y < 0 ? y0 - afast : cy, planta.GenLevel.Elevation);
        var marcador = ElevationMarker.CreateElevationMarker(document, tipoElev.Id, pos, escala);
        ViewSection vista = null;
        for (int i = 0; i < 4; i++) {
            var v = marcador.CreateElevation(document, planta.Id, i);
            if (v.ViewDirection.IsAlmostEqualTo(d)) vista = v; else document.Delete(v.Id);   // fica só a que olha para a edificação
        }
        if (vista == null) { criadas.Add("lado " + kv.Key + ": falhou"); continue; }
        vista.Name = prefixo + nomes[kv.Key];
        if (modelo != null) vista.ViewTemplateId = modelo.Id;
        vista.Scale = escala;
        // enquadramento em coordenadas da vista
        vista.CropBoxActive = true;
        var cb = vista.CropBox; var inv = cb.Transform.Inverse;
        var cantos = new List<XYZ>(); foreach (var xx in new[]{x0,x1}) foreach (var yy in new[]{y0,y1}) foreach (var zz in new[]{z0,z1}) cantos.Add(inv.OfPoint(new XYZ(xx,yy,zz)));
        // horizontal: edificação + 1 m; vertical: 0,5 m abaixo do nível mais baixo até 0,5 m acima do ponto mais alto
        double zLoc0 = inv.OfPoint(new XYZ(cx, cy, zNivelMin - F(0.5))).Y, zLoc1 = inv.OfPoint(new XYZ(cx, cy, z1 + F(0.5))).Y;
        cb.Min = new XYZ(cantos.Min(p => p.X) - mg, Math.Min(zLoc0, zLoc1), cb.Min.Z);
        cb.Max = new XYZ(cantos.Max(p => p.X) + mg, Math.Max(zLoc0, zLoc1), cb.Max.Z);
        vista.CropBox = cb;
        var prof = cantos.Max(p => -p.Z) + mg;   // até o lado oposto da edificação
        var pFar = vista.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR); if (pFar != null && !pFar.IsReadOnly) pFar.Set(prof);
        // ocultar muros na frente da edificação
        var ocultar = new List<ElementId>();
        if (bbAmb.Count > 0) {
            double limite = bbAmb.Max(b => Math.Max(d.DotProduct(b.Min), d.DotProduct(b.Max)));   // face da edificação voltada para este lado
            foreach (var x in paredes) {
                double c = d.DotProduct((x.b.Min + x.b.Max) / 2), espessura = Math.Abs(d.DotProduct(x.b.Max - x.b.Min));
                if (c > limite + F(0.3) && espessura < F(1.0) && (x.b.Max.Z - x.b.Min.Z) <= F(3.0) && x.b.Max.Z > planta.GenLevel.Elevation + F(1.0)) ocultar.Add(x.w.Id);   // só o que tampa a vista (sobe > 1 m)
            }
            if (ocultar.Count > 0) vista.HideElements(ocultar);
        }
        criadas.Add(vista.Name + " (lado " + kv.Key + ", largura " + M(cb.Max.X - cb.Min.X).ToString("0.0") + " m" + (ocultar.Count > 0 ? ", " + ocultar.Count + " muro(s) ocultado(s)" : "") + ")");
    }
    t.Commit();
}
log.AppendLine("Tipo: " + tipoElev.Name + " | modelo: " + (modelo != null ? modelo.Name : "NÃO ENCONTRADO") + " | escala 1:" + escala);
log.AppendLine("FACHADAS:"); foreach (var c in criadas) log.AppendLine("  " + c);
return log.ToString();
