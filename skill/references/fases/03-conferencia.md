# Fase 3 — Conferência do modelo

Quando: depois que o arquiteto modelou (paredes, portas, janelas, ambientes, pisos, forros, cobertura) e antes das fases 4 e 5.
Rode de novo sempre que o modelo mudar muito — é só leitura, rápido e sem risco.

## 1. Conferir — `scripts/03_conferir_modelo.cs` (somente leitura)
Parâmetro opcional `"detalhe"` lista todos os elementos de cada item (sem ele: contagem + 5 exemplos).

| Nível | O que confere | Por que importa |
|---|---|---|
| **BLOQUEIA** | níveis "PAV. X N.A"; nível COBERTURA; ambientes colocados; ambiente CALÇADA | as fases 4 e 5 dependem disso (plantas por pavimento, cobertura, vistas por ambiente, frente do lote) |
| **CORRIGIR** | ambientes não colocados / não fechados / sem nome; portas e janelas sem marca de tipo ou com a mesma marca em tipos diferentes; CAD importado no modelo | aparece errado nas tabelas (ID vazio ou repetido) e nas folhas |
| **ATENÇÃO** | nível de osso (N.O) 5 cm abaixo do acabado; linhas de propriedade; vínculos descarregados; paredes < 5 cm; áreas molhadas sem forro; avisos do Revit agrupados por tipo | qualidade do caderno; o arquiteto decide |

A coluna ID das tabelas de portas e janelas é a **marca de tipo** (P01, J01…) — é ela que a conferência olha.

## 2. Mostrar ao arquiteto
Apresente o relatório em linguagem simples: primeiro os bloqueios, depois o que corrigir, depois a atenção.
Para cada bloqueio, diga o que fazer (ex.: "crie o ambiente CALÇADA na faixa da frente").

## 3. Corrigir — `scripts/03_corrigir_modelo.cs` (só com confirmação)
Correções seguras, numa transação só (Ctrl+Z desfaz), sem salvar:
- `ambientes_nao_colocados` — apaga ambientes sem localização.
- `paredes_curtas` — apaga paredes com menos de 5 cm que não hospedam portas/janelas.

O resto (ambientes não fechados, marcas repetidas, linhas de separação sobrepostas, CAD importado) é decisão do arquiteto:
liste os itens com o id do elemento para ele localizar (Gerenciar › Selecionar por ID).

## 4. Rodar a conferência de novo e registrar
Sem bloqueios → grave em `<projeto>.skill.json` a fase 3 como concluída e siga para a fase 4.

Teste no projeto de referência: nenhum bloqueio; 411 ambientes não colocados apagados (sobra de lista); 34 avisos do Revit
agrupados em 8 tipos; vínculo de interiores descarregado apontado.
