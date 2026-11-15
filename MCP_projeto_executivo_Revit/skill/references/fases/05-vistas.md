# Fase 5 — Geração de vistas

Ordem: cortes → fachadas → vistas por ambiente → perspectivas → montagem das folhas.
Tudo roda no arquivo do projeto aberto (nunca no master/referência). Nada é salvo sem confirmação.

## 5.0 Plantas — `scripts/05_plantas.cs`
O template master já traz as plantas de cada pavimento (executivo, tomadas, iluminação e interruptores, forro, paginação),
cobertura e implantação. O script cria só as que faltarem (tipo + modelo + nível + escala do escritório, sem recorte, como o
arquiteto faz) e informa o conteúdo de cada uma; planta sem conteúdo (ex.: sem tomadas modeladas) sai com aviso "VAZIA".
Pavimento sem ambientes é ignorado (as vistas dele não entram no caderno).

## 5.0b Planta de situação
- **Padrão:** `scripts/05_situacao.cs` desenha a situação vetorial — lote da obra em hachura cruzada vermelha com medidas,
  lotes vizinhos numerados, rua da frente, rua da esquina com a distância até ela e norte girado.
  Medidas do lote lidas do modelo; o resto vem do Bloco 2b do questionário. Escala automática (1:250 a 1:2000).
- **Com a planta do loteamento:** `scripts/05_situacao_importar.cs` insere a imagem/PDF na vista na largura pedida
  (como no projeto de referência); o arquiteto marca o lote com a região "VZ-Lote do Projeto".

## 5.1 Cortes — a skill propõe, o arquiteto aprova
1. Rodar `scripts/05_propor_cortes.cs` com prefixo `"PROPOSTA "` → cria 4 cortes (A/B transversais pela área molhada e pelo maior ambiente social; C/D longitudinais).
2. Mostrar ao arquiteto a posição de cada corte (eixo e ambientes que atravessa) e perguntar: **aprovar / mover / trocar**.
3. Ajustes: mover o corte no Revit ou rodar de novo com posição forçada.
4. Conferir que cada corte atravessa os ambientes (plano no eixo pedido, não na borda da casa).
5. Aprovado → renomear tirando o prefixo `PROPOSTA ` (CORTE A … D). Escala 1:75, modelo "CORTE EXECUTIVO".

## 5.2 Fachadas
`scripts/05_criar_fachadas.cs` — 4 elevações (FRONTAL, POSTERIOR, LATERAL DIREITA, LATERAL ESQUERDA).
Oculta na vista o muro de divisa que fica na frente de cada fachada (paredes paralelas, até 3 m de altura, mais de 1 m acima do piso).
Frente detectada pelo ambiente CALÇADA; se não houver, perguntar qual lado é a frente (N/S/L/O) e passar no parâmetro 4.

## 5.3 Vistas por ambiente — `scripts/05_vistas_ambientes.cs`
Perguntar ao arquiteto a lista de ambientes (sugerir: dormitórios, suítes, lavabos e banheiros para iluminação;
áreas molhadas para paginação). Ambientes idênticos: informar só um, com nome no plural
(`WC APT 01=WC'S APARTAMENTOS`).

| Modo | Gera | Escala | Regra da parede |
|---|---|---|---|
| `iluminacao` | 1 elevação interna por ambiente | 1:25, altura 2,75 | área molhada → parede com mais louças/bancada; demais → parede com mais pontos elétricos; empate → a maior |
| `paginacao` | planta ampliada + 4 elevações num marcador só (01 N, 02 L, 03 S, 04 O) + 2 isométricas | 1:50, altura 2,90 | todas as paredes |

Isométricas: caixa começa na face interna das 2 paredes do lado da câmera (mostra o interior), inclui as 2 do fundo e
para 1 cm abaixo do forro; recorte ajustado e níveis ocultos. Planta ampliada com anotações recortadas.
Enquadramento: ambiente + 0,15 m de cada lado. Validado no projeto de referência: 21 de 21 vistas iguais às do arquiteto.
Ambiente irregular (não retangular): conferir o enquadramento.

## 5.4 Perspectivas — `scripts/05_perspectivas.cs`
Rodar depois dos cortes aprovados e das fachadas. Escala 1:100, modelo "MODELAGEM FACHADA".
- **PERSPECTIVA CORTE** — automático: em cada par (A/B e C/D) usa o corte que deixa a menor parte da casa
  (é o que o arquiteto fez no projeto de referência e o que faz as duas caberem na folha); dá para forçar as letras:
  isométrica com caixa de corte começando no plano do corte e mantendo o lado que o corte enxerga;
  câmera olhando a face cortada, girada 45° e 17° de cima.
- **PERSPECTIVA LATERAL DIREITA / ESQUERDA**: vista dos dois cantos da frente, 17° para cada lateral, 12° de cima, sem caixa.
Validado no projeto de referência: ângulos iguais aos do arquiteto (diferença de até 5°).
## 5.5 Montagem das folhas — `scripts/05_montar_folhas.cs`
Folhas prontas: **3/7 (cortes)** e **4/7 (fachadas)**. Carimbo "Executivo Arquitetônico", título de viewport "Title w Line".
- 3/7: C e D empilhados no topo, A e B lado a lado, perspectivas embaixo distribuídas pela largura.
- 4/7: fachadas longas no topo; embaixo, frontal e posterior à esquerda e perspectivas numa coluna à direita.
- As linhas são empilhadas pela altura real de cada vista, com 20 mm para o título. Se as perspectivas
  não couberem, a escala delas sobe sozinha (1:125 → 1:150 → 1:200) e o script avisa.
- Ao final o script confere a área útil (moldura menos o carimbo no canto inferior direito) e sobreposições acima de 15 mm.
- Preenche CONTEÚDO DA PRANCHA ("CORTES" / "FACHADAS") se o parâmetro existir.
- Depois de montar, exportar a folha como imagem e conferir visualmente antes de mostrar ao arquiteto.

### Folhas 6/7 e 7/7 — `scripts/05_montar_folhas_ambientes.cs`
Montadas a partir das vistas que existem (não de posições fixas), para servir a qualquer projeto:
- **6/7**: linha 1 = vistas de iluminação dos ambientes secos; linha 2 = áreas molhadas (lavatório primeiro);
  planta de forro na base e legenda de forros ao lado, sem invadir o carimbo. Validado: posições a poucos mm das do arquiteto.
- **7/7**: planta geral de paginação no topo (sobe até a base ficar acima do carimbo) e legenda à direita;
  uma linha por conjunto de paginação: planta ampliada → 2 isométricas → 4 paredes. Se não couber, as isométricas
  passam para 1:75/1:100 sozinhas. O espaço entre as vistas considera o tamanho do título.
- Parâmetros: folha, número, prefixo, nome da planta geral (forro ou paginação), nome da legenda.
- A planta de forro e a de paginação geral vêm de fases anteriores; a legenda pode repetir em várias folhas.

### Folhas 1/7, 2/7 e 5/7 — `scripts/05_montar_folhas_gerais.cs`
- **1/7**: planta executiva do pavimento + legenda; implantação + legenda; tabelas de janelas e de portas na base.
- **2/7**: cobertura + legenda; planta de situação.
- **5/7**: iluminação e interruptores + legendas luminotécnica e de tomadas; pontos de tomada; tabela luminotécnica na base.
Regra: tabelas ancoradas na base; blocos (planta + legendas à direita, ou embaixo se não couberem) repartem a altura que sobra;
o espaço entre blocos aperta até 6 mm se preciso (os títulos ficam na margem vazia das vistas, como o arquiteto faz).
Tabelas sem itens saem com aviso "VAZIA: modelar antes de emitir" — elas se preenchem sozinhas quando o modelo evolui.
Validado no projeto de referência: posições a poucos mm das do arquiteto; tabelas com 13 janelas, 24 portas e 264 luminárias.

Ordem completa da fase 5: plantas → situação → cortes (aprovação) → fachadas → vistas por ambiente → perspectivas →
folhas 1/7, 2/7, 3/7, 4/7, 5/7, 6/7, 7/7 → exportar imagens e conferir antes de mostrar ao arquiteto.

Fora do escopo automático (fica com o arquiteto): cotas, identificadores de material e hachura de terreno nos cortes.

## Lições do piloto (casa 10 × 10 m modelada do zero)
- `05_propor_cortes.cs`: o corte da área molhada passa pelo centro do banheiro principal (não pela média de todas as áreas molhadas)
  e é afastado 50 cm de qualquer parede paralela a menos de 35 cm.
- `05_plantas.cs`: a implantação é conferida por linhas de divisa/lote (o modelo de vista esconde telhados); vazia → `02_desenhar_lote.cs`.
- `05_montar_folhas_gerais.cs`: abaixo do primeiro bloco, legenda só vai à direita se não chegar ao carimbo.
- Plantas do template sem recorte pegam os marcadores de fachada (3 m fora da casa) e ficam maiores na folha; em casas pequenas
  sobra espaço à direita — ajuste manual se quiser.

## Duas colunas (casa pequena)
`05_montar_folhas_gerais.cs` coloca os dois blocos da folha lado a lado (cada coluna = vista + legendas 22 mm abaixo)
quando: cabem na largura útil (até 832 mm), cada coluna cabe acima das tabelas, a coluna da direita não desce à faixa
do carimbo e o empilhado ficaria apertado ou as vistas são estreitas (< 420 mm). Piloto: 1/7, 2/7 e 5/7 em duas colunas.
`parameters[4] = "empilhar"` mantém o empilhado.
