// SKILL EXECUTIVO REVIT — Fase 5: propor os 4 cortes (2 transversais A/B, 2 longitudinais C/D)
// send_code_to_revit · transactionMode "none"
// parameters[0] = prefixo do nome das vistas ("" no uso real; "SKILL " em teste)
// parameters[1] = nome do modelo de vista de corte (ex.: "VZ-CORTE EXECUTIVO")
// parameters[2] = nível de referência para os ambientes (ex.: "PAV. TERREO N.A")
// parameters[3] = escala (padrão 75) — o modelo de vista do escritório não controla a escala
// parameters[4] = trecho do nome do tipo de corte (padrão "CORTE EXECUTIVO"; define o marcador)
// Regras:
//  - Contorno da edificação = ambientes internos do nível (exclui externos: ÁREA EXTERNA, CALÇADA, PISCINA, SOLARIUM, PERGOLADO, PRAINHA, HIDRO, JARDIM, DECK, ESTACIONAMENTO, GARAGEM DESCOBERTA).
//  - Transversais (cortam o lado maior): A pela área molhada (centro dos banheiros/lavanderia/cozinha), B pelo maior ambiente social; afastamento mínimo de 25% do comprimento.
//  - Longitudinais (cortam o lado menor): C pela área molhada, D pelo maior ambiente social (mesma regra).
//  - O arquiteto confere em planta e arrasta as linhas se quiser; depois a skill segue.
var prefixo = (parameters != null && parameters.Length > 0) ? (parameters[0] as string ?? "") : "";
var nomeModelo = (parameters != null && parameters.Length > 1) ? parameters[1] as string : "VZ-CORTE EXECUTIVO";
var nomeNivel = (parameters != null && parameters.Length > 2) ? parameters[2] as string : null;
int escala = (parameters != null && parameters.Length > 3 && !string.IsNullOrWhiteSpace(parameters[3] as string)) ? int.Parse((string)parameters[3]) : 75;
var trechoTipo = (parameters != null && parameters.Length > 4 && !string.IsNullOrWhiteSpace(parameters[4] as string)) ? (string)parameters[4] : "CORTE EXECUTIVO";
var log = new System.Text.StringBuilder();
Func<double,double> M = v => v * 0.3048; Func<double,double> F = v => v / 0.3048;

var externos = new[]{"EXTERNA","CALÇADA","CALCADA","PISCINA","SOLARIUM","PERGOLADO","PRAINHA","HIDRO","JARDIM","DECK","ESTACIONAMENTO","DESCOBERTA","PRAIA"};
var molhados = new[]{"WC","BANHEIRO","LAVABO","LAVANDERIA","COZINHA","SERVIÇO","LAVATÓRIO","LAVATORIO","GOURMET"};
var sociais = new[]{"SALA","LIVING","ESTAR","JANTAR","TV","GOURMET","HALL"};
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
    .Where(r => r.Area > 0 && r.Location != null && (nomeNivel == null || r.Level.Name == nomeNivel)).ToList();
Func<Autodesk.Revit.DB.Architecture.Room,string> Nome = r => r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString().ToUpper();
var internos = rooms.Where(r => !externos.Any(e => Nome(r).Contains(e))).ToList();
if (internos.Count == 0) return "ERRO: nenhum ambiente interno encontrado" + (nomeNivel != null ? " no nível " + nomeNivel : "");
var bbs = internos.Select(r => r.get_BoundingBox(null)).ToList();
double x0 = bbs.Min(b => b.Min.X), x1 = bbs.Max(b => b.Max.X), y0 = bbs.Min(b => b.Min.Y), y1 = bbs.Max(b => b.Max.Y);
bool xLongo = (x1 - x0) >= (y1 - y0);
log.AppendLine("Edificação (ambientes internos: " + internos.Count + "): " + M(x1 - x0).ToString("0.0") + " × " + M(y1 - y0).ToString("0.0") + " m | lado maior em " + (xLongo ? "X" : "Y"));

// alturas: do piso mais baixo ao topo do modelo (paredes, telhados)
var elems = new FilteredElementCollector(document).WherePasses(new LogicalOrFilter(new ElementClassFilter(typeof(Wall)), new ElementClassFilter(typeof(RoofBase)))).ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
double zTopo = elems.Max(b => b.Max.Z) + F(0.3), zBase = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Min(l => l.Elevation) - F(0.3);

Func<IEnumerable<Autodesk.Revit.DB.Architecture.Room>, XYZ> Centro = rs => { var l = rs.ToList(); if (l.Count == 0) return null; return new XYZ(l.Average(r => ((LocationPoint)r.Location).Point.X), l.Average(r => ((LocationPoint)r.Location).Point.Y), 0); };
var cMolhado = Centro(internos.Where(r => molhados.Any(w => Nome(r).Contains(w))));
var maiorSocial = internos.Where(r => sociais.Any(s => Nome(r).Contains(s))).OrderByDescending(r => r.Area).FirstOrDefault() ?? internos.OrderByDescending(r => r.Area).First();
var cSocial = ((LocationPoint)maiorSocial.Location).Point;
log.AppendLine("Área molhada: " + (cMolhado != null ? "(" + M(cMolhado.X).ToString("0.0") + ", " + M(cMolhado.Y).ToString("0.0") + ") m" : "não encontrada") + " | maior social: " + Nome(maiorSocial));

// posições ao longo de um eixo, garantindo afastamento mínimo
Func<double,double,double,double,double[]> Duas = (a, b, lo, hi) => {
    double comp = hi - lo, min = 0.25 * comp;
    if (double.IsNaN(a)) a = lo + comp / 3; if (double.IsNaN(b)) b = lo + 2 * comp / 3;
    a = Math.Max(lo + 0.1 * comp, Math.Min(hi - 0.1 * comp, a)); b = Math.Max(lo + 0.1 * comp, Math.Min(hi - 0.1 * comp, b));
    if (Math.Abs(a - b) < min) { var m = (a + b) / 2; a = m - min / 2; b = m + min / 2; if (a < lo + 0.1 * comp) { a = lo + 0.1 * comp; b = a + min; } if (b > hi - 0.1 * comp) { b = hi - 0.1 * comp; a = b - min; } }
    return new[]{ a, b };
};
double mx = cMolhado != null ? cMolhado.X : double.NaN, my = cMolhado != null ? cMolhado.Y : double.NaN;
var posLongo = xLongo ? Duas(mx, cSocial.X, x0, x1) : Duas(my, cSocial.Y, y0, y1);   // transversais A, B
var posCurto = xLongo ? Duas(my, cSocial.Y, y0, y1) : Duas(mx, cSocial.X, x0, x1);   // longitudinais C, D

var tiposCorte = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t => t.ViewFamily == ViewFamily.Section).ToList();
var tipoCorte = tiposCorte.FirstOrDefault(t => t.Name.ToUpper().Contains(trechoTipo.ToUpper())) ?? tiposCorte.First();
var modelo = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().FirstOrDefault(v => v.IsTemplate && v.Name == nomeModelo);
if (modelo == null) log.AppendLine("AVISO: modelo de vista '" + nomeModelo + "' não encontrado — cortes sem modelo.");
double margem = F(1.0);
var criados = new List<string>();
using (var t = new Transaction(document, "Skill: propor cortes")) {
    t.Start();
    Action<string, bool, double> Criar = (nome, transversal, pos) => {
        // transversal corta o lado maior: plano perpendicular ao eixo longo
        bool planoEmX = transversal ? xLongo : !xLongo;          // plano de corte com coordenada fixa em X?
        XYZ bz, bx; XYZ origem; double meiaLarg, prof;
        // Convenção do CreateSection (verificada): o plano de corte fica na face Min.Z e o corte olha no sentido +BasisZ.
        if (planoEmX) { bz = new XYZ(1, 0, 0); bx = new XYZ(0, 1, 0); origem = new XYZ(pos, (y0 + y1) / 2, 0); meiaLarg = (y1 - y0) / 2 + margem; prof = (x1 - pos) + margem; }
        else          { bz = new XYZ(0, -1, 0); bx = new XYZ(1, 0, 0); origem = new XYZ((x0 + x1) / 2, pos, 0); meiaLarg = (x1 - x0) / 2 + margem; prof = (pos - y0) + margem; }
        var tr = Transform.Identity; tr.Origin = origem; tr.BasisX = bx; tr.BasisY = XYZ.BasisZ; tr.BasisZ = bz;
        var caixa = new BoundingBoxXYZ { Transform = tr, Min = new XYZ(-meiaLarg, zBase, 0), Max = new XYZ(meiaLarg, zTopo, prof) };
        var v = ViewSection.CreateSection(document, tipoCorte.Id, caixa);
        v.Name = prefixo + nome;
        if (modelo != null) v.ViewTemplateId = modelo.Id;
        v.Scale = escala;
        criados.Add(v.Name + " → " + (planoEmX ? "x = " + M(pos).ToString("0.00") : "y = " + M(pos).ToString("0.00")) + " m (" + (transversal ? "transversal" : "longitudinal") + ", profundidade " + M(prof).ToString("0.0") + " m)");
    };
    Criar("A", true, posLongo[0]); Criar("B", true, posLongo[1]);
    Criar("C", false, posCurto[0]); Criar("D", false, posCurto[1]);
    t.Commit();
}
log.AppendLine("CORTES PROPOSTOS:"); foreach (var c in criados) log.AppendLine("  " + c);
log.AppendLine("Confira em planta e arraste as linhas se quiser; depois confirme para seguir.");
return log.ToString();
