// SKILL EXECUTIVO REVIT — Fase 3: conferência do modelo (somente leitura)
// send_code_to_revit · transactionMode "none"
// parameters[0] = (opcional) "detalhe" para listar os elementos de cada problema (padrão: só contagens + 5 exemplos)
//
// Classifica em três níveis:
//   BLOQUEIA  — falta algo de que as fases seguintes dependem (níveis no padrão, calçada, linhas de propriedade, ambientes)
//   CORRIGIR  — aparece errado nas folhas/tabelas (ambiente sem nome/não colocado/não fechado, porta/janela sem marca ou repetida,
//               CAD importado no modelo, vínculo descarregado)
//   ATENÇÃO   — avisos do Revit agrupados, paredes muito curtas, áreas molhadas sem forro, cômodos sem piso
//   REGRAS DE PROJETO (ATENÇÃO) — padrões do checklist executivo do escritório que dá para medir no modelo:
//               peitoril de janela 1,00 m · porta de entrada ≥ 1,30 m e pivotante · porta de lavabo ≥ 0,70 m ·
//               porta de alumínio em corredor ≥ 0,90 m · vaga de garagem 3,00 × 5,00 m · bancadas (cozinha 0,70 de
//               profundidade e 0,90/0,95 de altura, ilha ≥ 0,90, lavabo 0,55, lavanderia 0,60, churrasqueira 1,10 ×
//               0,80–0,90) · topografia modelada. São da modelagem da equipe: a skill só aponta, a equipe decide.
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
    var foraPadrao = inst.Select(fi => fi.Symbol).GroupBy(x => x.Id).Select(g => g.First()).Where(fs => { var m = (fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK)?.AsString() ?? "").Trim(); return m != "" && !System.Text.RegularExpressions.Regex.IsMatch(m, "^" + (cat == BuiltInCategory.OST_Doors ? "P" : "J") + @"\d{2,}$"); }).ToList();
    if (foraPadrao.Count > 0) atencao.Add(nomeCat + " com marca fora do padrão " + (cat == BuiltInCategory.OST_Doors ? "P01, P02…" : "J01, J02…") + ": " + foraPadrao.Count + Ex(foraPadrao.Select(fs => fs.Name + " = " + fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK).AsString())) + " — 03_corrigir_modelo.cs \"renumerar_esquadrias\" renumera.");
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

// --- regras de projeto (checklist executivo) — só aponta, tolerância de 2 cm
var regras = new List<string>();
double tol = F(0.02);
Phase faseFinal = null; foreach (Phase ph in document.Phases) faseFinal = ph;
Func<FamilyInstance, Autodesk.Revit.DB.Architecture.Room[]> Lados = fi => { try { return new[] { fi.get_FromRoom(faseFinal), fi.get_ToRoom(faseFinal) }; } catch { return new Autodesk.Revit.DB.Architecture.Room[] { null, null }; } };
Func<FamilyInstance, BuiltInParameter, double> Dim = (fi, bip) => { var p = fi.get_Parameter(bip); if (p == null || !p.HasValue || p.AsDouble() <= 0) p = fi.Symbol.get_Parameter(bip); return p != null && p.HasValue ? p.AsDouble() : 0; };
Func<Autodesk.Revit.DB.Architecture.Room, string> NomeU = r => r == null ? "" : Nome(r).ToUpper();
Func<double, string> M = v => (v * 0.3048).ToString("0.00");
var umidos = new[] { "WC", "BANHEIRO", "LAVABO", "BANHO", "LAVATÓRIO", "LAVATORIO" };
// janelas: peitoril padrão 1,00 m (banheiros e lavabos ficam de fora: costumam ter peitoril alto)
var janelas = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
var peitoril = janelas.Where(fi => { var p = fi.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM); if (p == null || !p.HasValue) return false;
    var amb = Lados(fi).Where(r => r != null).Select(NomeU).ToList(); if (amb.Any(n => umidos.Any(u => n.Contains(u)))) return false;
    return Math.Abs(p.AsDouble() - F(1.00)) > tol; }).ToList();
if (peitoril.Count > 0) regras.Add("Janelas com peitoril diferente de 1,00 m (fora banheiros/lavabos): " + peitoril.Count + Ex(peitoril.Select(fi => fi.Symbol.Name + " h=" + M(fi.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).AsDouble()) + " [" + fi.Id.Value + "]")));
// portas
var portas = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
var entradaNomes = new[] { "SALA", "ESTAR", "LIVING", "HALL", "ENTRADA", "VESTÍBULO", "VESTIBULO" };
foreach (var fi in portas) {
    var lados = Lados(fi); var nomes = lados.Select(NomeU).ToList(); double w = Dim(fi, BuiltInParameter.DOOR_WIDTH); if (w <= 0) w = Dim(fi, BuiltInParameter.FAMILY_WIDTH_PARAM);
    string tipo = (fi.Symbol.FamilyName + " " + fi.Symbol.Name).ToUpper(); string id = fi.Symbol.Name + " [" + fi.Id.Value + "]";
    bool externa = lados.Any(r => r == null) || nomes.Any(n => n.Contains("ALPENDRE") || n.Contains("VARANDA") || n.Contains("CALÇADA"));
    if (externa && nomes.Any(n => entradaNomes.Any(e => n.Contains(e)))) {
        if (w > 0 && w < F(1.30) - tol) regras.Add("Porta de entrada com " + M(w) + " m (mínimo 1,30 m): " + id);
        if (!tipo.Contains("PIVOT")) regras.Add("Porta de entrada não é pivotante: " + id);
    }
    if (nomes.Any(n => n.Contains("LAVABO")) && w > 0 && w < F(0.70) - tol) regras.Add("Porta de lavabo com " + M(w) + " m (vão livre mínimo 0,70 m): " + id);
    if ((tipo.Contains("ALUM")) && nomes.Any(n => n.Contains("CORREDOR") || n.Contains("CIRCULA")) && w > 0 && w < F(0.90) - tol) regras.Add("Porta de alumínio em corredor com " + M(w) + " m (mínimo 0,90 m): " + id);
}
// garagem: vaga padrão 3,00 × 5,00 m (pelo contorno do ambiente)
foreach (var r in validos.Where(r => NomeU(r).Contains("GARAGEM"))) {
    var b = r.get_BoundingBox(null); double a1 = Math.Min(b.Max.X - b.Min.X, b.Max.Y - b.Min.Y), a2 = Math.Max(b.Max.X - b.Min.X, b.Max.Y - b.Min.Y);
    if (a1 < F(3.00) - tol || a2 < F(5.00) - tol) regras.Add("Garagem \"" + Nome(r) + "\" com " + M(a1) + " × " + M(a2) + " m (vaga padrão 3,00 × 5,00 m)");
}
// bancadas (marcenaria/casework): profundidade = menor lado em planta; altura = topo − nível do ambiente
var bancadas = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Casework).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
foreach (var fi in bancadas) {
    var b = fi.get_BoundingBox(null); if (b == null) continue;
    var c = (b.Min + b.Max) / 2; var r = validos.FirstOrDefault(x => { try { return x.IsPointInRoom(new XYZ(c.X, c.Y, x.Level.Elevation + F(0.5))); } catch { return false; } });
    string amb = NomeU(r); string nm = (fi.Symbol.FamilyName + " " + fi.Symbol.Name).ToUpper(); if (!nm.Contains("BANCADA") && !nm.Contains("ILHA") && !nm.Contains("CHURRASQ")) continue;
    double prof = Math.Min(b.Max.X - b.Min.X, b.Max.Y - b.Min.Y), larg = Math.Max(b.Max.X - b.Min.X, b.Max.Y - b.Min.Y), alt = r != null ? b.Max.Z - r.Level.Elevation : 0;
    string id = fi.Symbol.Name + " em " + (r != null ? Nome(r) : "?") + " [" + fi.Id.Value + "]";
    if (nm.Contains("CHURRASQ")) { if (Math.Abs(larg - F(1.10)) > tol) regras.Add("Churrasqueira com " + M(larg) + " m de largura (padrão 1,10 m): " + id);
        if (prof < F(0.80) - tol || prof > F(0.90) + tol) regras.Add("Churrasqueira com " + M(prof) + " m de profundidade (padrão 0,80–0,90 m): " + id); continue; }
    if (nm.Contains("ILHA")) { if (prof < F(0.90) - tol) regras.Add("Ilha com " + M(prof) + " m de largura (mínimo 0,90 m): " + id); continue; }
    if (amb.Contains("COZINHA") || amb.Contains("GOURMET")) {
        if (Math.Abs(prof - F(0.70)) > tol) regras.Add("Bancada de cozinha com " + M(prof) + " m de profundidade (padrão 0,70 m): " + id);
        if (r != null && Math.Abs(alt - F(0.90)) > tol && Math.Abs(alt - F(0.95)) > tol) regras.Add("Bancada de cozinha com " + M(alt) + " m de altura (padrão 0,90 m; 0,95 m com cervejeira): " + id);
    } else if (amb.Contains("LAVABO")) { if (Math.Abs(prof - F(0.55)) > tol) regras.Add("Bancada de lavabo com " + M(prof) + " m (padrão 0,55 m): " + id); }
    else if (amb.Contains("LAVANDERIA") || amb.Contains("SERVIÇO") || amb.Contains("SERVICO")) { if (Math.Abs(prof - F(0.60)) > tol) regras.Add("Bancada de lavanderia com " + M(prof) + " m (padrão 0,60 m): " + id); }
}
// topografia
bool temTopo = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Topography).WhereElementIsNotElementType().GetElementCount() > 0;
try { temTopo = temTopo || new FilteredElementCollector(document).OfClass(typeof(Toposolid)).GetElementCount() > 0; } catch { }
if (!temTopo) regras.Add("Topografia não modelada (o checklist pede o levantamento planialtimétrico no Revit: níveis do lote, postes, árvores, boca de lobo).");

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
if (regras.Count > 0) { sb.AppendLine("\nATENÇÃO — REGRAS DE PROJETO (checklist executivo; a equipe decide se corrige)"); foreach (var s in regras) sb.AppendLine("  • " + s); }
else sb.AppendLine("\nREGRAS DE PROJETO: nada fora do padrão do checklist entre as que a skill confere.");
sb.AppendLine("\nOK"); foreach (var s in ok) sb.AppendLine("  • " + s);
return sb.ToString();
