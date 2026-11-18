// SKILL EXECUTIVO REVIT — Fase 3 (opcional): criar ambientes e propor os nomes pelo que está modelado dentro deles
// send_code_to_revit · transactionMode "none"
// Só quando a equipe PEDIR (a skill não modela por conta própria). Aprendido no piloto 2: o modelo chegou sem ambientes.
// parameters[0] = modo: "propor" (padrão; cria numa transação desfeita e só relata) | "aplicar" (cria e nomeia de verdade)
// parameters[1] = nomes escolhidos pelo arquiteto, "k=NOME;k=NOME" (k = número da linha do relatório "propor"); vazio = nomes propostos
// parameters[2] = níveis, separados por ";" (padrão: todos os "PAV. X N.A")
// Regras:
//   - cria um ambiente em cada circuito fechado ainda sem ambiente (PlanCircuit), na fase mais recente do projeto;
//   - se a altura de cálculo do nível não corta as paredes (base deslocada), avisa e para — rode antes 03_corrigir_modelo.cs "altura_calculo";
//   - o nome vem do mobiliário e das louças DO PRÓPRIO NÍVEL dentro do ambiente (ver Propor abaixo); repetidos ganham 01, 02…;
//   - numeração continua do maior número existente (01, 02…), térreo antes do superior;
//   - circuitos sem nada dentro: "CIRCULAÇÃO" se forem alongados (lado maior ≥ 2× o menor), senão "A DEFINIR" (a conferência acusa como sem nome);
//   - suíte × quarto, banho de qual suíte, jardim interno, depósito… são escolha do arquiteto: passe em parameters[1].
// Uma transação "Skill: criar ambientes" (Ctrl+Z desfaz). Não salva. Identificadores ficam para 02_proposta.cs / 06_anotar_planta.cs.
// --- projeto pelo caminho (regra 5): se o 1º parâmetro for "DOC:<caminho do .rvt>", trabalha nesse arquivo aberto e retira o parâmetro.
//     Use sempre que houver mais de um arquivo aberto no Revit: o "documento ativo" muda quando o usuário troca de janela.
if (parameters != null && parameters.Length > 0 && ((parameters[0] as string) ?? "").StartsWith("DOC:")) {
    var __caminho = ((string)parameters[0]).Substring(4).Trim(); Document __doc = null;
    foreach (Document __d in document.Application.Documents) if (!__d.IsLinked && string.Equals(__d.PathName, __caminho, StringComparison.OrdinalIgnoreCase)) __doc = __d;
    if (__doc == null) return "ERRO: o projeto não está aberto no Revit: " + __caminho;
    document = __doc; parameters = parameters.Skip(1).ToArray();
}
var modo = (parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "").Trim().ToLower(); if (modo == "") modo = "propor";
var escolhidos = new Dictionary<int, string>();
if (parameters.Length > 1) foreach (var par in ((parameters[1] as string) ?? "").Split(';')) { var i = par.IndexOf('='); int k; if (i > 0 && int.TryParse(par.Substring(0, i).Trim(), out k)) escolhidos[k] = par.Substring(i + 1).Trim().ToUpper(); }
var filtroNiveis = (parameters.Length > 2 ? ((parameters[2] as string) ?? "") : "").Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
Func<double, double> F = m => m / 0.3048;
Func<string, string> Norm = s => new string((s ?? "").Normalize(System.Text.NormalizationForm.FormD).Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToUpper();
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().Where(l => l.Name.ToUpper().StartsWith("PAV.") && l.Name.ToUpper().EndsWith("N.A") && (filtroNiveis.Count == 0 || filtroNiveis.Contains(l.Name))).OrderBy(l => l.Elevation).ToList();
if (niveis.Count == 0) return "ERRO: nenhum nível \"PAV. X N.A\" encontrado.";
var fase = document.Phases.Cast<Phase>().Last();
var cats = new[] { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_Casework, BuiltInCategory.OST_SpecialityEquipment, BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_FurnitureSystems };

// nome proposto pelo conteúdo (nomes de família sem acento, em maiúsculas)
Func<List<FamilyInstance>, double, double, string> Propor = (pecas, area, alongamento) => {
    var n = pecas.Select(fi => Norm(fi.Symbol.FamilyName + " " + fi.Symbol.Name)).ToList();
    Func<string[], bool> Tem = chaves => n.Any(x => chaves.Any(c => x.Contains(c)));
    bool bacia = Tem(new[] { "BACIA", "VASO SANIT" }), banho = Tem(new[] { "CHUVEIRO", "BOX", "BANHEIRA", "DUCHA" });
    bool lav = Tem(new[] { "TANQUE", "LAVA E SECA", "MAQUINA DE LAVAR", "LAVADORA", "SECADORA" });
    bool coz = Tem(new[] { "COOKTOP", "COOK TOP", "FOGAO", "CUBA COZINHA", "GELADEIRA", "REFRIGERADOR", "LAVA LOUCA", "ARMARIO INFERIOR COZINHA", "FORNO" });
    bool cama = Tem(new[] { "CAMA", "BELICHE", "BERCO" }), sofa = Tem(new[] { "SOFA" }), jantar = Tem(new[] { "MESA DE JANTAR", "MESA JANTAR" });
    bool escr = Tem(new[] { "CADEIRA ESCRITORIO", "COMPUTADOR", "ESCRIVANINHA" }), churr = Tem(new[] { "CHURRASQUEIRA" }), carro = Tem(new[] { "CARRO", "VEICULO", "AUTOMOVEL" });
    if (banho) return "BANHEIRO";                 // box/chuveiro bastam (a bacia pode estar fora do contorno, presa na parede)
    if (bacia) return "LAVABO";
    if (cama) return "QUARTO";
    if (lav && !coz) return "LAVANDERIA";
    if (carro) return "GARAGEM";
    if (coz && (sofa || jantar)) return sofa && jantar ? "ESTAR / JANTAR / COZINHA" : sofa ? "ESTAR / COZINHA" : "JANTAR / COZINHA";
    if (coz) return churr ? "COZINHA / GOURMET" : "COZINHA";
    if (churr) return "ÁREA GOURMET";
    if (sofa && jantar) return "ESTAR / JANTAR";
    if (sofa) return "ESTAR";
    if (jantar) return "JANTAR";
    if (escr) return "ESCRITÓRIO";
    if (pecas.Count == 0 && alongamento >= 2.0) return "CIRCULAÇÃO";
    return "A DEFINIR";
};

var log = new System.Text.StringBuilder();
var existentes = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().ToList();
int num = existentes.Select(r => { int v; return int.TryParse(r.Number, out v) ? v : 0; }).DefaultIfEmpty(0).Max();
var linhas = new List<Tuple<int, Autodesk.Revit.DB.Architecture.Room, string, string>>();   // k, ambiente, proposta, conteúdo
EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
document.Application.FailuresProcessing += __avisos;
try {
using (var t = new Transaction(document, "Skill: criar ambientes")) {
    t.Start();
    int k = 0;
    foreach (var l in niveis) {
        // a altura de cálculo precisa cortar as paredes (piloto 2: 0,00 com paredes de base +0,20…+0,35 m)
        var ws = new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>().Where(w => w.LevelId == l.Id && w.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING)?.AsInteger() == 1).ToList();
        double alt = l.get_Parameter(BuiltInParameter.LEVEL_ROOM_COMPUTATION_HEIGHT).AsDouble();
        Func<double, int> Cruza = h => ws.Count(w => { var b = w.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET)?.AsDouble() ?? 0; var a = w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0; return h >= b - F(0.001) && h <= b + a + F(0.001); });
        if (ws.Count > 0 && Cruza(alt) < Cruza(F(1.20)) * 0.8) { log.AppendLine(l.Name + ": a altura de cálculo (" + (alt * 0.3048).ToString("0.00") + " m) não corta as paredes — rode 03_corrigir_modelo.cs \"altura_calculo\" e repita. Nível pulado."); continue; }
        var topo = document.get_PlanTopology(l, fase);
        var novos = new List<Autodesk.Revit.DB.Architecture.Room>();
        foreach (PlanCircuit c in topo.Circuits) { if (c.IsRoomLocated) continue; try { novos.Add(document.Create.NewRoom(null, c)); } catch { } }
        document.Regenerate();
        foreach (var r in novos.Where(x => x.Area <= 0).ToList()) document.Delete(r.Id);   // circuitos degenerados
        novos = novos.Where(x => x.IsValidObject && x.Area > 0).OrderBy(x => -x.Area).ToList();
        var pecas = new List<FamilyInstance>();
        // peças do pavimento: pelo nível; as presas à face da parede (bacias, cubas) ficam sem nível — vão pela altura
        Func<FamilyInstance, bool> DoNivel = fi => { if (fi.LevelId == l.Id) return true; if (fi.LevelId != ElementId.InvalidElementId) return false;
            var b = fi.get_BoundingBox(null); return b != null && b.Min.Z >= l.Elevation - F(0.30) && b.Min.Z <= l.Elevation + F(2.00); };
        foreach (var cat in cats) pecas.AddRange(new FilteredElementCollector(document).OfCategory(cat).WhereElementIsNotElementType().OfType<FamilyInstance>().Where(fi => DoNivel(fi) && fi.Location is LocationPoint));
        log.AppendLine("== " + l.Name + ": " + novos.Count + " ambientes novos" + (novos.Count == 0 ? " (todos os circuitos fechados já têm ambiente)" : ""));
        foreach (var r in novos) {
            k++;
            // centro da peça (o ponto de inserção de louças fica na face da parede, fora do ambiente); senão, o ponto de inserção
            var dentro = pecas.Where(fi => { var b = fi.get_BoundingBox(null); var p = b != null ? (b.Min + b.Max) / 2 : ((LocationPoint)fi.Location).Point; return r.IsPointInRoom(new XYZ(p.X, p.Y, l.Elevation + F(1.0))); }).ToList();
            var bb = r.get_BoundingBox(null); double dx = bb.Max.X - bb.Min.X, dy = bb.Max.Y - bb.Min.Y;
            var proposta = Propor(dentro, r.Area * 0.092903, Math.Max(dx, dy) / Math.Max(Math.Min(dx, dy), F(0.1)));
            var conteudo = string.Join("; ", dentro.Select(fi => fi.Symbol.FamilyName).GroupBy(s => s).Select(g => g.Key + (g.Count() > 1 ? " x" + g.Count() : "")));
            linhas.Add(Tuple.Create(k, r, proposta, conteudo));
        }
    }
    // nomes finais: escolha do arquiteto > proposta; repetidos (QUARTO, BANHEIRO…) ganham 01, 02…
    var finais = linhas.ToDictionary(x => x.Item1, x => escolhidos.ContainsKey(x.Item1) ? escolhidos[x.Item1] : x.Item3);
    foreach (var g in finais.Where(x => !escolhidos.ContainsKey(x.Key) && x.Value != "A DEFINIR").GroupBy(x => x.Value).Where(g => g.Count() > 1).ToList()) {
        int i = 0; foreach (var kv in g.OrderBy(x => x.Key).ToList()) finais[kv.Key] = kv.Value + " " + (++i).ToString("00");
    }
    foreach (var x in linhas) {
        var r = x.Item2; num++;
        r.get_Parameter(BuiltInParameter.ROOM_NAME).Set(finais[x.Item1]); r.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set(num.ToString("00"));
        var c = (r.Location as LocationPoint).Point;
        log.AppendLine("  " + x.Item1 + ") " + num.ToString("00") + " " + finais[x.Item1] + (escolhidos.ContainsKey(x.Item1) ? " (escolhido)" : "") + " | " + (r.Area * 0.092903).ToString("0.00") + " m² | " + r.Level.Name
            + " | centro " + (c.X * 0.3048).ToString("0.0") + ", " + (c.Y * 0.3048).ToString("0.0") + " | " + (x.Item4 == "" ? "vazio" : x.Item4));
    }
    if (modo == "aplicar") { t.Commit(); log.AppendLine("Aplicado: " + linhas.Count + " ambientes criados e nomeados (Ctrl+Z desfaz). Não salvo."); }
    else { t.RollBack(); log.AppendLine("PROPOSTA (nada foi criado). Para aplicar: [\"aplicar\", \"k=NOME;k=NOME\"] com os nomes que o arquiteto trocar."); }
}
} finally { document.Application.FailuresProcessing -= __avisos; }
return log.ToString();
