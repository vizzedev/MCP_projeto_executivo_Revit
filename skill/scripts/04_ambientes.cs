// SKILL EXECUTIVO REVIT — Fase 4: ambientes de portas e janelas + consistência dos nomes de ambientes
// send_code_to_revit · transactionMode "none"
// parameters[0] = modo: "propor" (só relatório, padrão) | "aplicar" (preenche os vazios) | "aplicar_tudo" (sobrescreve, só com ordem do arquiteto)
//
// AMBIENTE das portas/janelas (vai para a coluna AMBIENTE das tabelas, que agrupam por marca de tipo):
//   para cada tipo, junta os ambientes onde ele está instalado e escreve um texto único para todas as instâncias do tipo,
//   ex.: "LAVANDERIA, CIRCULAÇÃO E QUARTOS". Regras (extraídas do projeto de referência):
//   - porta: vale o ambiente que não é circulação nem área externa; entre dois ambientes internos, o mais reservado
//     (banheiro > quarto > serviço > cozinha > demais); se dá para fora, "<ambiente>/ÁREA EXTERNA";
//   - janela: vale o ambiente interno; sem ambiente (pavimento sem ambientes) → "FACHADA <LADO>[ - <PAVIMENTO>]";
//   - agrupamento: 2+ quartos/suítes/apartamentos → "QUARTOS"; 2+ banheiros/WCs/lavabos → "WC's"; CIRC… → "CIRCULAÇÃO".
//   Parâmetro procurado: o primeiro parâmetro de texto de instância cujo nome contém "AMBIENTE" e que esteja em uso nas tabelas
//   (padrão do template: "AMBIENTE DA PORTA" e ".AMBIENTE").
// IMPORTANTE: o arquiteto costuma refinar esses textos (ex.: "HALL DE ENTRADA"); por isso o padrão é propor e, ao aplicar,
// preencher só os vazios. Concordância medida no projeto de referência: ~1/3 dos tipos idênticos; o resto é escolha de nome.
// Nomes de ambientes: aponta variações do mesmo tipo de ambiente (ex.: "APT 02" × "APARTAMENTO 01") e nomes em minúsculas.
var modo = (parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "").Trim().ToLower(); if (modo == "") modo = "propor";
bool aplicar = modo.StartsWith("aplicar"), sobrescrever = modo == "aplicar_tudo";
var log = new System.Text.StringBuilder();
Func<Autodesk.Revit.DB.Architecture.Room, string> Nome = r => r == null ? "" : (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").Trim().ToUpper();
var externos = new[] { "EXTERNA", "CALÇADA", "CALCADA", "SOLARIUM", "QUINTAL", "JARDIM", "VARANDA", "PISCINA", "DECK", "ESTACIONAMENTO", "GARAGEM DESCOBERTA", "PRAINHA" };
var circulacao = new[] { "CIRC", "CORREDOR", "HALL" };
Func<string, bool> Ext = n => n == "" || externos.Any(e => n.Contains(e));
Func<string, bool> Circ = n => circulacao.Any(c => n.StartsWith(c) || n.Contains(" " + c));
Func<string, string> Grupo = n => {
    if (n.StartsWith("WC") || n.Contains("BANHEIRO") || n.Contains("LAVABO") || n.StartsWith("BWC")) return "WC";
    if (n.StartsWith("APARTAMENTO") || n.StartsWith("APT") || n.StartsWith("QUARTO") || n.StartsWith("DORMIT") || n.StartsWith("SUÍTE") || n.StartsWith("SUITE")) return "QUARTO";
    if (Circ(n)) return "CIRC";
    return n;
};
Func<List<string>, string> Texto = nomes => {
    var partes = new List<string>();
    // "QUARTO/ÁREA EXTERNA": agrupa pela parte interna e devolve o sufixo externo
    foreach (var porSufixo in nomes.Where(n => n != "").GroupBy(n => n.Contains("/") ? n.Substring(n.IndexOf('/')) : "")) {
        foreach (var g in porSufixo.Select(n => n.Contains("/") ? n.Substring(0, n.IndexOf('/')) : n).GroupBy(Grupo)) {
            var distintos = g.Distinct().ToList(); string parte;
            if (g.Key == "QUARTO") parte = distintos.Count > 1 ? "QUARTOS" : distintos[0];
            else if (g.Key == "WC") parte = distintos.Count > 1 ? "WC's" : distintos[0];
            else if (g.Key == "CIRC") parte = "CIRCULAÇÃO";
            else parte = distintos[0];
            partes.Add(parte + porSufixo.Key);
        }
    }
    partes = partes.Distinct().OrderBy(p => p == "QUARTOS" || p == "WC's" ? 1 : 0).ThenBy(p => p).ToList();
    return partes.Count <= 1 ? (partes.Count == 1 ? partes[0] : "") : string.Join(", ", partes.Take(partes.Count - 1)) + " E " + partes.Last();
};
// frente do lote (pela CALÇADA, como nas fachadas) para nomear janelas/portas sem ambiente pela fachada
var paredesBB = new FilteredElementCollector(document).OfClass(typeof(Wall)).ToElements().Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();
XYZ dirFrente = null;
if (paredesBB.Count > 0) {
    double cx = (paredesBB.Min(b => b.Min.X) + paredesBB.Max(b => b.Max.X)) / 2, cy = (paredesBB.Min(b => b.Min.Y) + paredesBB.Max(b => b.Max.Y)) / 2;
    double lx = paredesBB.Max(b => b.Max.X) - paredesBB.Min(b => b.Min.X), ly = paredesBB.Max(b => b.Max.Y) - paredesBB.Min(b => b.Min.Y);
    var calc = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().FirstOrDefault(r => r.Location != null && r.Area > 0 && Nome(r).Contains("CALÇADA"));
    if (calc != null) { var pc = ((LocationPoint)calc.Location).Point; double dx = (pc.X - cx) / lx, dy = (pc.Y - cy) / ly; dirFrente = Math.Abs(dx) > Math.Abs(dy) ? new XYZ(Math.Sign(dx), 0, 0) : new XYZ(0, Math.Sign(dy), 0); }
}
Func<FamilyInstance, string> Fachada = fi => {
    if (dirFrente == null) return "";
    var f = new XYZ(fi.FacingOrientation.X, fi.FacingOrientation.Y, 0); if (f.GetLength() < 1e-6) return "";
    f = f.Normalize(); var dir = new XYZ(dirFrente.Y, -dirFrente.X, 0);
    string lado = f.DotProduct(dirFrente) > 0.7 ? "FRONTAL" : f.DotProduct(-dirFrente) > 0.7 ? "POSTERIOR" : f.DotProduct(-dir) > 0.7 ? "LATERAL DIREITA" : f.DotProduct(dir) > 0.7 ? "LATERAL ESQUERDA" : "";
    if (lado == "") return "";
    var nivel = document.GetElement(fi.LevelId) as Level; var pav = nivel == null ? "" : nivel.Name.ToUpper().Replace("PAV.", "").Replace("N.A", "").Replace("N.O", "").Trim();
    return "FACHADA " + lado + (pav != "" && !pav.Contains("TERREO") && !pav.Contains("TÉRREO") ? " - " + pav : "");
};
// ambiente de uma porta/janela
Func<FamilyInstance, bool, string> AmbienteDe = (fi, porta) => {
    var lados = new[] { Nome(fi.FromRoom), Nome(fi.ToRoom), Nome(fi.Room) }.Where(n => n != "").Distinct().ToList();
    if (lados.Count == 0) return Fachada(fi);                                   // sem ambiente (ex.: pavimento sem ambientes): nome da fachada
    var internos = lados.Where(n => !Ext(n)).ToList(); var externosL = lados.Where(n => Ext(n)).ToList();
    if (!porta) return internos.Count > 0 ? internos[0] : lados[0];
    var util = internos.Where(n => !Circ(n)).ToList();
    Func<string, int> Pri = n => Grupo(n) == "WC" ? 0 : Grupo(n) == "QUARTO" ? 1 : new[] { "LAVANDERIA", "SERVIÇO", "SERVICO", "DEPÓSITO", "DEPOSITO", "DESPENSA" }.Any(k => n.Contains(k)) ? 2 : new[] { "COZINHA", "GOURMET" }.Any(k => n.Contains(k)) ? 3 : 4;
    var dentro = util.Count > 0 ? util.OrderBy(Pri).First() : internos.Count > 0 ? internos[0] : lados[0];
    // porta que dá para fora: "<ambiente>/ÁREA EXTERNA", como o arquiteto escreve ("QUARTOS/ÁREA EXTERNA")
    if (internos.Count > 0 && externosL.Count > 0) return dentro + "/ÁREA EXTERNA";
    return dentro;
};
int alterados = 0, iguais = 0, diferentes = 0, preenchidosVazios = 0;
using (var t = new Transaction(document, "Skill: ambientes de portas e janelas")) {
    if (aplicar) t.Start();
    foreach (var cat in new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows }) {
        bool porta = cat == BuiltInCategory.OST_Doors;
        var inst = new FilteredElementCollector(document).OfCategory(cat).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
        if (inst.Count == 0) continue;
        // parâmetro AMBIENTE em uso: o que tem mais valores preenchidos (ou o primeiro que existir)
        var candidatos = inst.SelectMany(fi => fi.Parameters.Cast<Parameter>()).Where(p => !p.IsReadOnly && p.StorageType == StorageType.String && p.Definition.Name.ToUpper().Contains("AMBIENTE")).Select(p => p.Definition.Name).Distinct().ToList();
        if (candidatos.Count == 0) { log.AppendLine((porta ? "Portas" : "Janelas") + ": nenhum parâmetro AMBIENTE de texto — pulado."); continue; }
        var nomePar = candidatos.OrderByDescending(n => inst.Count(fi => !string.IsNullOrWhiteSpace(fi.LookupParameter(n)?.AsString()))).ThenBy(n => n.Length).First();
        log.AppendLine((porta ? "PORTAS" : "JANELAS") + " — parâmetro \"" + nomePar + "\"");
        foreach (var g in inst.GroupBy(fi => fi.Symbol.Id)) {
            var marca = (g.First().Symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK)?.AsString() ?? "").Trim();
            var proposta = Texto(g.Select(fi => AmbienteDe(fi, porta)).ToList());
            var atuais = g.Select(fi => (fi.LookupParameter(nomePar)?.AsString() ?? "").Trim()).Distinct().ToList();
            var atual = string.Join(" / ", atuais.Where(a => a != ""));
            string situacao;
            if (atual == "") situacao = "vazio";
            else if (atual.ToUpper() == proposta.ToUpper()) { situacao = "igual"; iguais++; }
            else { situacao = "diferente"; diferentes++; }
            if (aplicar && proposta != "") foreach (var fi in g) {
                var p = fi.LookupParameter(nomePar); if (p == null || p.IsReadOnly) continue;
                var v = (p.AsString() ?? "").Trim();
                if (v == "" || sobrescrever) { if (v != proposta) { p.Set(proposta); alterados++; if (v == "") preenchidosVazios++; } }
            }
            log.AppendLine("  " + (marca == "" ? "(sem marca)" : marca) + " · " + g.Count() + " un. · proposta \"" + proposta + "\"" + (situacao == "igual" ? " = atual" : situacao == "vazio" ? " (estava vazio)" : " · atual \"" + atual + "\""));
        }
    }
    if (aplicar) t.Commit();
}
// consistência dos nomes de ambientes
var nomesAmb = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0)
    .Select(r => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").Trim()).ToList();
var avisosNome = new List<string>();
var minus = nomesAmb.Where(n => n != n.ToUpper()).Distinct().ToList(); if (minus.Count > 0) avisosNome.Add("Nomes com minúsculas (o padrão do carimbo/tabelas é caixa alta): " + string.Join(", ", minus));
var quartos = nomesAmb.Where(n => Grupo(n.ToUpper()) == "QUARTO" && !n.ToUpper().StartsWith("SUÍTE") && !n.ToUpper().StartsWith("SUITE")).Distinct().ToList();
var prefixos = quartos.Select(n => n.ToUpper().Split(' ')[0]).Distinct().ToList();
if (prefixos.Count > 1) avisosNome.Add("Quartos com nomes em padrões diferentes: " + string.Join(", ", quartos) + " → escolha um (ex.: \"APARTAMENTO 01, 02, 03\").");
var wcs = nomesAmb.Where(n => Grupo(n.ToUpper()) == "WC").Distinct().ToList();
var wcAbrev = wcs.Where(n => n.ToUpper().Contains(" APT ")).ToList(); var wcLongo = wcs.Where(n => n.ToUpper().Contains("APARTAMENTO")).ToList();
if (wcAbrev.Count > 0 && wcLongo.Count > 0) avisosNome.Add("WCs com abreviações diferentes: " + string.Join(", ", wcAbrev.Concat(wcLongo)));
var sb = new System.Text.StringBuilder();
sb.AppendLine("FASE 4 — AMBIENTES (" + (aplicar ? (sobrescrever ? "aplicado, sobrescrevendo" : "aplicado só nos vazios") : "proposta, nada alterado") + ")");
sb.Append(log.ToString());
sb.AppendLine("Resumo: " + iguais + " tipos iguais ao atual, " + diferentes + " diferentes" + (aplicar ? ", " + alterados + " instâncias alteradas (" + preenchidosVazios + " estavam vazias)" : ""));
sb.AppendLine(avisosNome.Count == 0 ? "Nomes de ambientes: consistentes." : "NOMES DE AMBIENTES\n  • " + string.Join("\n  • ", avisosNome));
return sb.ToString();
