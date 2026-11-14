// SKILL EXECUTIVO REVIT — Fase 3: correções seguras do modelo (só depois que o arquiteto confirmar)
// send_code_to_revit · transactionMode "none"
// parameters[0] = correções separadas por ";":
//   "ambientes_nao_colocados" — apaga ambientes sem localização (sobras de listas; aparecem em tabelas sem área)
//   "paredes_curtas"          — apaga paredes com menos de 5 cm sem portas/janelas hospedadas
// Tudo numa única transação "Skill: corrigir modelo" (desfazível com Ctrl+Z). Não salva.
// O que depende de decisão do arquiteto (ambientes não fechados, marcas de tipo repetidas, linhas de separação sobrepostas,
// CAD importado) fica só no relatório de 03_conferir_modelo.cs.
var pedidas = (parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "").ToLower().Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
if (pedidas.Count == 0) return "Nada pedido. Opções: ambientes_nao_colocados; paredes_curtas";
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
    t.Commit();
}
log.AppendLine("Feito numa transação só (Ctrl+Z desfaz). O arquivo não foi salvo.");
return log.ToString();
