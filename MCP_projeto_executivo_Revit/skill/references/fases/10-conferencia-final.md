# Fase 10 — Conferência final (antes de emitir)

Rotina: `scripts/10_conferencia_final.cs` — somente leitura. Parâmetro opcional: prefixo do número das folhas (teste).

| Nível | O que confere |
|---|---|
| **BLOQUEIA** | folha vazia; numeração i/N quebrada em algum caderno (falta folha, total errado, número repetido); carimbo sem cliente, endereço, autor, CAU, nome do projeto ou área do lote |
| **CORRIGIR** | tabela sem itens; vista fora da moldura ou sobre o carimbo; vistas sobrepostas (> 25 mm); CONTEÚDO DA PRANCHA vazio |
| **ATENÇÃO** | vistas do caderno que não estão em folha; RRT, ART, executor, CREA, desenhista ou etapa vazios; avisos do Revit |

Cadernos são separados pelo carimbo: PROPOSTA ("Proposta"), ARQ ("Executivo Arquitetônico"), DET ("Detalhamento").

## Como conduzir
1. Rode a conferência e mostre ao arquiteto: bloqueios primeiro.
2. Bloqueio → não emitir. Corrigir → resolver ou o arquiteto aceita explicitamente.
3. Exporte as folhas como imagem (como na fase 5) e mostre ao arquiteto antes de emitir.
4. Sem bloqueios e com o ok do arquiteto → fase 11.

Teste no projeto de referência: caderno ARQ 1/7 a 7/7 e proposta 1/1 sem bloqueios; apontou ETAPA DO PROJETO vazia
e uma vista de teste fora de folha.

## Lições do piloto
- Vistas de pavimentos sem ambientes (ex.: superior numa casa térrea) não entram no aviso de "vistas fora de folha".
