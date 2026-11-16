# Fase 4 — Ambientes de portas, janelas e nomes de ambientes

Quando: depois da fase 3 sem bloqueios, antes de montar as folhas (fase 5) — as tabelas de portas e janelas mostram a coluna AMBIENTE.

## O que o escritório preenche (levantado no projeto de referência)
- **Portas** (`AMBIENTE DA PORTA`) e **janelas** (`.AMBIENTE`): um texto por **tipo** (a tabela agrupa pela marca de tipo),
  resumindo onde o tipo é usado — ex.: "LAVANDERIA, CIRCULAÇÃO E QUARTOS", "WC's", "QUARTOS/ÁREA EXTERNA".
- **Ambientes**: só o nome e o número. Os campos de acabamento (piso, parede, forro, base) não são usados — a skill não os preenche.
- **Luminárias**: o campo "Ambiente" não é usado; a tabela luminotécnica agrupa por modelo.

## Rotina — `scripts/04_ambientes.cs`
1. Rode em modo `"propor"` (padrão, não altera nada). Para cada tipo de porta/janela mostra a proposta e o valor atual.
   Regras: porta → ambiente que não é circulação nem externo; porta que dá para fora → "<ambiente>/ÁREA EXTERNA";
   janela → ambiente interno; sem ambiente → "FACHADA <LADO>[ - <PAVIMENTO>]"; 2+ quartos → "QUARTOS"; 2+ WCs → "WC's".
2. Mostre ao arquiteto só o que estava **vazio** e as diferenças relevantes. O arquiteto costuma refinar os textos
   (ex.: "HALL DE ENTRADA", "CAIXA D'ÁGUA") — no projeto de referência ~1/3 dos tipos bateram exatamente; o resto é escolha de nome.
3. Com a confirmação, rode `"aplicar"`: preenche **só os campos vazios**, nunca sobrescreve o que o arquiteto escreveu.
   `"aplicar_tudo"` (sobrescreve) só com pedido explícito.
4. O relatório também aponta **nomes de ambientes inconsistentes** (ex.: "APT 02" × "APARTAMENTO 01") e nomes em minúsculas —
   eles aparecem nas vistas por ambiente, nas tabelas e nos títulos. Corrigir o nome é com o arquiteto (afeta o projeto inteiro).

Teste no projeto de referência: 1 porta vazia preenchida; nenhum valor do arquiteto alterado; inconsistência "APT 02" apontada.

## Lições do piloto
- Porta entre dois ambientes internos vale o mais reservado: banheiro > quarto > serviço > cozinha > demais
  (antes pegava o primeiro e a porta do banheiro saía como "COZINHA").
