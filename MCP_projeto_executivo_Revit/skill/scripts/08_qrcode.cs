// SKILL EXECUTIVO REVIT — Fase 8: QR code no carimbo de cada folha (checklist executivo: "Atualizar carimbo … QR code")
// send_code_to_revit · transactionMode "none"
// O QR code é gerado ANTES, fora do Revit, a partir do link informado na abertura (ex.: pasta do projeto na nuvem,
// página da obra): a skill cria a imagem PNG com Python (biblioteca qrcode) e grava na pasta do projeto, no computador
// do usuário. Este script só coloca a imagem nas folhas.
// parameters[0] = caminho completo do PNG no computador onde o Revit roda (ex.: C:\Projetos\Silva\qrcode.png)
// parameters[1] = cadernos, separados por ";": ARQ, DET, PROPOSTA (padrão "ARQ;DET")
// parameters[2] = posição do centro da imagem em mm, medida do canto inferior direito do carimbo: "dx,dy" (padrão "35,35")
// parameters[3] = lado da imagem em mm (padrão 25)
// parameters[4] = "refazer" apaga antes os QR codes colocados pela skill (imagens com o mesmo arquivo)
// Folha que já tem a imagem é pulada. Uma transação "Skill: QR code" (Ctrl+Z desfaz). Não salva.
// A posição padrão é um palpite: ajuste dx,dy uma vez pelo carimbo do escritório e grave em config/escritorio.json
// ("qrcode": { "dx_mm": …, "dy_mm": …, "lado_mm": … }).
Func<int, string> P = i => parameters.Length > i ? ((parameters[i] as string) ?? "").Trim() : "";
var caminho = P(0);
if (caminho == "" || !System.IO.File.Exists(caminho)) return "ERRO: imagem do QR code não encontrada: \"" + caminho + "\" (gere o PNG na pasta do projeto antes).";
var cadernos = (P(1) == "" ? "ARQ;DET" : P(1)).ToUpper().Split(';').Select(x => x.Trim()).ToList();
double dx = 35, dy = 35, lado = 25;
var pos = P(2).Split(','); if (pos.Length == 2) { double.TryParse(pos[0].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dx); double.TryParse(pos[1].Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out dy); }
if (P(3) != "") double.TryParse(P(3).Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lado);
bool refazer = P(4).ToLower() == "refazer";
Func<double, double> MM = mm => mm / 304.8;
Func<FamilySymbol, string> Caderno = fs => { var nm = (fs.FamilyName + " " + fs.Name).ToUpper(); return nm.Contains("PROPOSTA") ? "PROPOSTA" : nm.Contains("DETALHAMENTO") || nm.Contains("INTERIORES") || nm.Contains("A3") ? "DET" : nm.Contains("EXECUTIVO") ? "ARQ" : ""; };
var arquivo = System.IO.Path.GetFileName(caminho);
int n = 0, pulados = 0; var avisos = new List<string>();
using (var t = new Transaction(document, "Skill: QR code")) {
    t.Start();
    var tipoImg = new FilteredElementCollector(document).OfClass(typeof(ImageType)).Cast<ImageType>().FirstOrDefault(it => { try { return string.Equals(it.Path, caminho, StringComparison.OrdinalIgnoreCase); } catch { return false; } });
    if (tipoImg == null) tipoImg = ImageType.Create(document, new ImageTypeOptions(caminho, false, ImageTypeSource.Import));
    foreach (var folha in new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => !s.IsPlaceholder)) {
        var tb = new FilteredElementCollector(document, folha.Id).OfCategory(BuiltInCategory.OST_TitleBlocks).FirstElement() as FamilyInstance; if (tb == null) continue;
        if (!cadernos.Contains(Caderno(tb.Symbol))) continue;
        var ja = new FilteredElementCollector(document, folha.Id).OfClass(typeof(ImageInstance)).Cast<ImageInstance>().Where(ii => ii.GetTypeId() == tipoImg.Id).ToList();
        if (ja.Count > 0) { if (refazer) document.Delete(ja.Select(x => x.Id).ToList()); else { pulados++; continue; } }
        var bb = tb.get_BoundingBox(folha); if (bb == null) { avisos.Add(folha.SheetNumber + ": sem caixa do carimbo"); continue; }
        var centro = new XYZ(bb.Max.X - MM(dx), bb.Min.Y + MM(dy), 0);
        try { var ii = ImageInstance.Create(document, folha, tipoImg.Id, new ImagePlacementOptions(centro, BoxPlacement.Center));
            ii.Width = MM(lado); if (Math.Abs(ii.Height - MM(lado)) > MM(0.5)) ii.Height = MM(lado); n++; }
        catch (Exception ex) { avisos.Add(folha.SheetNumber + ": " + ex.Message); }
    }
    t.Commit();
}
return "QR code (" + arquivo + "): colocado em " + n + " folha(s)" + (pulados > 0 ? ", " + pulados + " já tinham" : "") + " — confira a posição no carimbo de uma folha e ajuste dx,dy se precisar."
    + (avisos.Count > 0 ? "\n  " + string.Join("\n  ", avisos) : "");
