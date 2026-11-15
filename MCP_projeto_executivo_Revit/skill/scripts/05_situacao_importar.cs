// SKILL EXECUTIVO REVIT — Fase 5: planta de situação a partir da planta do loteamento (imagem ou PDF)
// send_code_to_revit · transactionMode "none"
// parameters[0] = caminho do arquivo no computador (PNG, JPG, BMP, TIF ou PDF)
// parameters[1] = nome da vista (padrão "PLANTA DE SITUAÇÃO")
// parameters[2] = largura desejada na folha em mm (padrão 400)
// parameters[3] = página do PDF (padrão 1)
// Cria (ou recria) a vista de desenho, insere o arquivo e ajusta o tamanho para a largura pedida na folha.
// O arquiteto marca o lote com a região "VZ-Lote do Projeto" (hachura cruzada vermelha) — a skill não sabe onde ele está na imagem.
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var arquivo = P(0); var nomeVista = P(1) != "" ? P(1) : "PLANTA DE SITUAÇÃO";
double largura = 400; double.TryParse(P(2).Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out largura); if (largura <= 0) largura = 400;
int pagina = 1; int.TryParse(P(3), out pagina); if (pagina < 1) pagina = 1;
if (!System.IO.File.Exists(arquivo)) return "ERRO: arquivo não encontrado: " + arquivo;
var tipoDesenho = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().Where(t => t.ViewFamily == ViewFamily.Drafting).OrderBy(t => t.Name.ToUpper().Contains("LOGO") ? 1 : 0).First();
using (var t = new Transaction(document, "Skill: situação por imagem")) {
    t.Start();
    var antiga = new FilteredElementCollector(document).OfClass(typeof(ViewDrafting)).Cast<View>().FirstOrDefault(v => v.Name == nomeVista);
    if (antiga != null) document.Delete(antiga.Id);
    var vista = ViewDrafting.Create(document, tipoDesenho.Id); vista.Name = nomeVista; vista.Scale = 10;
    var opcoes = arquivo.ToLower().EndsWith(".pdf") ? new ImageTypeOptions(arquivo, false, ImageTypeSource.Import) { PageNumber = pagina } : new ImageTypeOptions(arquivo, false, ImageTypeSource.Import);
    var tipoImg = ImageType.Create(document, opcoes);
    var img = ImageInstance.Create(document, vista, tipoImg.Id, new ImagePlacementOptions(XYZ.Zero, BoxPlacement.Center));
    // largura na folha = largura do modelo / escala → largura do modelo = mm × escala
    double larguraAtual = img.Width; double alvo = (largura / 304.8) * vista.Scale;
    img.Width = alvo;
    t.Commit();
    return "Vista '" + nomeVista + "' criada com " + System.IO.Path.GetFileName(arquivo) + " — " + Math.Round(largura) + " × " + Math.Round(img.Height * 304.8 / vista.Scale) + " mm na folha. Marque o lote com a região 'VZ-Lote do Projeto'.";
}
