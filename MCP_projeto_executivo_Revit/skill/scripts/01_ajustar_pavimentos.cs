// SKILL EXECUTIVO REVIT — Fase 1: ajustar o projeto ao número de pavimentos
// send_code_to_revit · transactionMode "none"
// parameters[0] = número de pavimentos (1 ou 2)
// parameters[1] = nome da família de carimbo da proposta (padrão "VZ-Prancha A1 Proposta Layout")
// Regra: uma folha de proposta de layout por pavimento. Remove as folhas de proposta excedentes
// (só se estiverem vazias — se tiverem vistas, não remove e avisa).
if (document.IsFamilyDocument || document.PathName.EndsWith(".rte", StringComparison.OrdinalIgnoreCase)) return "ERRO: documento ativo não é um projeto (.rvt).";
int pav = (parameters != null && parameters.Length > 0) ? int.Parse((string)parameters[0]) : 1;
var carimbo = (parameters != null && parameters.Length > 1 && !string.IsNullOrWhiteSpace(parameters[1] as string)) ? (string)parameters[1] : "VZ-Prancha A1 Proposta Layout";
var folhas = new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
    .Where(s => new FilteredElementCollector(document, s.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).Cast<FamilyInstance>().Any(f => f.Symbol.FamilyName == carimbo))
    .OrderBy(s => s.SheetNumber).ToList();
var log = new List<string>();
using (var t = new Transaction(document, "Skill: folhas de proposta por pavimento")) {
    t.Start();
    for (int i = 0; i < folhas.Count; i++) {
        var s = folhas[i];
        if (i < pav) { var novo = (i + 1) + "/" + pav; if (s.SheetNumber != novo) { var antigo = s.SheetNumber; s.SheetNumber = "tmp_" + i; log.Add("renumerar " + antigo + " → " + novo); } continue; }
        if (s.GetAllViewports().Count > 0) { log.Add("MANTIDA " + s.SheetNumber + " (tem vistas)"); continue; }
        log.Add("removida " + s.SheetNumber + " (vazia)"); document.Delete(s.Id);
    }
    // segunda passada: números finais (evita conflito de número duplicado)
    for (int i = 0; i < Math.Min(pav, folhas.Count); i++) { var s = folhas[i]; if (s.IsValidObject) s.SheetNumber = (i + 1) + "/" + pav; }
    t.Commit();
}
return "Pavimentos: " + pav + " | folhas de proposta encontradas: " + folhas.Count + "\n  " + string.Join("\n  ", log);
