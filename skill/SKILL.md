---
name: executivo-revit
description: Faz o detalhamento técnico executivo de um projeto de arquitetura já modelado no Revit (vistas, cotas, textos, identificadores, tabelas, carimbo, pranchas ARQ e DET e emissão), no padrão do escritório, usando o MCP do Revit. Vale para casas de um ou mais pavimentos. Use quando o usuário quiser detalhar, retomar ou emitir o executivo de um projeto modelado.
---

# Projeto executivo de arquitetura no Revit

Você conduz o detalhamento em **fases**. Em cada fase: executa a parte automática pelo MCP do Revit, mostra o resultado e **para no ponto de aprovação** do arquiteto antes de seguir. Nunca pule a aprovação.

## Escopo: a equipe modela, a skill faz o executivo inteiro
- **A equipe entrega o modelo pronto:** paredes, portas, janelas, pisos, forros, cobertura, ambientes e o layout
  (mobiliário). Concepção e layout são da equipe.
- **A skill faz todo o projeto executivo a partir desse modelo:** vistas, cotas, identificadores, textos, implantação
  (rua, lotes, numeração, norte, recuos), plantas complementares, tabelas, carimbo (com QR code), pranchas ARQ e DET,
  conferência e emissão. O checklist do escritório está ligado item a item em `references/checklist-executivo.md`.
- **Elementos físicos do executivo** (abrigo de medidores, caixa de captação, lixeira, piso tátil, rebaixo de meio-fio,
  ducha, casa de gás): a skill **propõe** — insere as famílias numa posição calculada (`06_elementos_externos.cs`),
  mostra a implantação e só segue com a aprovação do arquiteto ("aprovar", ajuste no Revit ou "desfazer").
- **Regras de projeto do checklist** (peitoril, larguras de portas, bancadas): a fase 3 lista o que foge do padrão; a skill
  **corrige** no modelo o que o arquiteto aprovar (`03_corrigir_modelo.cs` `peitoril`, `portas_largura`, `bancadas`).
  O que é decisão de projeto (mover parede, mudar ambiente, porta pivotante) fica no relatório para a equipe.
- **Área do lote sai da linha de divisa do modelo** (bloco 2 do questionário, antes de gravar "Área do lote"):
  - o lote é a **linha de propriedade** (categoria "Linhas de divisa", preta tracejada); a linha **azul é recuo** e não conta;
  - leia a área pelo parâmetro do Revit e confira pela geometria (polígono dos segmentos); se houver mais de uma linha de
    propriedade, liste todas (medidas, área, posição) e peça ao projetista para **selecionar a do lote** — leia a seleção;
  - se o projetista informou outra área, mostre as duas e grave a que ele confirmar; registre no `<projeto>.skill.json`
    o Id da linha usada.
- **TO é calculada pela projeção da cobertura somada à face externa das alvenarias que contornam o telhado**, nunca só
  pela telha nem pela caixa envolvente das paredes (ela superestima). Prévia na abertura: projete no plano os sólidos de
  paredes e coberturas (`ExtrusionAnalyzer`), una tudo (`BooleanOperationsUtils` Union), use o contorno externo de cada
  mancha e divida pela área do lote; informe também quanto fica fora do lote. O valor oficial continua vindo da região
  "Área Ocupada" na fase 8, que deve seguir a mesma regra.
- **Linha de recuo (linha de divisa azul) confere com os recuos informados.** Logo depois do bloco 2c do questionário:
  - a linha de recuo é a linha de propriedade **azul**; o lote é a **preta tracejada**. Se não der para distinguir pela
    cor/subcategoria, peça ao projetista para selecionar a de recuo e leia a seleção;
  - a frente é o lado do lote voltado para a rua (piso de asfalto / calçada); esquerda, direita e fundos olhando da rua;
  - meça cada lado da linha de recuo até o lado correspondente do lote e compare com os recuos do questionário
    (tolerância 1 cm). Recuo 0 / "recuos livres" = a linha de recuo coincide com o lote naquele lado. A linha de recuo
    nunca pode sair do lote;
  - **se não bater:** mostre a tabela lado × recuo informado × recuo desenhado × diferença e **pergunte sempre:
    "Ajustar a linha de recúo aos valores informados?"**
    - **sim** → ajuste em uma transação nomeada ("Skill: linha de recuo conforme recuos informados"). A API não tem
      `PropertyLine.Create` no Revit 2025 nem edita o esboço: copie a linha do lote (`ElementTransformUtils.CopyElement`,
      deslocamento zero) quando todos os recuos forem 0, copie as sobreposições de cor da linha antiga por vista, apague a
      antiga e grave em Comentários "LINHA DE RECUO". Com recuos diferentes de 0 que o script não consiga desenhar, oriente
      o projetista a editar o esboço (lados e distâncias) e releia. Depois confira a cor azul na vista e registre o Id novo
      no `<projeto>.skill.json`;
    - **não** → pergunte se os valores do questionário estão errados (corrija-os) ou registre a divergência no
      `<projeto>.skill.json` para a conferência final.
- **Dados de fora do modelo** (rua, largura, asfalto, lotes vizinhos, numeração, recuos, CPF, ART/RRT, tipo de construção,
  link do QR code) vêm do questionário de abertura (`references/questionario-abertura.md`).

## Ordem de trabalho: folha por folha
Feche uma folha antes de abrir a próxima: vistas da folha → anotação completa (cotas, identificadores, níveis, textos,
linhas e projeções) → conferência por imagem → montagem da folha → aprovação do arquiteto. Ordem: executivo(s) (+ implantação
e tabelas) → cobertura e situação → cortes → fachadas → tomadas e iluminação → forro(s) → paginação(ões). Não crie cortes,
fachadas ou perspectivas antes da planta executiva estar anotada e aprovada.
- Executivo: `06_anotar_planta.cs` liga o recorte e o ajusta às cotas; quando muro e divisa estão a menos de 1 cm, vale a
  divisa. Linhas de corte com as cabeças fora das cotas. Cotas externas (refinadas 18/11/2026):
  - do lado da rua as linhas ficam **depois do meio-fio e da sarjeta**; a 1ª linha é a do **rebaixo** (divisa → aba → pleno →
    aba → divisa), pelos planos MF_U do meio-fio gerado;
  - nas faces perpendiculares à rua a linha de ambientes segue até a face externa da guia: **largura da calçada** (2,00);
  - borda de piso dentro da espessura de uma parede não vira referência (nada de 0,13 | 0,13 numa parede de 0,25);
  - texto de trecho curto sai ao longo da linha para o lado livre (para fora nas pontas da cadeia).
  - Família gerada pela skill: cote pelos **planos de referência nomeados** (`GetReferenceByName`/`GetReferences`), nunca pelas
    faces da geometria — o Revit descarta ou mede errado.
- Projeções no executivo (`06_projecoes.cs` [planta; "refazer"]): paredes do pavimento de cima e borda da laje de cima
  ("VZ-Projeção Pav. Superior/Inferior", texto PROJEÇÃO PAV. SUPERIOR) e contorno externo da cobertura (estilo de cobertura,
  senão "VZ-Projeção Preta"; PROJEÇÃO COBERTURA / (MARQUISE) quando passa da laje), tudo tracejado e sem os trechos sobre
  paredes. Paredes de cima desalinhadas poucos cm das de baixo geram linha solta: pergunte se alinha.
- Meio-fio e rebaixo (`06_meio_fio.cs` ["<início>:<pleno>"; opções]): pergunte o comprimento do rebaixo de veículos (mínimo
  3,00 m de trecho pleno) e a distância da divisa esquerda (olhando da rua). A skill gera a família "VZ-Meio-Fio com Rebaixo"
  (guia 10 cm, espelho 15 cm, sarjeta 30 cm, rampa 1,00 m na calçada em TRAPÉZIO com abas triangulares de 0,456 m na guia,
  material da calçada sem hachura e branco 255,255,254), refaz a laje da calçada com o recorte em trapézio, recria a cota de
  nível da calçada e põe o asfalto 15 cm abaixo. A família do template "Calçada, Meio-Fio, Rua" não acompanha os parâmetros: não use.
- Acesso: símbolo "VZ-Acesso Veículos"/"VZ-Acesso Pedestres" na calçada apontando para o lote (pelo questionário).
- Cota de nível em **todas** as plantas (layout e executivo) entre o nome do ambiente e a área: 1,4 mm no papel abaixo do
  ponto do identificador, alinhada a ele.
- Grama colorida também no executivo: o filtro "VZ-GRAMA PREENCHIDA" pega todo tipo de piso com "Grama" no nome (menos
  pisograma/concregrama). Se a grama sair branca, confira a regra do filtro.
- Ambientes abertos (varanda, sacada, garagem): crie o ambiente (pergunte o nome) — as portas para eles contam como vão externo.
- Conferência do modelo (fase 3): face externa dos muros na divisa e paredes da casa sem sobrepor os muros. Se a casa não
  couber entre os muros, pergunte: recuar as paredes da casa, casa na divisa sem muro ali, ou muro mais fino.

## Vários pavimentos (frequente)
Toda rotina vale para casas de um ou mais pavimentos ("PAV. X N.A" com ambientes):
- Plantas por pavimento (executivo, layout, tomadas, iluminação, forro, paginação) — `05_plantas.cs` já cria uma de cada.
- Cotas externas em todos os pavimentos, pela regra do escritório: 3 linhas por fora dos muros do lote, de divisa a divisa.
  O lote vem do térreo; no superior, as divisas precisam aparecer na vista (linhas de propriedade ou subposição do térreo).
- Cortes: cadeia vertical por ambiente, até a laje acima dele; à esquerda, piso acabado de cada pavimento + topo.
- **Terra nos cortes:** entre o nível do térreo e o nível CALÇADA, todo corte leva região "VZ-Terra" na largura do lote
  (de divisa a divisa; topo no TERREO N.O se existir). Pergunte a cota da calçada em relação ao térreo (ex.: −0,30 m) e rode
  `05_terra_cortes.cs` [vistas; cota; "refazer"]: move o nível CALÇADA, passa calçada e asfalto para ele e baixa o recorte
  dos cortes. Rode de novo ("refazer") sempre que criar ou mover um corte.
- Pranchas: em geral **um pavimento por folha**; se couber, dois na mesma folha. Decida com o modo `"testar"` de
  `05_montar_folhas_gerais.cs` (ver `references/gabarito-folhas.md`) e numere `i/N` pela quantidade final de folhas.
- Áreas: construída por pavimento + total (`calcular_areas_indices.cs`).
- **Escada não é detalhada** pela skill (aparece nas vistas como modelada, sem cotas nem detalhe próprio).

## Planta de layout / folha de proposta (fase 2) — regras do escritório
Valem por cima do que `02_proposta.cs` e `06_cotar_pranchas.cs` (modo `layout`) fazem hoje; confira na imagem exportada.
- **Pavimentos na mesma folha quando couber:** se as plantas de layout de todos os pavimentos couberem juntas na área útil
  do carimbo de proposta (x 25–832, y 58–584 mm), coloque-as **na mesma folha, uma abaixo da outra, alinhadas** (mesmo
  centro em X; térreo em cima), com a implantação de layout e a legenda na coluna livre ao lado. Só abra uma folha por
  pavimento se não couber. Apague a folha que sobrar e renumere `i/N` pela quantidade final (ex.: `1/1`); ajuste
  CONTEÚDO DA PRANCHA ("PROPOSTA DE LAYOUT - PAV. TÉRREO, PAV. SUPERIOR E IMPLANTAÇÃO").
- **Cotas internas justas às paredes:** largura e comprimento de cada ambiente (tipo "VZ-Cota Interna") com a linha de cota
  a **0,20 m** da face de uma das paredes (horizontal junto à parede de cima, vertical junto à da esquerda), correndo ao
  longo dela — nunca no meio do ambiente nem do lote. Assim o topo do número fica justo à parede. "VZ-Cota Interna" é
  cota **ordenada**: o Revit não deixa mover o texto (`TextPosition` falha) e ele fica na ponta, junto à parede lateral;
  "Deslocamento do texto" do tipo deve ficar em 0 (valores maiores empurram o número para dentro da parede). Para afastar o
  número da parede lateral é preciso mudar o tipo de cota — decisão do escritório, não da skill.
- **Cota do lote por fora, próxima dos muros, em TODOS os lados** (frente, laterais e fundos) — rotina
  `scripts/02_cotas_lote.cs` (`[vistas]`, `"refazer"`; padrão: todas as plantas com LAYOUT ou IMPLANTA no nome). Uma cota
  por lado, por fora da linha de divisa e **próxima dos muros**: **0,40 m** da divisa nas plantas 1:50 e **0,50 m** na implantação 1:100 (≈ 8 e 5 mm no
  papel), nas plantas de layout e na implantação, com o tipo de cota da escala da vista (ex.: "VZ-Cota 1-50"). Recorte das
  plantas só o necessário para caber a cota (~0,9 m além da divisa) e o mesmo recorte em todos os pavimentos, para
  continuarem alinhados na folha.
- **Símbolo de norte** (família "VZ-Norte") na folha, junto da implantação, girado pelo cardeal da frente informado no
  questionário e pela posição da frente na vista (lado voltado para a rua). **Junto à quina** do desenho: ~12 mm à direita e
  ~14 mm acima do canto superior do lote do lado da rua (por fora das cotas), nunca solto no canto da folha. Para achar a
  quina na folha: `view.GetModelToProjectionTransforms()[0].GetModelToProjectionTransform()` e depois
  `viewport.GetProjectionToSheetTransform()` (só o segundo trata o ponto como se fosse do modelo e joga o norte para fora
  da folha); confira que o ponto cai dentro da caixa da vista antes de mover. Reposicione o norte sempre que a vista mudar.
- **Nível do piso em cada ambiente:** cota de nível (tipo "VZ-Nível Planta") na face superior do piso do ambiente,
  posicionada **entre o nome do ambiente e os valores de área** do identificador.
- **Nome da rua na implantação** (padrão Família Agra, `PROJETO_REFERENCIA/FAMILIA AGRA - ARQ.pdf`, folha 1/7): o nome da
  rua da frente em texto grande (~4,5–5 mm no papel), **paralelo à testada**, **justo à calçada** (logo depois do
  meio-fio: borda do piso de asfalto/rua, ~0,15 m) e **alinhado pela esquerda na face do muro de divisa do lado esquerdo**
  (olhando o lote da rua) — sempre; logo depois, para fora, em texto de 2,5 mm, "RUA POSSUI PAV. ASFÁLTICA" quando o
  questionário disser que é asfaltada, com o mesmo alinhamento. Identifique os textos pelo conteúdo (TextNote não grava
  Comentários). Abra o recorte da implantação para o lado da rua o bastante para o texto caber e
  marque os textos com o comentário "SKILL RUA". Vale para a implantação de layout (fase 2) e para a implantação
  executiva (`06_implantacao.cs`, fase 6). Depois de abrir o recorte, reposicione a implantação, o norte e a legenda
  e confira na imagem que nada se sobrepõe.

## Área construída, área ocupada e hachuras da implantação — regras do escritório
Valem para a implantação de layout (fase 2), para as regiões de área (fase 8) e para a prévia de TO na abertura.
- **Área construída de cada pavimento** = contorno pela **face externa das alvenarias do corpo principal** daquele
  pavimento (área bruta, paredes inclusas — não a área útil dos ambientes) **+ áreas cobertas fora das paredes que
  avancem mais de 1,5 m** da alvenaria do corpo principal (varanda, garagem, sacada, área sob sacada/beiral — o tipo de
  piso não importa; "coberta" = telhado **ou laje** acima). Se avança mais de 1,5 m, **entra tudo**; até 1,5 m não entra.
  Descoberto não entra (jardim, quintal, sacada sem cobertura, piscina, calçada fora do lote).
  - Térreo: candidata = o próprio lote; pavimentos de cima: candidata = o piso modelado daquele pavimento.
  - Cobertura = telhados e pisos/lajes de níveis acima do pavimento. Confira na planta de cobertura.
  - **O avanço é medido de cada cobertura a partir do corpo de onde ELA sai** (o pavimento logo abaixo dela), nunca da
    parede do pavimento que está sendo medido: o telhado/marquise sai do corpo do último pavimento; a laje do superior
    sai do corpo do térreo. Ex.: marquise de 1,49 m na frente do quarto do superior fica fora — mesmo estando a 3,8 m da
    parede do térreo. Nunca junte numa mancha só coberturas que saem de corpos diferentes.
  - **Muros de divisa não são corpo principal:** exclua paredes baixas (até ~2,6 m) que correm junto à divisa.
  - Método: projetar sólidos no plano (`ExtrusionAnalyzer`), unir/subtrair/intersectar (`BooleanOperationsUtils`),
    para cada cobertura, pegue as peças fora do corpo de onde ela sai e teste o avanço com uma **faixa de 1,5 m em volta
    desse corpo** (`CurveLoop.CreateViaOffset` do contorno externo): a peça entra inteira só se sobrar área além da faixa.
    **Não use a caixa envolvente** (menor dimensão do bbox): peças em L somam lados rasos e parecem avançar muito. Descarte
    retalhos < 0,5 m². Mostre ao arquiteto, por peça, a área, de onde sai, quanto passa de 1,5 m e se entra.
- **Beiral e marquise:** só entram na área ocupada e na construída se avançarem **mais de 1,5 m** da alvenaria do corpo
  principal — e aí entram inteiros.
- **Área ocupada** = projeção da edificação = união das áreas construídas dos pavimentos (com a mesma regra dos 1,5 m);
  TO = ocupada ÷ área do lote.
- **Implantação de layout:** mostra as áreas construídas de **cada pavimento**, cada uma com a sua hachura
  ("VZ-Área Construída Pavimento Térreo", "VZ-Área Construída Pavimento Superior"), sobrepostas; sem contorno grosso de
  projeção. A legenda "IMPLANTAÇÃO LAYOUT" vem do template com valores de outro projeto: atualize área de cada pavimento,
  área do terreno, área ocupada (m² e %) e área total construída; confira se a amostra de cada pavimento usa a hachura
  certa (no template a do superior vem com a do térreo) e corrija "PÁVIMENTO" → "PAVIMENTO".
- **Rotina automática `scripts/02_areas_legenda.cs`** (fase 2, de novo na fase 8 e sempre que o modelo mudar): calcula as
  áreas pelas regras acima, redesenha as hachuras da implantação de layout e **atualiza a legenda sozinha** com os valores
  já determinados (textos achados pelo conteúdo, não por Id); grava PAV. TÉRREO/SUPERIOR, ÁREA CONSTRUÍDA/OCUPADA/
  PERMEÁVEL/PISCINA e as taxas nas Informações do projeto. Parâmetros: `[vista]`, `[legenda]`, `"simular"`. Não edite a
  legenda à mão.
  - **Área permeável = piso de grama** (pisos com "GRAMA" no nome) dentro do lote, hachura "VZ-Área Permeável"; na
    legenda: "ÁREA PERMEÁVEL" / "A = x M²" / "y%" (o mínimo exigido só entra se o questionário trouxer a TP da zona).
  - **Sem piscina no projeto** (nenhum piso "PISCINA" no lote): a amostra e os textos da piscina **não aparecem** na
    legenda (ocultos na vista; voltam sozinhos se a piscina for modelada).
  - **Contorno sem "dentes":** pontas de parede da casa que entram nos muros deixam saliências de 10–25 cm; a rotina tira
    dos vértices toda saliência retangular com lados ≤ 0,16 m e largura ≤ 0,32 m e os vértices colineares (o
    desloca-e-devolve com `CreateViaOffset` falha calado nesses contornos). As áreas da legenda saem das regiões finais.
  - **Ordem de desenho:** regiões criadas de baixo para cima — permeável, piscina, térreo, superior — porque a última
    fica por cima e o fundo dela esconde a hachura de baixo.
  - Scripts novos só chegam aos outros computadores depois de refazer o pacote da skill (`installer/REFAZER_ZIP_SKILL.cmd`).

## Checklist executivo do escritório
`references/checklist-executivo.md` liga cada item do checklist unificado do escritório à skill: o que ela **faz**, o que ela
**confere** (aponta no relatório das fases 3 e 10) e o que é da **equipe**. Use-o para responder "isso a skill faz?" e, ao fim de
cada fase, para lembrar a equipe dos itens manuais daquela etapa.

## Três camadas (não misture)
- **Método** (esta skill): fases, regras, scripts. Igual para todos os escritórios.
- **Perfil do escritório**: `config/escritorio.json`. Nome, prefixo, responsáveis, carimbos, caminhos. Nunca escreva dados de um escritório no código.
- **Dados do projeto**: Informações do projeto no .rvt + `<projeto>.skill.json` ao lado do arquivo (estado das fases).

## Regras que valem em todas as fases
1. **Teste a conexão com o Revit antes de cada fase** (`scripts/00_preflight.cs`). Se falhar, oriente: ligar o servidor do MCP na aba de suplementos, fechar caixas de diálogo abertas no Revit, aguardar carregamento. Não siga sem conexão.
2. **Nunca trabalhe no arquivo de um projeto em andamento de outra pessoa nem no template master.** Projetos novos nascem de uma cópia local do master.
3. **Servidor é lento:** não abra, crie nem salve arquivos .rvt/.rte grandes pela rede durante o trabalho. Use a cópia local do template (`01_preparar_template_local.cs`) e salve o projeto onde o perfil indicar.
4. **Nada é salvo sem confirmação** do usuário. Toda alteração em transação nomeada ("Skill: …"), reversível pelo Desfazer.
5. **Localize documentos pelo caminho exato**, nunca pelo "documento ativo", quando houver mais de um arquivo aberto no Revit.
6. Execute scripts com `send_code_to_revit`. Operações longas (salvar, copiar) podem passar de 80 s: espere e confirme o resultado com uma consulta curta antes de repetir qualquer coisa.
7. A API do Revit **não edita rótulos de famílias** (carimbos, identificadores). Se for preciso, peça ao usuário ou use o controle da tela com permissão.
8. **Avisos do Revit travam o MCP.** Um aviso (ex.: "inserções se sobrepõem") abre uma janela modal e a chamada fica sem resposta até alguém clicar. Envie todo script que cria ou altera elementos envolvido neste bloco, que apaga os avisos (erros continuam aparecendo):
   ```
   EventHandler<Autodesk.Revit.DB.Events.FailuresProcessingEventArgs> __avisos = (__s, __e) => { __e.GetFailuresAccessor().DeleteAllWarnings(); };
   document.Application.FailuresProcessing += __avisos;
   try {
     <código do script>
   } finally { document.Application.FailuresProcessing -= __avisos; }
   ```
   Se mesmo assim o Revit parar de responder, peça para olhar a tela (controle do computador) e confirme a janela com o usuário.
9. **Ambientes são sempre sugestão, nunca imposição.** Ao receber o modelo (fase 1) e na conferência (fase 3), confira os
   ambientes de cada pavimento. Se houver circuito fechado sem ambiente, ambiente sem nome, "A DEFINIR" ou nome fora do padrão
   (minúsculas, número colado no nome), **pergunte ao projetista: "Posso colocar/sugerir os nomes dos ambientes?"**. Com o sim,
   rode `03_criar_ambientes.cs` em modo `"propor"` (transação desfeita), mostre a lista numerada (circuito, pavimento, nome
   sugerido, área) e só rode `"aplicar"` com os nomes que o projetista aprovar ou corrigir. Sem o sim, registre a pendência e siga.
10. **Nunca desfaça montagens (assemblies) por script** — o Revit pede confirmação numa janela modal ("a exclusão desta montagem removerá todas as vistas associadas"). Apagar uma vista de montagem é seguro.

## Fases
| # | Fase | Referência | Status |
|---|---|---|---|
| 0 | Pré-voo | `references/fases/00-pre-voo.md` | pronta |
| 1 | Recebimento do projeto modelado (dados do carimbo, estado da skill) | `references/fases/01-abertura.md` | pronta |
| 2 | Planta de layout: anotação e folha de proposta (+ `02_desenhar_lote.cs` quando não há linhas de propriedade); cotas do lote nos 4 lados (`02_cotas_lote.cs`); hachuras de área e legenda automática (`02_areas_legenda.cs`) | `references/fases/02-proposta.md` + seções "Planta de layout" e "Área construída" acima | pronta — testada na referência, no piloto e no projeto teste de 2 pavimentos (18/11/2026) |
| 3 | Conferência do modelo + regras de projeto do checklist (corrige o que o arquiteto aprovar) | `references/fases/03-conferencia.md` | pronta — testada no projeto de referência |
| 4 | Ambientes de portas e janelas, nomes de ambientes | `references/fases/04-ambientes.md` | pronta — testada no projeto de referência |
| 5 | Vistas e folhas do caderno ARQ (1/N a N/N; 7 folhas numa casa térrea); terra nos cortes e nível da calçada (`05_terra_cortes.cs`) | `references/fases/05-vistas.md` | pronta — testada no projeto de referência |
| 6 | Anotação: identificadores, níveis, cotas em 3 escalas, paisagismo oculto (`06_anotar_planta.cs`); cotas por prancha e eixos hidrossanitários (`06_cotar_pranchas.cs`); textos, paginação, ar e hidro (`06_textos.cs`); implantação e recuos (`06_implantacao.cs`); elementos externos em proposta (`06_elementos_externos.cs`); projeções tracejadas (`06_projecoes.cs`); meio-fio, sarjeta e rebaixo com família gerada (`06_meio_fio.cs`) | `references/fases/06-anotacao.md` | pronta — testada no projeto de referência |
| 7 | Tabelas (vêm do template; dados das fases 3 e 4) | `references/fases/07-tabelas.md` | pronta |
| 8 | Carimbo: dados, áreas e índices (`02_areas_legenda.cs` de novo, com o modelo final, + `08_regioes_areas.cs` + `calcular_areas_indices.cs`), QR code (`08_qrcode.cs`) | `references/fases/08-folhas-carimbo.md` | pronta — validada na referência e no piloto |
| 9 | Caderno DET (montagens + folhas A3) | `references/fases/09-caderno-det.md` | pronta — testada no projeto de referência (telhado manual) |
| 10 | Conferência final | `references/fases/10-conferencia-final.md` | pronta — testada no projeto de referência |
| 11 | Emissão | `references/fases/11-emissao.md` | pronta — testada no projeto de referência |

## Como começar uma conversa
1. Leia `config/escritorio.json`. Se não existir ou estiver com marcadores `<<…>>`, faça primeiro o cadastro do escritório (ver `references/perfil-escritorio.md`).
2. Rode a fase 0.
3. Pergunte qual é o **projeto modelado** a detalhar (caminho do .rvt) e se é o **início do detalhamento** (fase 1) ou uma
   **retomada** (leia `<projeto>.skill.json` e continue da fase registrada). Pergunte também quantos pavimentos a casa tem.
4. Logo após receber o modelo, aplique a regra 9 (ambientes como sugestão) antes de seguir para a fase 2.
5. Ao fim de cada fase, grave o estado em `<projeto>.skill.json` e diga ao usuário qual é a próxima fase.

## Referências
- `references/questionario-abertura.md` — perguntas da abertura e onde cada resposta é gravada
- `references/perfil-escritorio.md` — campos do perfil do escritório
- `references/parametros.md` — parâmetros compartilhados da skill (GUIDs fixos)
- `references/receitas-pranchas.md` — conteúdo de cada prancha (do projeto de referência)
- `references/checklist-executivo.md` — checklist executivo do escritório × o que a skill faz, confere ou deixa com a equipe
- `references/relatorio-piloto.md` — piloto num projeto novo: o que foi corrigido e pendências do template master
- `scripts/` — rotinas C# para o MCP do Revit

**Manutenção do template:** `scripts/00_limpar_template.cs` (relatório; limpeza só numa cópia do master — ver `references/relatorio-piloto.md`).