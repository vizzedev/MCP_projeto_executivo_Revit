# Fase 8 — Folhas ARQ e carimbo

As folhas do caderno ARQ são montadas na **fase 5** (`05_montar_folhas*.cs`). Esta fase cuida
dos dados que aparecem no carimbo.

## 1. Dados do projeto
Cliente, endereço, responsável técnico, etapa e data vêm das Informações do projeto, gravadas na
fase 1 (`01_preencher_dados.cs`). Confira com o usuário antes de emitir.

## 2. Áreas e índices
`scripts/calcular_areas_indices.cs` lê as **regiões preenchidas** da vista de implantação
executiva, identificadas pelo final do nome do tipo (independe do prefixo do escritório):

| Final do nome do tipo da região | Parâmetro gravado |
|---|---|
| Área Construída Pavimento Térreo | PAV. TÉRREO |
| Área Construída Pavimento Superior | PAV. SUPERIOR |
| Área Ocupada (opcional) | ÁREA OCUPADA (senão = maior pavimento) |
| Área Permeável | ÁREA PERMEÁVEL |
| Área da Piscina | ÁREA PISCINA |

Parâmetros: `[0]` vista de referência (padrão `IMPLANTAÇÃO`), `[1]` `gravar` (qualquer outro
valor só simula). Rode primeiro simulando, mostre os números, grave com o OK do usuário.
Validado no projeto de referência: bate com o PDF emitido.

## 3. Numeração e carimbo por caderno
- ARQ: `i/N` no carimbo A1; Proposta: `i/N` no carimbo de proposta (fase 2); DET: `ii/NN` no A3 (fase 9).
- A fase 10 confere numeração, campos vazios e folhas vazias por carimbo.

## Pendências de template (por escritório)
- Logotipo do escritório no carimbo (família do carimbo — a API não edita rótulos).
- Hachura "<prefixo>Hachura Cruzada 45" e região "<prefixo>Lote do Projeto" (a fase 5 cria se faltar;
  convém levar ao template master).

## Regiões de área automáticas (`scripts/08_regioes_areas.cs`)
Num projeto novo não há regiões de área desenhadas. O script cria, na implantação:
- área construída de cada pavimento = contorno dos pisos internos do nível "PAV. X N.A";
- área permeável = pisos de grama/jardim/concregrama/seixo; piscina = pisos de piscina/prainha.
Rode sem `criar` para ver a proposta; o relatório avisa regiões que já existem na vista (no template havia 2 de outro lote,
que somavam 106 m² a mais). `substituir` apaga todas as regiões de área da vista antes. Depois rode `calcular_areas_indices.cs`.
Piloto: 40,90 m² construídos, 59,10 m² permeáveis em lote de 100 m² → TO 40,90 %, TP 59,10 %, IA 0,41.

## QR code no carimbo
1. Com o link do questionário, gere o PNG com Python na pasta do projeto, no computador do usuário (pasta conectada à
   conversa). Exemplo: `qrcode.make(link).save(r"C:\...\<projeto>\qrcode.png")`; instale com `pip install qrcode[pil]`
   se faltar.
2. Rode `08_qrcode.cs` `["<caminho do PNG>", "ARQ;DET", "<dx,dy>", "<lado>"]`. A posição padrão é 35,35 mm do canto
   inferior direito do carimbo, com 25 mm de lado.
3. Confira numa folha. Se a posição estiver errada, ajuste e grave em `config/escritorio.json`:
   `"qrcode": { "dx_mm": …, "dy_mm": …, "lado_mm": … }`. Use `"refazer"` em `parameters[4]` para recolocar.
