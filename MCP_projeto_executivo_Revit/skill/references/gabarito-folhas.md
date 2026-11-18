# Gabarito de diagramação das folhas ARQ (A1 841 × 594 mm)

Extraído do projeto de referência (residência térrea, 14/11/2026). Coordenadas = centro da vista em mm a partir do canto inferior esquerdo do carimbo. "tam" = tamanho da vista na folha (L × A). Use como ponto de partida; ajuste quando o desenho não couber.
Área útil de desenho: moldura x 25–832, y 10–584 mm, menos o carimbo, que ocupa o canto inferior direito (x > 656 e y < 297 mm). Acima do carimbo a faixa direita é livre (legendas, fachadas longas).

## 1/7 — Planta executivo + implantação + esquadrias
| Vista | Centro (x, y) | Tamanho |
|---|---|---|
| Planta térreo executivo (1:75) | (354, 448) | 667 × 274 |
| Implantação (1:100) | (264, 211) | 468 × 197 |
| Legenda planta baixa executivo | (754, 414) | 146 × 166 |
| Legenda implantação | (568, 215) | 163 × 147 |
| Tabela de janelas | canto sup. esq. (132, 90) | — |
| Tabela de portas | canto sup. esq. (389, 90) | — |

## 2/7 — Cobertura + situação
| Cobertura executivo (1:75) | (394, 448) | 667 × 246 |
| Legenda planta de cobertura | (124, 298) | 119 × 47 |
| Planta de situação (desenho) | (340, 157) | 408 × 230 |

## 3/7 — Cortes
| Corte C (longitudinal) | (369, 522) | 644 × 143 |
| Corte D (longitudinal) | (370, 408) | 647 × 131 |
| Corte A (transversal) | (191, 277) | 289 × 158 |
| Corte B (transversal) | (510, 263) | 268 × 129 |
| Perspectiva corte D (3D) | (224, 120) | 336 × 127 |
| Perspectiva corte B (3D) | (536, 91) | 230 × 122 |
Regra: longitudinais empilhados no topo; transversais lado a lado no meio; perspectivas embaixo.

## 4/7 — Fachadas
| Fachada lateral direita (longa) | (443, 531) | 725 × 164 |
| Fachada lateral esquerda (longa) | (367, 391) | 684 × 134 |
| Fachada frontal | (206, 245) | 361 × 125 |
| Fachada posterior | (205, 112) | 354 × 149 |
| Perspectiva lateral direita (3D) | (517, 255) | 303 × 207 |
| Perspectiva lateral esquerda (3D) | (518, 102) | 278 × 192 |
Regra: fachadas longas no topo; curtas à esquerda embaixo; perspectivas à direita.

## 5/7 — Iluminação e tomadas
| Iluminação e interruptores (forro, 1:75) | (307, 474) | 539 × 206 |
| Pontos de tomada (planta, 1:75) | (305, 242) | 543 × 206 |
| Legenda luminotécnico | (664, 474) | 162 × 192 |
| Legenda de tomadas | (780, 462) | 83 × 167 |
| Tabela luminotécnico | canto sup. esq. (261, 121) | — |

## 6/7 — Forro + vistas de iluminação
| Planta de forro (1:75) | (295, 145) | 539 × 209 |
| Legenda forros | (612, 210) | 107 × 92 |
| Vistas de iluminação dos quartos (1:25) | linha y = 509; x = 116, 286, 464, 674 | ~158 × 116 |
| Vistas de iluminação dos banheiros/lavatório (1:25) | linha y = 375; x = 94, 212, 355, 505, 668 | ~116 × 117 |
Regra: uma linha para quartos, outra para áreas molhadas; distribuídas da esquerda para a direita.

## 7/7 — Paginação
| Paginação de piso geral (1:75) | (329, 459) | 637 × 260 |
| Ampliações de paginação (1:50), 1 por ambiente molhado | coluna x ≈ 78; y = 286, 170, 71 | ~50 × 65 |
| Isométricas (3D), 2 por ambiente | colunas x ≈ 170 e 245, mesmas linhas | ~60 × 85 |
| Vistas de parede (1:50), 4 por ambiente | linhas y = 284, 175, 72; x de 330 a 570 | ~50 × 65 |
| Legenda paginação | (765, 364) | 112 × 48 |
Regra: uma linha por ambiente molhado: ampliação → 2 isométricas → 4 vistas de parede.

## Posição dos cortes no modelo de referência
Edifício (paredes) ~40 × 15 m. Transversais A e B a ~21% e ~58% do comprimento; longitudinais C e D a ~68% e ~22% da largura. Profundidade de vista: transversais atravessam todo o comprimento; longitudinais ~4–11 m.

## Casas de mais de um pavimento — plano do caderno
Os códigos 1/7 … 7/7 acima são **tipos de folha** (o conteúdo). O número de cada folha é dado pelo plano do caderno.
Regra do escritório: em geral **um pavimento por folha**; se couber, dois pavimentos na mesma folha.

| Tipo | Térrea | Mais de um pavimento |
|---|---|---|
| 1/7 executivo | planta + implantação + tabelas de esquadrias | tenta `"TERREO;SUPERIOR"` com `"testar"`; se `NAO_COUBE`, uma folha por pavimento. Implantação e tabelas só na folha do térreo |
| 2/7 cobertura + situação | uma folha | uma folha (cobertura é única) |
| 3/7 cortes | uma folha | uma folha (os cortes atravessam todos os pavimentos; se não couber, divida A/B e C/D) |
| 4/7 fachadas | uma folha | uma folha |
| 5/7 iluminação e tomadas | uma folha | tenta os pavimentos juntos com `"testar"`; senão uma por pavimento. Tabela luminotécnica só na do térreo |
| 6/7 forro + vistas de iluminação | uma folha | uma por pavimento (só as vistas daquele pavimento) |
| 7/7 paginação | uma folha | uma por pavimento (só os conjuntos daquele pavimento) |

Como montar:
1. Para cada tipo que admite pavimentos juntos (1/7 e 5/7), rode `05_montar_folhas_gerais.cs` com
   `[tipo, "TESTE", prefixo, "TERREO;SUPERIOR", "", "testar"]`. A 1ª linha do retorno é `COUBE` ou `NAO_COUBE`; nada é gravado.
2. Com as decisões, conte as folhas (N) e numere na ordem: executivo(s) → cobertura → cortes → fachadas →
   iluminação e tomadas → forro(s) → paginação(ões). Ex.: sobrado com tudo separado = 10 folhas (01/10 … 10/10).
3. Monte cada folha com o número definitivo (`"01/10"` …). O nome da folha vira "PRANCHA 01" … e o conteúdo do carimbo ganha
   " - PAVIMENTO X" quando o modelo tem mais de um pavimento.
4. A conferência final (`10_conferencia_final.cs`) confere a sequência i/N.

## Folhas complementares (checklist executivo)
Quando a equipe modelou os pontos, entram depois da folha de iluminação e tomadas, nesta ordem: **ar-condicionado** (`ar`),
**hidro e gás** (`hidro`), **iluminação externa** (`ilumext`, só o pavimento mais baixo). Monte com `05_montar_folhas_gerais.cs`
`[tipo, número, prefixo, pavimentos]`. Uma folha por pavimento, ou dois juntos se `"testar"` devolver `COUBE`. Elas contam no N
da numeração i/N.
