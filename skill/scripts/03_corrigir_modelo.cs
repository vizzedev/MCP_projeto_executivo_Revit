// SKILL EXECUTIVO REVIT — Fase 3: correções seguras do modelo (só depois que o arquiteto confirmar)
// send_code_to_revit · transactionMode "none"
// parameters[0] = correções separadas por ";":
//   "ambientes_nao_colocados" — apaga ambientes sem localização (sobras de listas; aparecem em tabelas sem área)
//   "paredes_curtas"          — apaga paredes com menos de 5 cm sem portas/janelas hospedadas
//   "renumerar_esquadrias"    — renumera a marca de tipo das portas (P01, P02…) e janelas (J01, J02…) usadas no modelo,
//                               da maior largura para a menor; tipos não usados ficam como estão
// Tudo numa única transação "Skill: corrigir modelo" (desfazível com Ctrl+Z). Não salva.
// O que depende de decisão do arquiteto (ambientes não fechados, marcas de tipo repetidas, linhas de separação sobrepostas,
// CAD importado) fica só no relatório de 03_conferir_modelo.cs.
var pedidas = (parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "").ToLower().Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
if (pedidas.Count == 0) return "Nada pedido. Opções: ambientes_nao_colocados; paredes_curtas; renumerar_esquadrias";
var log = new System.Text.StringBuilder();
using (var t = new Transaction(document, "Skill: corrigir modelo")) {
    t.Start();
    if (pedidas.Contains("ambientes_nao_colocados")) {
        var ids = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>()
            .Where(r => r.Location == null).Select(r => r.Id).ToList();
        if (ids.Count > 0) document.Delete(ids);
        log.AppendLine("Ambientes não colocados apagados: " + ids.Count);
    }
    if (pedidas.Contains("paredes_curtas")) {
        var hospedes = new HashSet<ElementId>(new FilteredElementCollector(document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(fi => fi.Host is Wall).Select(fi => fi.Host.Id));
        var ids = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>()
            .Where(w => w.Location is LocationCurve && ((LocationCurve)w.Location).Curve.Length < 0.05 / 0.3048 && !hospedes.Contains(w.Id)).Select(w => w.Id).ToList();
        if (ids.Count > 0) document.Delete(ids);
        log.AppendLine("Paredes com menos de 5 cm apagadas: " + ids.Count);
    }
    if (pedidas.Contains("renumerar_esquadrias")) {
        foreach (var par in new[] { Tuple.Create(BuiltInCategory.OST_Doors, "P"), Tuple.Create(BuiltInCategory.OST_Windows, "J") }) {
            var tipos = new FilteredElementCollector(document).OfCategory(par.Item1).WhereElementIsNotElementType().Cast<FamilyInstance>().Select(fi => fi.Symbol).GroupBy(x => x.Id).Select(g => g.First()).ToList();
            Func<FamilySymbol, double> Larg = fs => { var p = fs.get_Parameter(BuiltInParameter.DOOR_WIDTH) ?? fs.get_Parameter(BuiltInParameter.WINDOW_WIDTH) ?? fs.LookupParameter("Largura"); return p != null && p.StorageType == StorageType.Double ? p.AsDouble() : 0; };
            int n = 0; var mud = new List<string>();
            foreach (var fs in tipos.OrderByDescending(Larg).ThenBy(x => x.FamilyName).ThenBy(x => x.Name)) {
                var nova = par.Item2 + (++n).ToString("00"); var pm = fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK); var velha = pm.AsString() ?? "";
                if (velha != nova) { pm.Set(nova); mud.Add(fs.Name + ": " + (velha == "" ? "(vazia)" : velha) + " → " + nova); }
            }
            log.AppendLine((par.Item2 == "P" ? "Portas" : "Janelas") + " renumeradas: " + mud.Count + (mud.Count > 0 ? " (" + string.Join("; ", mud) + ")" : ""));
        }
    }
    t.Commit();
}
log.AppendLine("Feito numa transação só (Ctrl+Z desfaz). O arquivo não foi salvo.");
return log.ToString();
