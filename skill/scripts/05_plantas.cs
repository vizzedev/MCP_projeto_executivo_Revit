// SKILL EXECUTIVO REVIT — Fase 5: plantas do caderno (garante que existem e confere o conteúdo)
// send_code_to_revit · transactionMode "none"
// parameters[0] = prefixo ("" no uso real; "SKILL " em teste: trabalha em duplicatas "SKILL <nome>" das plantas existentes)
// parameters[1] = prefixo dos modelos de vista e tipos do escritório (padrão "VZ-"; os tipos de planta são localizados por trecho do nome)
//
// O template master já traz as plantas; a skill só cria a que faltar (tipo + modelo + nível + escala do escritório),
// sem recorte (como o arquiteto faz — a vista mostra o lote inteiro).
// Por pavimento com ambientes ("PAV. X N.A"): EXECUTIVO, PONTOS DE TOMADA, ILUMINAÇÃO E INTERRUPTORES, FORRO, PAGINAÇÃO DE PISO.
// Plantas complementares (checklist executivo), só quando a equipe modelou os pontos naquele pavimento:
//   AR CONDICIONADO (equipamentos mecânicos), HIDRO E GÁS (peças hidráulicas, acessórios de tubulação, "GÁS"),
//   ILUMINAÇÃO EXTERNA (no pavimento mais baixo, luminárias fora dos ambientes internos: jardim, arandelas, balizadores).
//   Modelo de vista: o do escritório com esse nome; se não houver, o da planta executiva / pontos elétricos (avisa).
// Uma vez: COBERTURA EXECUTIVO (nível com "COBERTURA") e IMPLANTAÇÃO (nível mais alto).
// Relatório: conteúdo de cada planta (paredes, tomadas, luminárias, forros, pisos) e pavimentos sem ambientes (vistas não usadas).
var prefixo = parameters.Length > 0 ? ((parameters[0] as string) ?? "") : "";
var pm = parameters.Length > 1 && !string.IsNullOrWhiteSpace(parameters[1] as string) ? (string)parameters[1] : "VZ-";
var log = new System.Text.StringBuilder();
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
var vistas = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().Where(v => !v.IsTemplate).ToList();
var modelos = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate).ToList();
var tipos = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList();

// especificação: nome, família, trecho do tipo, modelo, nível, escala, categoria-conteúdo (para conferir se está vazia)
var specs = new List<Tuple<string, ViewFamily, string, string, Level, int, BuiltInCategory>>();
foreach (var nv in niveis.Where(l => l.Name.ToUpper().StartsWith("PAV.") && l.Name.ToUpper().Contains("N.A"))) {
    var pav = nv.Name.Substring(4).Replace("N.A", "").Trim().ToUpper();           // "PAV. TERREO N.A" → "TERREO"
    bool temAmbientes = rooms.Any(r => r.LevelId == nv.Id);
    if (!temAmbientes) { log.AppendLine("Pavimento " + pav + ": sem ambientes — plantas dele não entram no caderno."); continue; }
    specs.Add(Tuple.Create("PAVIMENTO " + pav + " EXECUTIVO", ViewFamily.FloorPlan, "PLANTA BAIXA EXECUTIVO", pm + "PLANTA BAIXA EXECUTIVO", nv, 75, BuiltInCategory.OST_Walls));
    specs.Add(Tuple.Create("PAVIMENTO " + pav + " PONTOS DE TOMADA", ViewFamily.FloorPlan, "PONTOS ELÉTRICOS", pm + "PLANTA BAIXA PONTOS ELÉTRICOS", nv, 75, BuiltInCategory.OST_ElectricalFixtures));
    specs.Add(Tuple.Create("PAVIMENTO " + pav + " ILUMINAÇÃO E INTERRUPTORES", ViewFamily.CeilingPlan, "FORRO", pm + "PLANTA DE FORRO LUMINOTÉCNICO", nv, 75, BuiltInCategory.OST_LightingFixtures));
    specs.Add(Tuple.Create("PAV. " + pav + " FORRO", ViewFamily.CeilingPlan, "FORRO", pm + "PLANTA DE FORRO", nv, 75, BuiltInCategory.OST_Ceilings));
    specs.Add(Tuple.Create("PAVIMENTO " + pav + " PAGINAÇÃO DE PISO", ViewFamily.FloorPlan, "PAGINAÇÃO DE PISO", pm + "PLANTA BAIXA PAGINAÇÃO DE PISO", nv, 75, BuiltInCategory.OST_Floors));
    // complementares: só com pontos modelados neste pavimento
    Func<BuiltInCategory[], Func<Element, bool>, int> NoNivel = (cs, filtro) => cs.Sum(bc => new FilteredElementCollector(document).OfCategory(bc).WhereElementIsNotElementType().ToElements()
        .Count(e => { var b = e.get_BoundingBox(null); if (b == null) return false; var prox = niveis.Where(l => l.Elevation > nv.Elevation + 1.0 / 0.3048).Select(l => l.Elevation).DefaultIfEmpty(double.MaxValue).Min();
            double zc = (b.Min.Z + b.Max.Z) / 2; return zc >= nv.Elevation - 0.5 / 0.3048 && zc < prox - 0.3 / 0.3048 && (filtro == null || filtro(e)); }));
    if (NoNivel(new[] { BuiltInCategory.OST_MechanicalEquipment }, null) > 0)
        specs.Add(Tuple.Create("PAVIMENTO " + pav + " AR CONDICIONADO", ViewFamily.FloorPlan, "AR CONDICIONADO", pm + "PLANTA BAIXA AR CONDICIONADO|" + pm + "PLANTA BAIXA EXECUTIVO", nv, 75, BuiltInCategory.OST_MechanicalEquipment));
    if (NoNivel(new[] { BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_PipeAccessory }, null) > 0)
        specs.Add(Tuple.Create("PAVIMENTO " + pav + " HIDRO E GÁS", ViewFamily.FloorPlan, "HIDRO", pm + "PLANTA BAIXA HIDRO E GÁS|" + pm + "PLANTA BAIXA PONTOS ELÉTRICOS", nv, 75, BuiltInCategory.OST_PlumbingFixtures));
    bool maisBaixo = !niveis.Any(l => l.Elevation < nv.Elevation - 1.0 / 0.3048 && l.Name.ToUpper().StartsWith("PAV.") && rooms.Any(r => r.LevelId == l.Id));
    if (maisBaixo && NoNivel(new[] { BuiltInCategory.OST_LightingFixtures }, e => { var b = e.get_BoundingBox(null); var c = new XYZ((b.Min.X + b.Max.X) / 2, (b.Min.Y + b.Max.Y) / 2, nv.Elevation + 1.0 / 0.3048);
            return !rooms.Any(r => { var nn = (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").ToUpper(); if (nn.Contains("CALÇADA") || nn.Contains("JARDIM") || nn.Contains("QUINTAL") || nn.Contains("EXTERNA")) return false; try { return r.IsPointInRoom(c); } catch { return false; } }); }) > 0)
        specs.Add(Tuple.Create("PAVIMENTO " + pav + " ILUMINAÇÃO EXTERNA", ViewFamily.FloorPlan, "ILUMINAÇÃO", pm + "PLANTA BAIXA ILUMINAÇÃO EXTERNA|" + pm + "PLANTA BAIXA PONTOS ELÉTRICOS", nv, 75, BuiltInCategory.OST_LightingFixtures));
}
var nCob = niveis.FirstOrDefault(l => l.Name.ToUpper().Contains("COBERTURA"));
if (nCob != null) specs.Add(Tuple.Create("COBERTURA EXECUTIVO", ViewFamily.FloorPlan, "COBERTURA", pm + "PLANTA BAIXA COBERTURA", nCob, 75, BuiltInCategory.OST_Roofs));
else log.AppendLine("⚠ Sem nível COBERTURA — planta de cobertura não criada.");
specs.Add(Tuple.Create("IMPLANTAÇÃO", ViewFamily.FloorPlan, "IMPLANTAÇÃO", pm + "PLANTA BAIXA IMPLANTAÇÃO", niveis.Last(), 100, BuiltInCategory.OST_SiteProperty));

using (var t = new Transaction(document, "Skill: plantas do caderno")) {
    t.Start();
    foreach (var s in specs) {
        var nome = prefixo + s.Item1;
        var v = vistas.FirstOrDefault(x => x.Name == nome);
        string acao = "existente";
        if (v == null && prefixo != "") {   // teste: duplica a planta original
            var base_ = vistas.FirstOrDefault(x => x.Name == s.Item1);
            if (base_ != null) { v = (ViewPlan)document.GetElement(base_.Duplicate(ViewDuplicateOption.WithDetailing)); v.Name = nome; acao = "duplicada da original"; }
        }
        if (v == null) {
            var tipo = tipos.Where(x => x.ViewFamily == s.Item2).OrderByDescending(x => x.Name.ToUpper().Contains(s.Item3)).FirstOrDefault();
            if (tipo == null) { log.AppendLine("⚠ " + nome + ": sem tipo de vista " + s.Item2); continue; }
            v = ViewPlan.Create(document, tipo.Id, s.Item5.Id); v.Name = nome;
            var nomesMd = s.Item4.Split('|'); var md = nomesMd.Select(nm => modelos.FirstOrDefault(x => x.Name == nm)).FirstOrDefault(x => x != null);
            if (md != null) { v.ViewTemplateId = md.Id; if (md.Name != nomesMd[0]) log.AppendLine("  ⚠ modelo '" + nomesMd[0] + "' não existe no template — usado '" + md.Name + "' (crie o modelo próprio para esta planta)"); }
            else log.AppendLine("  ⚠ modelo '" + nomesMd[0] + "' não encontrado");
            v.Scale = s.Item6; v.CropBoxActive = false;
            acao = "criada (tipo '" + tipo.Name + "')";
        }
        document.Regenerate();
        int qtd = new FilteredElementCollector(document, v.Id).OfCategory(s.Item7).WhereElementIsNotElementType().GetElementCount();
        if (s.Item7 == BuiltInCategory.OST_SiteProperty) qtd += new FilteredElementCollector(document, v.Id).OfCategory(BuiltInCategory.OST_Lines).WhereElementIsNotElementType().GetElementCount();
        string conteudo = s.Item7 == BuiltInCategory.OST_MechanicalEquipment ? "equipamentos de ar-condicionado" : s.Item7 == BuiltInCategory.OST_PlumbingFixtures ? "peças hidráulicas" : s.Item7 == BuiltInCategory.OST_ElectricalFixtures ? "tomadas/pontos" : s.Item7 == BuiltInCategory.OST_LightingFixtures ? "luminárias" : s.Item7 == BuiltInCategory.OST_Ceilings ? "forros" : s.Item7 == BuiltInCategory.OST_Floors ? "pisos" : s.Item7 == BuiltInCategory.OST_Roofs ? "telhados" : s.Item7 == BuiltInCategory.OST_SiteProperty ? "linhas de divisa/lote" : "paredes";
        log.AppendLine((qtd == 0 ? "⚠ " : "") + nome + " — " + acao + " | 1:" + v.Scale + " | " + qtd + " " + conteudo + (qtd == 0 ? (s.Item7 == BuiltInCategory.OST_SiteProperty ? " → VAZIA: rode 02_desenhar_lote.cs" : " → VAZIA: modelar antes de emitir") : ""));
    }
    t.Commit();
}
return log.ToString();
