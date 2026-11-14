# Fase 2 — Proposta de layout

Quando: depois da abertura (fase 1) e da conferência do modelo (fase 3), com o layout modelado (ambientes e mobiliário).
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
