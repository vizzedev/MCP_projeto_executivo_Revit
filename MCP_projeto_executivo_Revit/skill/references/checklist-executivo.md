# Checklist executivo do escritório × skill

**Regra:** recebida a modelagem da equipe, a skill faz o projeto executivo inteiro.
- **Anotações, cotas, textos, vistas e folhas:** a skill faz direto.
- **Elementos físicos do executivo** (abrigo, captação, lixeira…): a skill insere numa posição proposta e o arquiteto aprova.
- **Regras de projeto fora do padrão:** a skill corrige no modelo o que o arquiteto aprovar.
- **Dados de fora do modelo** (rua, lotes, recuos, CPF, QR code): vêm do questionário de abertura.

Coluna **Skill**:
- **faz**: script entre parênteses;
- **propõe**: insere e espera aprovação;
- **corrige**: muda o modelo depois da aprovação;
- **equipe**: concepção, modelagem ou decisão que não sai do modelo.

## 1 · Briefing e levantamento topográfico (concepção — equipe)

| Item | Skill |
|---|---|
| Briefing, reuniões, programa de necessidades, condomínio, documentos, matrícula, visita, entorno, referências | equipe |
| Levantamento topográfico e sondagem; passar o levantamento para o Revit e modelar a topografia | equipe · a fase 3 avisa se a topografia não estiver modelada |
| Norte verdadeiro e do projeto | **faz** símbolo de norte na implantação (`06_implantacao.cs`); a rotação é conferida pelo arquiteto |
| DWG original do topógrafo na prancha | equipe |

## 2 · Concepção e regras projetuais (layout)

| Regra | Skill |
|---|---|
| Eixo 0.00, PNT, modelagem pelas paredes, janelas centralizadas, bonecas de portas | equipe (modelagem) |
| Projeção dos recuos em azul tracejado | **faz** (`06_implantacao.cs`: estilo "VZ-RECUO" na implantação e no executivo do térreo) |
| Norte arquitetônico | **faz** (`06_implantacao.cs`) |
| Janelas: peitoril padrão 1,00 m | **corrige** (`03_corrigir_modelo.cs`, `peitoril`) |
| Porta de entrada ≥ 1,30 m; lavabo 0,70 m; alumínio em corredor ≥ 0,90 m | **corrige** (`portas_largura`: troca pelo tipo da mesma família) · porta não pivotante: a fase 3 aponta |
| Bancadas: cozinha 0,70 × 0,90 (0,95 com cervejeira); ilha ≥ 0,90; lavabo 0,55; lavanderia 0,60; churrasqueira 1,10 × 0,80–0,90 | **corrige** (`bancadas`: parâmetros da família) |
| Garagem: vaga 3,00 × 5,00 m | a fase 3 aponta (mudar o ambiente é decisão de projeto) |
| Sacada (veneziana, +0,37 m), janelas +0,27 m, sofá, circulações, alpendre, espelho/saia/desnível da cozinha | equipe (modelagem) |
| Planta de prefeitura, varanda separada da área construída | **faz** áreas por região (`08_regioes_areas.cs`) |
| Numeração predial | **faz** (`06_implantacao.cs`) |
| Padrão de energia na implantação | **propõe** (`06_elementos_externos.cs`, `abrigo`) |
| Planta layout: exportar bonita, interiores em meio-tom, térreo em segundo tom no superior, paisagismo | concepção da equipe · a skill anota e monta a folha (fase 2) |

## 3 · Processo executivo (pranchas) — a skill faz

### Etapa 1 · Aprovação e executivo 1 (A1)

| Item | Skill |
|---|---|
| Cotas detalhadas em três escalas (vãos, ambientes, total); tamanho do terreno em todas as plantas | **faz** (`06_anotar_planta.cs`, `cotas_externas`) |
| Identificadores de ambientes (área, perímetro, nível), portas, janelas; níveis de piso | **faz** (`06_anotar_planta.cs`) |
| Identificadores de bancadas | **faz** (fase 9) |
| Linhas de corte em áreas molhadas e escadas; fachadas | **faz** (`05_propor_cortes.cs`, `05_criar_fachadas.cs`) |
| Legendas; legenda da planta baixa executivo | **faz** (montagem das folhas) |
| Lotes vizinhos, nome da rua, largura, asfalto | **faz** (`06_implantacao.cs`) |
| Linhas de recuo em azul tracejado | **faz** (`06_implantacao.cs`) |
| Projeções de pergolados e beirais, com o nome | **faz** (`06_textos.cs`, executivo) |
| Norte | **faz** (`06_implantacao.cs`) |
| Abrigo de medidores, caixa de captação (≥ 1,5 m), lixeira, piso tátil, rebaixo do meio-fio (50 cm), ducha (com piscina), casa de gás | **propõe** (`06_elementos_externos.cs`) |
| Carimbo: conteúdo, tipo de construção, endereço, data, desenhista, ART/RRT, áreas, nomes e CPF | **faz** (`01_preencher_dados.cs` com o questionário; `calcular_areas_indices.cs`) |
| Carimbo: QR code | **faz** (PNG gerado pela skill + `08_qrcode.cs`) |
| Esconder o paisagismo (só na planta de layout) | **faz** (`06_anotar_planta.cs`, `paisagismo`) |
| X tracejado nos vazios de volumetria | **faz** (`06_textos.cs`, executivo) |
| Eixos de hidrossanitário amarrados | **faz** (`06_cotar_pranchas.cs`, modo `hidro`) |
| Descidas de água, shafts (laranja), casa de máquinas, armário externo, piscina, pisos externos, níveis do lote, acessos | equipe (modelagem) · aparecem nas vistas como modelados |
| Conferir cotas, remover sobras de rampa/topografia, conferir camadas | **faz** a conferência final (fase 10); limpeza de sobras: equipe |

### Etapa 2 · Paginação

| Item | Skill |
|---|---|
| Início da paginação (banheiros na porta) com seta/chamada | **faz** (`06_textos.cs`, `paginacao`: "INÍCIO DA PAGINAÇÃO" na porta principal de cada ambiente) |
| Tipos de piso por ambiente | **faz** ("PISO <acabamento>" em cada ambiente) |
| Nota de ralos conforme o hidrossanitário; canaletas nas áreas externas | **faz** (notas da planta de paginação) |
| Vistas e folhas de paginação | **faz** (`05_vistas_ambientes.cs`, `05_montar_folhas_ambientes.cs`) |
| Sentido de partida em cada ambiente (direção das peças), símbolos bolinha/quadrado/triângulo, imagens numeradas, tabelas de bits/soleiras/pingadeiras, ralo oculto, material da piscina | equipe (identificadores e tabelas do template; decisão de paginação) |

### Etapa 2 · Luminotécnico

| Item | Skill |
|---|---|
| Legendas e tabela quantitativa | **faz** (folha 5/N) |
| Tomadas fora das alturas padrão (0,30 / 0,70 / 1,10 / 1,40 / 2,20) e de uso específico | **faz** (`06_textos.cs`, tomadas) |
| Altura das luminárias de parede e prateleira; cota do LED | **faz** (`06_textos.cs`, iluminação; `06_cotar_pranchas.cs`) |
| Pontos, quadros, inversor, cortineiros, torres, nichos, chuveiro, nome e cor das luminárias | equipe (modelagem e especificação) |

### Plantas complementares

| Planta | Skill |
|---|---|
| Ar-condicionado: BTU, modelo, distância condensadora–evaporadora | **faz** (`05_plantas.cs`, `06_textos.cs` modo `ar`, folha `ar`) |
| Iluminação externa: jardim, arandelas | **faz** (vista, alturas, folha `ilumext`) · interruptor de cada ponto: em aberto |
| Hidro e gás: torneiras, água quente/fria, gás, torneira de parede com chamada e altura, nichos | **faz** (vista, `06_textos.cs` modo `hidro`, cotas de eixo, folha `hidro`) |

### Etapa 3 · Detalhamento (A3)

| Item | Skill |
|---|---|
| Tabelas de portas e janelas (altura, largura, quantidade, ambiente, material) | **faz** (fase 7; a fase 10 confere as colunas) |
| Bancadas: 3D, vistas superior/frontal/lateral, identificadores, louças, material | **faz** (`09_montagens.cs`, `06_textos.cs` bancada, `09_montar_folhas_det.cs`) |
| Cotas L/H das esquadrias, área molhada e rodapé em pedra, nichos, churrasqueira, caderno de pintura e de pedras, planta de acabamentos, piscina, telhado, casa de gás, armário externo | próxima etapa da skill (hoje: equipe) |

## 4 · Entrega para o cliente (equipe)
Impressões, pen drive e pastas de entrega ficam com a equipe. A skill entrega os PDFs da emissão (fase 11).
