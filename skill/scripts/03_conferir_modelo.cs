// SKILL EXECUTIVO REVIT — Fase 3: conferência do modelo (somente leitura)
// send_code_to_revit · transactionMode "none"
// parameters[0] = (opcional) "detalhe" para listar os elementos de cada problema (padrão: só contagens + 5 exemplos)
//
// Classifica em três níveis:
//   BLOQUEIA  — falta algo de que as fases seguintes dependem (níveis no padrão, calçada, linhas de propriedade, ambientes)
//   CORRIGIR  — aparece errado nas folhas/tabelas (ambiente sem nome/não colocado/não fechado, porta/janela sem marca ou repetida,
//               CAD importado no modelo, vínculo descarregado)
//   ATENÇÃO   — avisos do Revit agrupados, paredes muito curtas, áreas molhadas sem forro, cômodos sem piso
// Não altera nada. As correções automáticas ficam em 03_corrigir_modelo.cs (com confirmação).
bool detalhe = parameters.Length > 0 && ((parameters[0] as string) ?? "").ToLower().StartsWith("det");
Func<double, double> F = m => m / 0.3048;
var bloqueia = new List<string>(); var corrigir = new List<string>(); var atencao = new List<string>(); var ok = new List<string>();
Func<IEnumerable<string>, string> Ex = l => { var a = l.ToList(); return a.Count == 0 ? "" : " → " + string.Join("; ", detalhe ? a : a.Take(5)) + (!detalhe && a.Count > 5 ? "; … (+" + (a.Count - 5) + ")" : ""); };
Func<Element, string> Id = e => e.Name + " [" + e.Id.Value + "]";

// --- níveis
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
var na = niveis.Where(l => l.Name.ToUpper().StartsWith("PAV.") && l.Name.ToUpper().EndsWith("N.A")).ToList();
if (na.Count == 0) bloqueia.Add("Nenhum nível de piso acabado no padrão \"PAV. <NOME> N.A\" — as plantas e vistas por pavimento dependem dele.");
else ok.Add("Níveis de pavimento: " + string.Join(", ", na.Select(l => l.Name + " (" + (l.Elevation * 0.3048).ToString("0.00") + ")")));
foreach (var l in na) { var no = l.Name.Substring(0, l.Name.Length - 3) + "N.O"; var lo = niveis.FirstOrDefault(x => x.Name == no); if (lo == null) atencao.Add("Falta o nível de osso \"" + no + "\"."); else if (Math.Abs((l.Elevation - lo.Elevation) * 0.3048 - 0.05) > 0.005) atencao.Add("\"" + no + "\" não está 5 cm abaixo de \"" + l.Name + "\" (diferença " + ((l.Elevation - lo.Elevation) * 0.3048).ToString("0.00") + " m)."); }
if (!niveis.Any(l => l.Name.ToUpper().Contains("COBERTURA"))) bloqueia.Add("Falta o nível COBERTURA — a planta de cobertura depende dele.");

// --- ambientes
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().ToList();
Func<Autodesk.Revit.DB.Architecture.Room, string> Nome = r => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").Trim();
var naoColocados = rooms.Where(r => r.Location == null).ToList();
var naoFechados = rooms.Where(r => r.Location != null && r.Area == 0).ToList();
var validos = rooms.Where(r => r.Area > 0).ToList();
if (validos.Count == 0) bloqueia.Add("Nenhum ambiente colocado — vistas por ambiente, cortes e tabelas dependem deles.");
else ok.Add("Ambientes colocados: " + validos.Count);
if (naoColocados.Count > 0) corrigir.Add("Ambientes não colocados (aparecem nas tabelas sem área): " + naoColocados.Count + Ex(naoColocados.Select(r => "\"" + Nome(r) + "\" [" + r.Id.Value + "]")));
if (naoFechados.Count > 0) corrigir.Add("Ambientes não fechados ou redundantes (área zero): " + naoFechados.Count + Ex(naoFechados.Select(r => "\"" + Nome(r) + "\" em " + r.Level.Name + " [" + r.Id.Value + "]")));
var semNome = validos.Where(r => Nome(r) == "" || Nome(r).ToUpper() == "AMBIENTE" || Nome(r).ToUpper() == "ROOM").ToList();
if (semNome.Count > 0) corrigir.Add("Ambientes sem nome: " + semNome.Count + Ex(semNome.Select(r => "[" + r.Id.Value + "] " + r.Level.Name)));
if (!validos.Any(r => Nome(r).ToUpper().Contains("CALÇADA") || Nome(r).ToUpper().Contains("CALCADA")))
    bloqueia.Add("Nenhum ambiente \"CALÇADA\" — a skill usa a calçada para saber qual é a frente do lote (fachadas, situação). Crie o ambiente ou informe a frente manualmente.");

// --- terreno
int linhasProp = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_SiteProperty).WhereElementIsNotElementType().GetElementCount();
if (linhasProp == 0) atencao.Add("Sem linhas de propriedade — a planta de situação vai usar as paredes para medir o lote (confira testada e profundidade).");
else ok.Add("Linhas de propriedade: " + linhasProp);

// --- portas e janelas: marca vazia ou repetida
foreach (var cat in new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows }) {
    var nomeCat = cat == BuiltInCategory.OST_Doors ? "Portas" : "Janelas";
    var inst = new FilteredElementCollector(document).OfCategory(cat).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
    // a coluna ID das tabelas do escritório é a "Marca de tipo" (P01, J01…) — a marca de instância não aparece
    Func<FamilyInstance, string> Marca = fi => (fi.Symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK)?.AsString() ?? "").Trim();
    var semMarca = inst.Where(fi => Marca(fi) == "").ToList();
    ok.Add(nomeCat + ": " + inst.Count + " no modelo, " + inst.Select(fi => fi.Symbol.Id).Distinct().Count() + " tipos");
    if (semMarca.Count > 0) corrigir.Add(nomeCat + " sem marca de tipo (ficam sem ID na tabela e na planta): " + semMarca.Count + Ex(semMarca.Select(fi => fi.Symbol.FamilyName + " : " + fi.Symbol.Name + " [" + fi.Id.Value + "]").Distinct()));
    // mesma marca em tipos diferentes = códigos repetidos na tabela
    var repetidas = inst.Where(fi => Marca(fi) != "").GroupBy(Marca).Where(g => g.Select(fi => fi.Symbol.Id).Distinct().Count() > 1).ToList();
    if (repetidas.Count > 0) corrigir.Add(nomeCat + ": mesmo identificador em tipos diferentes: " + repetidas.Count + Ex(repetidas.Select(g => g.Key + " = " + string.Join(" / ", g.Select(fi => fi.Symbol.Name).Distinct()))));
}

// --- CAD e vínculos
var imports = new FilteredElementCollector(document).OfClass(typeof(ImportInstance)).Cast<ImportInstance>().ToList();
var cadImportado = imports.Where(i => !i.IsLinked).ToList();
if (cadImportado.Count > 0) corrigir.Add("CAD importado dentro do modelo (pesa o arquivo e polui as vistas; prefira vincular): " + cadImportado.Count + Ex(cadImportado.Select(i => (i.Category != null ? i.Category.Name : "?") + " [" + i.Id.Value + "]" + (i.ViewSpecific ? " na vista " + (document.GetElement(i.OwnerViewId) as View)?.Name : " em 3D"))));
var links = new FilteredElementCollector(document).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>().ToList();
foreach (var lt in links) { var st = RevitLinkType.IsLoaded(document, lt.Id); if (!st) atencao.Add("Vínculo Revit descarregado: " + lt.Name + " (se for necessário nas vistas, recarregue)."); }

// --- paredes muito curtas
var curtas = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.Location is LocationCurve && ((LocationCurve)w.Location).Curve.Length < F(0.05)).ToList();
if (curtas.Count > 0) atencao.Add("Paredes com menos de 5 cm (geralmente sobra de edição): " + curtas.Count + Ex(curtas.Select(w => Id(w))));

// --- áreas molhadas sem forro (o topo das isométricas de paginação usa o forro)
var molhados = new[] { "WC", "BANHEIRO", "LAVABO", "LAVANDERIA", "LAVATÓRIO", "LAVATORIO", "COZINHA" };
var forros = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType().ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
var semForro = validos.Where(r => molhados.Any(m => Nome(r).ToUpper().Contains(m))).Where(r => { var b = r.get_BoundingBox(null); var c = (b.Min + b.Max) / 2; return !forros.Any(f => c.X > f.Min.X && c.X < f.Max.X && c.Y > f.Min.Y && c.Y < f.Max.Y && f.Min.Z > r.Level.Elevation); }).ToList();
if (semForro.Count > 0) atencao.Add("Áreas molhadas sem forro modelado (planta de forro e isométricas de paginação ficam incompletas): " + semForro.Count + Ex(semForro.Select(r => Nome(r))));

// --- avisos do Revit agrupados
var avisos = document.GetWarnings();
var grupos = avisos.GroupBy(w => w.GetDescriptionText()).OrderByDescending(g => g.Count()).ToList();
if (avisos.Count > 0) atencao.Add("Avisos do Revit: " + avisos.Count + " em " + grupos.Count + " tipos" + Ex(grupos.Select(g => g.Count() + "× " + g.Key)));

// --- relatório
var sb = new System.Text.StringBuilder();
sb.AppendLine("CONFERÊNCIA DO MODELO — " + document.Title);
sb.AppendLine(bloqueia.Count == 0 ? "Nenhum bloqueio: o modelo pode seguir para as fases 4 e 5." : "⛔ " + bloqueia.Count + " bloqueio(s): resolver antes da fase 5.");
if (bloqueia.Count > 0) { sb.AppendLine("\nBLOQUEIA"); foreach (var s in bloqueia) sb.AppendLine("  • " + s); }
if (corrigir.Count > 0) { sb.AppendLine("\nCORRIGIR"); foreach (var s in corrigir) sb.AppendLine("  • " + s); }
if (atencao.Count > 0) { sb.AppendLine("\nATENÇÃO"); foreach (var s in atencao) sb.AppendLine("  • " + s); }
sb.AppendLine("\nOK"); foreach (var s in ok) sb.AppendLine("  • " + s);
return sb.ToString();
