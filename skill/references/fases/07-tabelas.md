# Fase 7 — Tabelas

Não há script próprio: as tabelas vêm prontas do **template master** e se preenchem sozinhas a
partir do modelo. O trabalho desta fase é garantir que os dados que elas leem estejam corretos.

| Tabela | Lê | Quem preenche | Onde vai |
|--------|----|---------------|----------|
| Tabela de portas / janelas | marca de tipo (ID), dimensões, abertura, material, **AMBIENTE** | fase 3 (marca), fase 4 (AMBIENTE), família (dimensões) | folha 1/N do ARQ (fase 5) e folhas de esquadrias do DET (fase 9) |
| Luminotécnico, pontos, louças etc. | instâncias do modelo | modelagem do arquiteto | folhas da fase 5 |
| Áreas e índices do carimbo | regiões preenchidas da implantação | `scripts/calcular_areas_indices.cs` (fase 8) | carimbo de todas as folhas |

## Roteiro
1. Rode a fase 3 (`03_conferir_modelo.cs`): portas/janelas sem marca de tipo aparecem como CORRIGIR.
2. Rode a fase 4 (`04_ambientes.cs`, modo `propor` → `aplicar`) para o campo AMBIENTE.
3. Abra cada tabela com `get_schedule_data` e mostre ao usuário as linhas com campos vazios.
4. A conferência final (fase 10) acusa tabelas vazias nas folhas.

Se o escritório não tiver alguma tabela no template, crie com `create_schedule` do MCP copiando
campos e filtros de uma tabela equivalente, e **adicione ao template master pelo responsável** —
nunca só no projeto.
