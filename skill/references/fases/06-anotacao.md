# Fase 6 — Anotação das plantas

Script: `scripts/06_anotar_planta.cs`

Coloca, numa planta já criada na fase 5, as anotações que o arquiteto faz à mão:
identificadores de portas, janelas e ambientes, cotas de nível de piso e cotas internas
dos ambientes. Roda em uma transação nomeada ("Skill: anotar planta"), não salva o arquivo.

## Parâmetros

| # | Conteúdo | Exemplo |
|---|----------|---------|
| 0 | Nome da vista de planta | `PAVIMENTO TERREO EXECUTIVO` |
| 1 | Opções (vazio = todas) | `portas;janelas;ambientes;niveis;cotas_internas` |

## O que faz

- **Portas e janelas**: identificador ("Identificador da porta" / "Identificador de Janela")
  0,45 m para o lado interno do vão. Só os elementos visíveis na vista.
- **Ambientes**: "Identificador de Ambiente" no ponto de locação do ambiente
  (apenas ambientes do nível da vista, com área).
- **Níveis**: cota de nível tipo "VZ-Nível Planta" na face superior do piso,
  0,9 m abaixo do identificador do ambiente.
- **Cotas internas**: tipo "VZ-Cota Interna" (ou "VZ-Cota Interna 1/75"), uma em cada
  direção, entre as faces de acabamento das paredes que limitam o ambiente, a 35 % da
  largura do ambiente. Ambientes com contorno com furos/ilhas são pulados.

## Idempotência

Pode ser rodado de novo: elementos que já têm identificador na vista são pulados; cotas de
nível a menos de 1,5 m de uma existente são puladas. As cotas internas são sempre
recriadas — rode `cotas_internas` uma única vez por vista (ou apague as anteriores antes).

## Teste no projeto de referência (térreo, escala 1:75)

| Item | Arquiteto | Skill |
|------|-----------|-------|
| Portas | 23 | 23 |
| Janelas | 10 | 10 |
| Ambientes | 21 | 29 (a skill também identifica áreas externas) |
| Cotas de nível | 27 | 28 |
| Cotas internas | 53 | 26 (1 por direção por ambiente) |

## Depois de rodar

1. Exporte a vista como imagem e mostre ao usuário.
2. Avise que identificadores podem sobrepor mobiliário ou cotas em ambientes pequenos (WC):
   o ajuste fino de posição é manual.
3. As cotas externas (gerais e de vãos na fachada) continuam a cargo do arquiteto, ou
   via a ferramenta `create_dimensions` do MCP quando ele pedir.
