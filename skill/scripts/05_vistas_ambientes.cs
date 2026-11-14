// SKILL EXECUTIVO REVIT — Fase 5: vistas por ambiente (iluminação e paginação)
// send_code_to_revit · transactionMode "none"
// parameters[0] = modo: "iluminacao" ou "paginacao"
// parameters[1] = prefixo do nome das vistas ("" no uso real; "SKILL " em teste)
// parameters[2] = ambientes separados por ";" — "NOME DO AMBIENTE" ou "NOME DO AMBIENTE=nome exibido"
//                 (o usuário escolhe a lista; para ambientes idênticos, informe só um e dê nome no plural)
// parameters[3] = escala (padrão: 25 iluminação, 50 paginação)
// parameters[4] = altura da vista em m (padrão: 2,75 iluminação, 2,90 paginação)
// parameters[5] = prefixo dos modelos de vista do escritório (padrão "VZ-")
//
// ILUMINAÇÃO: 1 elevação por ambiente; área molhada → parede com mais peças hidrossanitárias (bancada);
//             demais → parede com mais pontos de iluminação/elétrica
//             (luminárias, arandelas, interruptores, tomadas a até 0,40 m da parede); empate → parede maior.
//             Modelo "ELEVAÇÃO ILUMINAÇÃO". Nome = nome exibido.
// PAGINAÇÃO:  por ambiente → planta ampliada "PAGINAÇÃO DE PISO <nome>" (modelo "PLANTA BAIXA PAGINAÇÃO DE PISO"),
//             4 elevações "<nome> 01..04" (01 parede norte, 02 leste, 03 sul, 04 oeste; modelo "ELEVAÇÃO PAGINAÇÃO"),
//             2 isométricas "PAGINAÇÃO DE PISO <nome> 01/02" de cantos opostos (modelo "MODELAGEM FACHADA").
// Enquadramento: parede/ambiente + 0,15 m de cada lado (validado contra o projeto de referência: 21/21 vistas).
var modo = (parameters[0] as string ?? "").ToLower();
var prefixo = parameters.Length > 1 ? (parameters[1] as string ?? "") : "";
var lista = (parameters.Length > 2 ? parameters[2] as string : "").Split(';').Select(s => s.Trim()).Where(s => s != "").ToList();
bool ilum = modo.StartsWith("ilum");
int escala = parameters.Length > 3 && !string.IsNullOrWhiteSpace(parameters[3] as string) ? int.Parse((string)parameters[3]) : (ilum ? 25 : 50);
double altura = (parameters.Length > 4 && !string.IsNullOrWhiteSpace(parameters[4] as string) ? double.Parse(((string)parameters[4]).Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture) : (ilum ? 2.75 : 2.90)) / 0.3048;
var pm = parameters.Length > 5 && !string.IsNullOrWhiteSpace(parameters[5] as string) ? (string)parameters[5] : "VZ-";
Func<double,double> F = v => v / 0.3048; Func<double,string> M = v => (v * 0.3048).ToString("0.00");
var log = new System.Text.StringBuilder();

var modelos = new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Where(v => v.IsTemplate).ToList();
Func<string, View> Modelo = n => modelos.FirstOrDefault(v => v.Name == pm + n);
var tiposVista = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().ToList();
var tipoElev = tiposVista.Where(t => t.ViewFamily == ViewFamily.Elevation).OrderByDescending(t => t.Name.ToUpper().Contains("PAGINA")).First();
var tipoPlanta = tiposVista.Where(t => t.ViewFamily == ViewFamily.FloorPlan).OrderByDescending(t => t.Name.ToUpper().Contains("PAGINA")).First();
var tipo3D = tiposVista.First(t => t.ViewFamily == ViewFamily.ThreeDimensional);
var rooms = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Area > 0).ToList();
var catsPontos = new List<BuiltInCategory> { BuiltInCategory.OST_LightingFixtures, BuiltInCategory.OST_LightingDevices, BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_ElectricalEquipment };
var pontos = new FilteredElementCollector(document).WherePasses(new ElementMulticategoryFilter(catsPontos)).WhereElementIsNotElementType().ToElements()
    .Select(e => (e.Location as LocationPoint)?.Point).Where(p => p != null).ToList();
var hidros = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_PlumbingFixtures).WhereElementIsNotElementType().ToElements()
    .Select(e => { try { var b = e.get_BoundingBox(null); return b == null ? null : (b.Min + b.Max) / 2; } catch { return null; } }).Where(p => p != null).ToList();

// paredes do ambiente: chave → (direção de vista, comprimento); N = parede em y máx etc.
var lados = new[] { "N", "L", "S", "O" };
var dirVista = new Dictionary<string, XYZ> { { "N", -XYZ.BasisY }, { "L", -XYZ.BasisX }, { "S", XYZ.BasisY }, { "O", XYZ.BasisX } };

using (var t = new Transaction(document, "Skill: vistas por ambiente (" + modo + ")")) {
    t.Start();
    foreach (var item in lista) {
        var partes = item.Split('='); var nomeAmb = partes[0].Trim(); var exib = partes.Length > 1 ? partes[1].Trim() : nomeAmb;
        var room = rooms.FirstOrDefault(r => (r.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() ?? "").Trim().ToUpper() == nomeAmb.ToUpper());
        if (room == null) { log.AppendLine("NÃO ENCONTRADO: " + nomeAmb); continue; }
        var bb = room.get_BoundingBox(null); var nivel = room.Level;
        double x0 = bb.Min.X, x1 = bb.Max.X, y0 = bb.Min.Y, y1 = bb.Max.Y, zc = nivel.Elevation;
        var centro = new XYZ((x0 + x1) / 2, (y0 + y1) / 2, zc);
        var comp = new Dictionary<string, double> { { "N", x1 - x0 }, { "S", x1 - x0 }, { "L", y1 - y0 }, { "O", y1 - y0 } };

        // cria UMA elevação interna olhando para a parede "lado"
        // ajusta enquadramento (parede + 0,15 m de cada lado, altura informada) e profundidade de uma elevação interna
        Action<ViewSection, string, View> Configurar = (vs, nome, modelo) => {
            vs.Name = prefixo + nome; if (modelo != null) vs.ViewTemplateId = modelo.Id; vs.Scale = escala; vs.CropBoxActive = true;
            var cb = vs.CropBox; var inv = cb.Transform.Inverse;
            var cs = new List<XYZ>(); foreach (var xx in new[] { x0, x1 }) foreach (var yy in new[] { y0, y1 }) cs.Add(inv.OfPoint(new XYZ(xx, yy, zc)));
            cb.Min = new XYZ(cs.Min(p => p.X) - F(0.15), inv.OfPoint(new XYZ(centro.X, centro.Y, zc)).Y, cb.Min.Z);
            cb.Max = new XYZ(cs.Max(p => p.X) + F(0.15), inv.OfPoint(new XYZ(centro.X, centro.Y, zc + altura)).Y, cb.Max.Z);
            vs.CropBox = cb;
            var prof = cs.Max(p => -p.Z) + F(0.20);
            var pf = vs.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR); if (pf != null && !pf.IsReadOnly) pf.Set(prof);
        };
        var plantaNivel = new FilteredElementCollector(document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>().First(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan && v.GenLevel != null && v.GenLevel.Id == nivel.Id);
        // um marcador no centro do ambiente; cria as elevações pedidas (lado → nome) e apaga as que sobram
        Func<Dictionary<string, string>, View, List<ViewSection>> Elevacoes = (pedidas, modelo) => {
            var mk = ElevationMarker.CreateElevationMarker(document, tipoElev.Id, centro, escala);
            var criadas = new List<ViewSection>();
            for (int i = 0; i < 4; i++) {
                var v = mk.CreateElevation(document, plantaNivel.Id, i);
                var lado = lados.FirstOrDefault(l => v.ViewDirection.IsAlmostEqualTo(dirVista[l]));
                if (lado != null && pedidas.ContainsKey(lado)) { Configurar(v, pedidas[lado], modelo); criadas.Add(v); } else document.Delete(v.Id);
            }
            return criadas;
        };

        if (ilum) {
            // conta pontos perto de cada parede
            Func<XYZ, bool> NoAmb = p => p.X > x0 - F(0.2) && p.X < x1 + F(0.2) && p.Y > y0 - F(0.2) && p.Y < y1 + F(0.2) && p.Z > zc - F(0.5) && p.Z < zc + F(4);
            var perto = pontos.Where(NoAmb).ToList(); double tol = F(0.40);
            var cont = new Dictionary<string, int> {
                { "N", perto.Count(p => Math.Abs(p.Y - y1) < tol) }, { "S", perto.Count(p => Math.Abs(p.Y - y0) < tol) },
                { "L", perto.Count(p => Math.Abs(p.X - x1) < tol) }, { "O", perto.Count(p => Math.Abs(p.X - x0) < tol) } };
            // só peças DENTRO do ambiente (evita contar louças do banheiro vizinho)
            // áreas molhadas: parede com mais peças hidrossanitárias (bancada/louças) manda; depois pontos elétricos; depois a maior
            var ph = hidros.Where(p => room.IsPointInRoom(new XYZ(p.X, p.Y, zc + F(1.0)))).ToList(); double th = F(0.60);
            var hid = new Dictionary<string, int> {
                { "N", ph.Count(p => Math.Abs(p.Y - y1) < th) }, { "S", ph.Count(p => Math.Abs(p.Y - y0) < th) },
                { "L", ph.Count(p => Math.Abs(p.X - x1) < th) }, { "O", ph.Count(p => Math.Abs(p.X - x0) < th) } };
            var escolha = lados.OrderByDescending(l => hid[l]).ThenByDescending(l => cont[l]).ThenByDescending(l => comp[l]).First();
            var v = Elevacoes(new Dictionary<string, string> { { escolha, exib } }, Modelo("ELEVAÇÃO ILUMINAÇÃO")).First();
            log.AppendLine(v.Name + " → parede " + escolha + " (" + M(comp[escolha]) + " m) | hidro N" + hid["N"] + " L" + hid["L"] + " S" + hid["S"] + " O" + hid["O"] + " | pontos N" + cont["N"] + " L" + cont["L"] + " S" + cont["S"] + " O" + cont["O"] + " | dir (" + Math.Round(v.ViewDirection.X) + "," + Math.Round(v.ViewDirection.Y) + ")");
        } else {
            // planta ampliada
            var pl = ViewPlan.Create(document, tipoPlanta.Id, nivel.Id);
            pl.Name = prefixo + "PAGINAÇÃO DE PISO " + exib; var mp = Modelo("PLANTA BAIXA PAGINAÇÃO DE PISO"); if (mp != null) pl.ViewTemplateId = mp.Id; pl.Scale = escala;
            pl.CropBoxActive = true; var pac = pl.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE); if (pac != null && !pac.IsReadOnly) pac.Set(1);   // anotações (linhas de corte etc.) não vazam do recorte
            pl.CropBox = new BoundingBoxXYZ { Min = new XYZ(x0 - F(0.15), y0 - F(0.15), -1000), Max = new XYZ(x1 + F(0.15), y1 + F(0.15), 1000) };
            // 4 elevações: 01 N, 02 L, 03 S, 04 O
            var me = Modelo("ELEVAÇÃO PAGINAÇÃO"); int n = 1;
            Elevacoes(lados.ToDictionary(l => l, l => exib + " " + (n++).ToString("00")), me);   // um marcador só com as 4 paredes (01 N, 02 L, 03 S, 04 O)
            // 2 isométricas de cantos opostos
            var m3 = Modelo("MODELAGEM FACHADA");
            var dirs = new[] { new XYZ(1, 1, -1).Normalize(), new XYZ(-1, -1, -1).Normalize() };
            for (int i = 0; i < 2; i++) {
                var v3 = View3D.CreateIsometric(document, tipo3D.Id);
                v3.Name = prefixo + "PAGINAÇÃO DE PISO " + exib + " " + (i + 1).ToString("00");
                // caixa como no projeto de referência: começa na face interna das 2 paredes do lado da câmera (somem da vista),
                // inclui as 2 paredes do fundo (+0,15 m) e para 1 cm abaixo do forro do ambiente (ou na altura informada)
                var forros = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType().ToElements()
                    .Select(e => e.get_BoundingBox(null)).Where(b => b != null && (b.Min.X + b.Max.X) / 2 > x0 && (b.Min.X + b.Max.X) / 2 < x1 && (b.Min.Y + b.Max.Y) / 2 > y0 && (b.Min.Y + b.Max.Y) / 2 < y1 && b.Min.Z > zc).ToList();
                double topoIso = forros.Count > 0 ? forros.Min(b => b.Min.Z) - F(0.01) : zc + altura;
                v3.SetSectionBox(i == 0
                    ? new BoundingBoxXYZ { Min = new XYZ(x0, y0, zc - F(0.10)), Max = new XYZ(x1 + F(0.15), y1 + F(0.15), topoIso) }
                    : new BoundingBoxXYZ { Min = new XYZ(x0 - F(0.15), y0 - F(0.15), zc - F(0.10)), Max = new XYZ(x1, y1, topoIso) });
                var fwd = dirs[i]; var up = new XYZ(0, 0, 1).Subtract(fwd.Multiply(fwd.Z)).Normalize();
                v3.SetOrientation(new ViewOrientation3D(centro - fwd.Multiply(F(30)), up, fwd));
                if (m3 != null) v3.ViewTemplateId = m3.Id; v3.Scale = escala;
                // recorte na projeção da caixa do ambiente (+0,1 m) e níveis ocultos — sem isso a vista ocupa o modelo inteiro na folha
                document.Regenerate();
                var sb = v3.GetSectionBox(); var cbi = v3.CropBox; var invI = cbi.Transform.Inverse; var ptsI = new List<XYZ>();
                foreach (var xx in new[] { sb.Min.X, sb.Max.X }) foreach (var yy in new[] { sb.Min.Y, sb.Max.Y }) foreach (var zz in new[] { sb.Min.Z, sb.Max.Z }) ptsI.Add(invI.OfPoint(sb.Transform.OfPoint(new XYZ(xx, yy, zz))));
                cbi.Min = new XYZ(ptsI.Min(q => q.X) - F(0.1), ptsI.Min(q => q.Y) - F(0.1), cbi.Min.Z); cbi.Max = new XYZ(ptsI.Max(q => q.X) + F(0.1), ptsI.Max(q => q.Y) + F(0.1), cbi.Max.Z);
                v3.CropBox = cbi; v3.CropBoxActive = true; v3.CropBoxVisible = false;
                var nvI = new FilteredElementCollector(document, v3.Id).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElementIds(); if (nvI.Count > 0) v3.HideElements(nvI);
            }
            log.AppendLine(exib + " → planta ampliada + 4 elevações + 2 isométricas (" + M(x1 - x0) + " × " + M(y1 - y0) + " m)");
        }
    }
    t.Commit();
}
log.AppendLine("Tipos: elevação '" + tipoElev.Name + "', planta '" + tipoPlanta.Name + "' | escala 1:" + escala);
return log.ToString();
