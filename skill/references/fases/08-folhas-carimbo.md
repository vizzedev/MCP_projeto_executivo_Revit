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
