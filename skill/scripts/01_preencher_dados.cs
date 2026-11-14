// SKILL EXECUTIVO REVIT — Fase 1: preencher Informações do projeto e níveis
// send_code_to_revit · transactionMode "none"
// parameters = linhas "Nome do parâmetro=valor"  (uma por item)
//   Texto:  "Nome do cliente=FAMÍLIA SILVA"   (use \n para quebra de linha no valor)
//   Área:   "Área do lote=600"                (m²; vírgula ou ponto)
//   Número: "TAXA DE OCUPAÇÃO=43.65"
//   Nível:  "NIVEL:PAV. SUPERIOR N.A=3.20"    (cota em m; o N.O correspondente é ajustado para −0,05 m)
//   Nativos aceitos: Nome do cliente, Endereço do projeto, Nome do projeto, Data de emissão do projeto,
//                    Status do projeto, Nome da organização, Descrição da organização
// Grava só no documento ATIVO, que precisa ser um projeto (.rvt) — nunca o template.
if (document.IsFamilyDocument || document.PathName.EndsWith(".rte", StringComparison.OrdinalIgnoreCase)) return "ERRO: documento ativo não é um projeto (.rvt): " + document.PathName;
var pi = document.ProjectInformation;
var nativos = new Dictionary<string, BuiltInParameter> {
    {"Nome do cliente", BuiltInParameter.CLIENT_NAME}, {"Endereço do projeto", BuiltInParameter.PROJECT_ADDRESS},
    {"Nome do projeto", BuiltInParameter.PROJECT_NAME}, {"Data de emissão do projeto", BuiltInParameter.PROJECT_ISSUE_DATE},
    {"Status do projeto", BuiltInParameter.PROJECT_STATUS}, {"Nome da organização", BuiltInParameter.PROJECT_ORGANIZATION_NAME},
    {"Descrição da organização", BuiltInParameter.PROJECT_ORGANIZATION_DESCRIPTION}
};
var ok = new List<string>(); var falhas = new List<string>();
var niveis = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().ToList();
Func<string,double> Num = s => double.Parse(s.Trim().Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
using (var t = new Transaction(document, "Skill: dados de abertura")) {
    t.Start();
    foreach (var item in parameters ?? new object[0]) {
        var linha = item as string; if (string.IsNullOrWhiteSpace(linha) || !linha.Contains("=")) continue;
        var i = linha.IndexOf('='); var nome = linha.Substring(0, i).Trim(); var valor = linha.Substring(i + 1).Replace("\\n", "\r\n");
        try {
            if (nome.StartsWith("NIVEL:")) {
                var nn = nome.Substring(6).Trim(); var lv = niveis.FirstOrDefault(l => l.Name == nn);
                if (lv == null) { falhas.Add(nome + " (nível não existe; existentes: " + string.Join(", ", niveis.Select(l => l.Name)) + ")"); continue; }
                var cota = Num(valor);
                lv.Elevation = UnitUtils.ConvertToInternalUnits(cota, UnitTypeId.Meters);
                ok.Add(nn + " = " + cota.ToString("0.00") + " m");
                if (nn.EndsWith(" N.A")) { var osso = niveis.FirstOrDefault(l => l.Name == nn.Substring(0, nn.Length - 4) + " N.O"); if (osso != null) { osso.Elevation = UnitUtils.ConvertToInternalUnits(cota - 0.05, UnitTypeId.Meters); ok.Add(osso.Name + " = " + (cota - 0.05).ToString("0.00") + " m (automático)"); } }
                continue;
            }
            Parameter p = nativos.ContainsKey(nome) ? pi.get_Parameter(nativos[nome]) : pi.LookupParameter(nome);
            if (p == null) { falhas.Add(nome + " (parâmetro não existe)"); continue; }
            if (p.IsReadOnly) { falhas.Add(nome + " (somente leitura)"); continue; }
            if (p.StorageType == StorageType.String) p.Set(valor);
            else if (p.StorageType == StorageType.Double) {
                var spec = p.Definition.GetDataType();
                if (spec == SpecTypeId.Area) p.Set(UnitUtils.ConvertToInternalUnits(Num(valor), UnitTypeId.SquareMeters));
                else if (spec == SpecTypeId.Length) p.Set(UnitUtils.ConvertToInternalUnits(Num(valor), UnitTypeId.Meters));
                else p.Set(Num(valor));
            }
            else if (p.StorageType == StorageType.Integer) p.Set((int)Num(valor));
            else { falhas.Add(nome + " (tipo não suportado)"); continue; }
            ok.Add(nome + " = " + (p.AsValueString() ?? p.AsString()));
        } catch (Exception ex) { falhas.Add(nome + " (" + ex.Message + ")"); }
    }
    t.Commit();
}
return "GRAVADO (" + ok.Count + "):\n  " + string.Join("\n  ", ok) + (falhas.Count > 0 ? "\nNÃO GRAVADO (" + falhas.Count + "):\n  " + string.Join("\n  ", falhas) : "");
