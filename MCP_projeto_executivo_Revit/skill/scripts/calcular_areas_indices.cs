// =====================================================================
// SKILL EXECUTIVO REVIT — Cálculo de áreas e índices do carimbo
// Execução: MCP do Revit › send_code_to_revit (transactionMode: "none")
// Versão 0.1 · 13/11/2026 · validado no projeto de referência (bate com o PDF emitido)
// ---------------------------------------------------------------------
// Fonte das áreas: REGIÕES PREENCHIDAS desenhadas na vista de implantação
// executiva (padrão do escritório), identificadas pelo FINAL do nome do tipo
// (independe do prefixo do escritório, ex.: "VZ-", "CBC_"):
//   ... Área Construída Pavimento Térreo   → PAV. TÉRREO
//   ... Área Construída Pavimento Superior → PAV. SUPERIOR
//   ... Área Ocupada (opcional)            → ÁREA OCUPADA (senão = maior pavimento)
//   ... Área Permeável                     → ÁREA PERMEÁVEL
//   ... Área da Piscina                    → ÁREA PISCINA
// Legendas são ignoradas (só conta a região que pertence à vista de referência).
//
// parameters[0] = nome da vista de referência (padrão "IMPLANTAÇÃO")
// parameters[1] = "gravar" para gravar nas Informações do projeto; qualquer outro valor = só simular
//
// Grava (parâmetros do arquivo PARAMETROS_COMPARTILHADOS_SKILL.txt):
//   PAV. TÉRREO · PAV. SUPERIOR · ÁREA CONSTRUÍDA · ÁREA OCUPADA · ÁREA PISCINA · ÁREA PERMEÁVEL
//   TAXA DE OCUPAÇÃO (%) · TAXA DE PERMEABILIDADE (%) · ÍNDICE DE APROVEITAMENTO
// Lê: Área do lote
// =====================================================================

var nomeVista = (parameters != null && parameters.Length > 0 && !string.IsNullOrWhiteSpace(parameters[0] as string)) ? (string)parameters[0] : "IMPLANTAÇÃO";
var gravar = parameters != null && parameters.Length > 1 && (parameters[1] as string) == "gravar";
var log = new System.Text.StringBuilder();

// 1. Vista de referência (nunca legenda)
var vista = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>()
    .FirstOrDefault(v => !v.IsTemplate && v.ViewType != ViewType.Legend && v.Name == nomeVista);
if (vista == null) return "ERRO: vista de referência '" + nomeVista + "' não encontrada (legendas são ignoradas).";

// 2. Área do lote
var pi = document.ProjectInformation;
var pLote = pi.LookupParameter("Área do lote");
if (pLote == null) return "ERRO: parâmetro 'Área do lote' não existe neste arquivo.";
double lote = UnitUtils.ConvertFromInternalUnits(pLote.AsDouble(), UnitTypeId.SquareMeters);
if (lote <= 0) return "ERRO: 'Área do lote' está vazia ou zero — preencha na abertura do projeto.";

// 3. Somar regiões por tipo
Func<FilledRegion, double> m2 = r => {
    try {
        var solido = GeometryCreationUtilities.CreateExtrusionGeometry(r.GetBoundaries(), XYZ.BasisZ, 1.0);
        return UnitUtils.ConvertFromInternalUnits(solido.Volume, UnitTypeId.CubicFeet) * 0.09290304; // ft³ de 1 pé de altura → m²
    } catch { return 0; }
};
var regras = new Dictionary<string, string> {
    { "Área Construída Pavimento Térreo",   "TERREO" },
    { "Área Construída Pavimento Superior", "SUPERIOR" },
    { "Área Ocupada",                       "OCUPADA" },
    { "Área Permeável",                     "PERMEAVEL" },
    { "Área da Piscina",                    "PISCINA" }
};
var soma = regras.Values.ToDictionary(k => k, k => 0.0);
var cont = regras.Values.ToDictionary(k => k, k => 0);
foreach (var r in new FilteredElementCollector(document, vista.Id).OfClass(typeof(FilledRegion)).Cast<FilledRegion>()) {
    var nomeTipo = document.GetElement(r.GetTypeId()).Name.Replace("_", "-");
    foreach (var kv in regras) if (nomeTipo.EndsWith(kv.Key, StringComparison.OrdinalIgnoreCase)) { soma[kv.Value] += m2(r); cont[kv.Value]++; }
}

// 4. Índices
double terreo = soma["TERREO"], superior = soma["SUPERIOR"];
double construida = terreo + superior;
double ocupada = cont["OCUPADA"] > 0 ? soma["OCUPADA"] : Math.Max(terreo, superior);
double to = ocupada / lote * 100.0, tp = soma["PERMEAVEL"] / lote * 100.0, ia = construida / lote;

log.AppendLine("Vista de referência: " + nomeVista + " | lote " + lote.ToString("0.00") + " m²");
log.AppendLine("Térreo " + terreo.ToString("0.00") + " (" + cont["TERREO"] + " reg.) | Superior " + superior.ToString("0.00") + " (" + cont["SUPERIOR"] + " reg.)");
log.AppendLine("Construída " + construida.ToString("0.00") + " | Ocupada " + ocupada.ToString("0.00") + (cont["OCUPADA"] > 0 ? " (regiões próprias)" : " (maior pavimento)"));
log.AppendLine("Piscina " + soma["PISCINA"].ToString("0.00") + " | Permeável " + soma["PERMEAVEL"].ToString("0.00"));
log.AppendLine("TO " + to.ToString("0.00") + "% | TP " + tp.ToString("0.00") + "% | IA " + ia.ToString("0.00"));
if (cont["TERREO"] == 0) log.AppendLine("AVISO: nenhuma região de área construída do térreo na vista.");
if (cont["PERMEAVEL"] == 0) log.AppendLine("AVISO: nenhuma região de área permeável na vista.");

// 5. Gravar
if (!gravar) return "SIMULAÇÃO (nada gravado)\n" + log.ToString();
var faltando = new List<string>();
using (var t = new Transaction(document, "Skill: calcular áreas e índices")) {
    t.Start();
    Action<string, double> A = (n, v) => { var p = pi.LookupParameter(n); if (p == null) faltando.Add(n); else p.Set(UnitUtils.ConvertToInternalUnits(v, UnitTypeId.SquareMeters)); };
    Action<string, double> N = (n, v) => { var p = pi.LookupParameter(n); if (p == null) faltando.Add(n); else p.Set(Math.Round(v, 2)); };
    A("PAV. TÉRREO", terreo); A("PAV. SUPERIOR", superior); A("ÁREA CONSTRUÍDA", construida); A("ÁREA OCUPADA", ocupada);
    A("ÁREA PISCINA", soma["PISCINA"]); A("ÁREA PERMEÁVEL", soma["PERMEAVEL"]);
    N("TAXA DE OCUPAÇÃO", to); N("TAXA DE PERMEABILIDADE", tp); N("ÍNDICE DE APROVEITAMENTO", ia);
    t.Commit();
}
return "GRAVADO" + (faltando.Count > 0 ? " — parâmetros ausentes: " + string.Join(", ", faltando) : "") + "\n" + log.ToString();
