# Fase 2 — Planta de layout (anotação e folha de proposta)

Quando: depois da fase 1 e da conferência do modelo (fase 3). O layout (ambientes, mobiliário, portas, janelas, pisos) é
**concebido e modelado pela equipe**; a skill não mexe nele. Ela só anota a planta de layout de cada pavimento e diagrama a folha:
- nomes dos ambientes (identificador com nome e área, no padrão do escritório);
- cotas dos ambientes só em texto, sem linha (`06_cotar_pranchas.cs`, modo `layout`, tipo "VZ-Cota Interna");
- textos de acabamento, quando o padrão da prancha pedir.
Faltas encontradas (ambiente sem nome, piso sem material) vão para o relatório, para a equipe corrigir.
É o primeiro material que o cliente vê — tem carimbo próprio ("Proposta Layout"), em faixa na base da folha.

## Rotina — `scripts/02_proposta.cs`
- Uma folha por pavimento com ambientes ("PAV. X N.A"): `1/N`, `2/N`… (a fase 1 já cria; o script cria a que faltar).
- Folha i: planta "PAV. X LAYOUT" (1:75, modelo "PLANTA BAIXA LAYOUT"), no topo e centralizada.
- Folha 1: também a "IMPLANTAÇÃO LAYOUT" (1:100), embaixo à direita, e a legenda de implantação de layout à esquerda,
  centrada na altura dela.
- As plantas vêm do template; se faltar, o script cria (tipo "APRESENTAÇÃO" + modelo do escritório).
- Área útil do carimbo de proposta: x 25–832, y 58–584 mm (carimbo em faixa na base).

Validado no projeto de referência: planta, implantação e legenda a até 10 mm das posições do arquiteto.

## Depois de montar
1. Rode a rotina de áreas (`calcular_areas_indices.cs`) para atualizar a legenda/carimbo de áreas.
2. Exporte a folha como imagem e confira antes de mostrar ao arquiteto.
3. A proposta vai ao cliente pela fase 11 (emissão) com a etapa "PROPOSTA".

## Lições do piloto (casa nova, sem linhas de propriedade)
- A implantação de layout do template esconde paredes e telhados: sem linhas de propriedade ela sai **vazia**. Antes da fase 2 rode
  `scripts/02_desenhar_lote.cs` (lote pelos muros de divisa, projeção da casa, cotas de testada/profundidade e recorte no lote).
  A API não cria linhas de propriedade; o script usa linhas de detalhe.
- A legenda da implantação do template traz valores fixos de outro projeto (área do terreno, ocupada…): confira/edite antes de emitir.
- O script preenche CONTEÚDO DA PRANCHA ("PROPOSTA DE LAYOUT - PAV. X").
