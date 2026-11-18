# Changelog

## 0.9.0 — 18/11/2026
Rodada no projeto teste de 2 pavimentos (fases 4 e 5, planta executiva do térreo). Regras novas no SKILL.md e três rotinas novas.
- **Ordem de trabalho folha por folha:** vistas da folha → anotação completa → conferência por imagem → montagem →
  aprovação. Cortes, fachadas e perspectivas só depois da planta executiva anotada e aprovada.
- **Terra nos cortes e nível da calçada** (`05_terra_cortes.cs`, novo): pergunta a cota da calçada em relação ao térreo
  (ex.: −0,30), move o nível CALÇADA, passa calçada e asfalto para ele, desenha "VZ-Terra" de divisa a divisa entre a calçada
  e o térreo em todos os cortes e baixa o recorte. Rodar de novo ("refazer") quando criar ou mover corte.
- **Cotas externas refinadas** (`06_anotar_planta.cs`): recorte da vista ligado e justo às cotas (o do template vinha
  desligado); divisa prevalece sobre muro a menos de 1 cm; do lado da rua as linhas ficam depois do meio-fio e da sarjeta, com
  a 1ª linha do **rebaixo**; nas faces perpendiculares à rua a cadeia segue até a guia e mostra a **largura da calçada**; borda
  de piso dentro da espessura de parede não vira referência; texto de trecho curto sai ao longo da linha para o lado livre;
  portas para ambientes abertos (varanda, sacada, garagem) contam como vão externo; cota de nível entre o nome e a área.
- **Projeções no executivo** (`06_projecoes.cs`, novo): paredes do pavimento de cima, borda da laje de cima (PROJEÇÃO PAV.
  SUPERIOR) e contorno externo da cobertura (PROJEÇÃO COBERTURA / MARQUISE), tracejados, sem os trechos sobre paredes.
- **Meio-fio e rebaixo** (`06_meio_fio.cs`, novo): a skill gera a família "VZ-Meio-Fio com Rebaixo" com as medidas do lote
  (guia 10 cm, espelho 15 cm, sarjeta 30 cm, rebaixo ≥ 3,00 m pleno, rampa em trapézio com abas triangulares, material da
  calçada sem hachura e branco 255,255,254), com planos de referência nomeados para cotar; refaz a laje da calçada com o
  recorte da rampa, recria a cota de nível e põe o asfalto 15 cm abaixo.
- **Conferência do modelo:** muros com a face externa na divisa e paredes da casa sem sobrepor os muros (se não couber,
  perguntar); paredes de cima desalinhadas poucos cm das de baixo → perguntar se alinha; ambientes abertos sem ambiente
  (varanda) → criar com o nome que o projetista aprovar.
- **Grama colorida no executivo:** o filtro "VZ-GRAMA PREENCHIDA" passa a pegar todo piso com "Grama" no nome (menos
  pisograma/concregrama).
- **Acesso:** símbolos "VZ-Acesso Veículos/Pedestres" na calçada apontando para o lote.

## 0.8.0 — 18/11/2026
Rodada no projeto teste de 2 pavimentos (fases 0 a 2). Regras novas no SKILL.md e duas rotinas novas.
- **Ambientes como sugestão** (regra 9): ao receber o modelo e na fase 3, a skill pergunta "Posso colocar/sugerir os nomes
  dos ambientes?" e só aplica o que o projetista aprovar.
- **Área do lote pela linha de divisa** (preta tracejada; a azul é recuo), conferida pela geometria; com mais de uma linha,
  o projetista seleciona a do lote.
- **Linha de recuo × recuos informados:** tabela por lado e a pergunta "Ajustar a linha de recúo aos valores informados?".
- **Folha de proposta:** pavimentos na mesma folha quando couber, alinhados um abaixo do outro; cotas internas a 0,20 m da
  parede; nível do piso entre o nome e a área do ambiente; norte junto à quina da implantação; nome da rua justo à
  calçada, alinhado ao muro esquerdo, com "RUA POSSUI PAV. ASFÁLTICA" (padrão Família Agra).
- **Cotas do lote nos 4 lados** (`02_cotas_lote.cs`, novo): frente, laterais e fundos, a 0,40 m da divisa em 1:50 e a 0,50 m
  em 1:100, nas plantas de layout e nas implantações.
- **Área construída / ocupada / permeável** (`02_areas_legenda.cs`, novo): construída = face externa das alvenarias do corpo
  principal (sem muros) + áreas cobertas que avançam mais de 1,5 m do corpo de onde a cobertura sai (entra inteira);
  ocupada = união das construídas; permeável = piso de grama. Hachura por pavimento sem "dentes", desenhadas de baixo para
  cima, **legenda atualizada automaticamente** (piscina some da legenda quando não existe) e valores gravados nas
  Informações do projeto.
- **Template master (rodada 3):** áreas e linhas de limite do lote de exemplo apagadas (0 avisos), legendas de implantação
  corrigidas e sem valores fixos, Janela Padrão 1,50 × 1,20 com peitoril 1,00, marcas de porta no padrão Pnn. Backup em
  `01_TEMPLATE/_backup/`; detalhes em `01_TEMPLATE/LOG_MASTER_v1.md`.
- `ATUALIZAR_SKILL_TIME.cmd` (novo, na pasta installer): gera o ZIP da skill para enviar no Claude e o instalador .exe do time.

## 0.7.0 — 17/11/2026
Aprimoramentos a partir do checklist executivo do escritório. **Regra nova:** recebida a modelagem, a skill faz o
executivo inteiro. Elementos físicos ela propõe para aprovação, regras de projeto ela corrige com aprovação, e os dados de
fora do modelo vêm do questionário. `skill/references/checklist-executivo.md` liga cada item ao que a skill faz.
- **Implantação** (`06_implantacao.cs`, novo): rua, largura, pavimentação, lotes vizinhos, numeração predial, norte e
  linhas de recuo em azul tracejado (estilo "VZ-RECUO"), na implantação e no executivo do térreo.
- **Elementos externos** (`06_elementos_externos.cs`, novo): propõe abrigo de medidores, gás, caixa de captação, lixeira,
  rebaixo do meio-fio, ducha e piso tátil; depois "aprovar" ou "desfazer".
- **Correção das regras** (`03_corrigir_modelo.cs`): `peitoril`, `portas_largura` (troca de tipo na mesma família) e
  `bancadas` (parâmetros da família; duplica o tipo compartilhado), só nos ids aprovados.
- **QR code no carimbo** (`08_qrcode.cs`, novo): imagem gerada pela skill colocada em todas as folhas ARQ/DET.
- **Paginação** (`06_textos.cs` modo `paginacao`): piso por ambiente, início da paginação na porta, nota de ralos e canaletas.
- **Eixos hidrossanitários amarrados** (`06_cotar_pranchas.cs` modo `hidro`).
- **Questionário**: asfalto, numeração predial, lote de fundos, recuos, lado da garagem, tipo de construção, link do QR code.
- **Instalador 1.4**: o ZIP da skill é montado pelo nome das pastas (antes, em computadores com o TEMP em nome curto 8.3, o
  SKILL.md ia para "executivo-revit/<sobra>/SKILL.md" e o Claude recusava); o instalador confere o ZIP e, se precisar,
  remonta sozinho. `REFAZER_ZIP_SKILL.cmd` corrige ZIPs gerados por instaladores antigos. Ação do GitHub que gera o `.exe`
  em cada release.
- **Cotas em três escalas** (`06_anotar_planta.cs`): micro (vãos de portas e janelas da fachada), médio (ambiente por
  ambiente) e macro (total do terreno), por fora do lote e em todas as plantas. A linha de blocos foi substituída pela micro.
- **Conferência do modelo** (`03_conferir_modelo.cs`): regras de projeto do checklist como ATENÇÃO (peitoril 1,00 m, porta de
  entrada ≥ 1,30 m pivotante, lavabo 0,70 m, alumínio em corredor 0,90 m, vaga 3,00 × 5,00 m, bancadas, churrasqueira, topografia).
- **Anotação**:
  - identificador de ambiente com perímetro (quando o template tem o tipo);
  - paisagismo escondido fora da planta de layout;
  - "PROJEÇÃO PERGOLADO";
  - X tracejado nos vazios;
  - tomadas com as alturas padrão do checklist;
  - altura das luminárias de parede e prateleira.
- **Plantas complementares**: ar-condicionado (BTU, modelo, distância evaporadora–condensadora), hidro e gás (chamadas com
  altura) e iluminação externa. Vistas em `05_plantas.cs`, textos em `06_textos.cs` (modos `ar` e `hidro`) e folhas em
  `05_montar_folhas_gerais.cs` (`ar`, `hidro`, `ilumext`). São criadas só quando há pontos modelados.
- **Conferência final** (`10_conferencia_final.cs`): seção "CHECKLIST EXECUTIVO" com campos do carimbo, implantação, elementos,
  legenda, paisagismo e colunas das tabelas.
- Mudanças ainda não testadas no Revit: validar no piloto antes de usar em projeto.

## 0.6.0 — 16/11/2026
- **Escopo:** a equipe modela (inclusive o layout); a skill faz só o detalhamento técnico executivo. Na planta de layout ela
  anota (nomes, cotas em texto, acabamentos) e diagrama. A conferência do modelo só relata; correções só a pedido da equipe.
- **Vários pavimentos:**
  - `06_anotar_planta.cs`: cotas externas em todos os pavimentos pela regra do lote (divisas do térreo); muro identificado
    por não delimitar ambiente interno (ou tipo "MURO"), não só pela altura; aviso quando as divisas não aparecem no superior.
  - `06_cotar_pranchas.cs` (corte): cadeia de cada ambiente até a laje logo acima; linha à esquerda com o piso de cada pavimento + topo.
  - `05_montar_folhas_gerais.cs`: vários pavimentos por folha (`"TERREO;SUPERIOR"`), modo `"testar"` (COUBE/NAO_COUBE, sem
    gravar), implantação e tabelas só na folha do térreo, nome "PRANCHA <i>" pelo número, conteúdo com o pavimento.
  - `05_montar_folhas_ambientes.cs`: só as vistas do pavimento da planta geral; nome e conteúdo por pavimento.
  - `05_propor_cortes.cs`: nível vazio = ambientes de todos os pavimentos.
  - Plano do caderno para sobrados em `references/gabarito-folhas.md` (um pavimento por folha; dois se couber; numeração i/N).
- Escada fora do escopo de detalhamento.
- Instalador: detecta se o Claude Desktop está instalado (com link para baixar); liga o MCP ao Claude Desktop por conta
  própria (`configurar-mcp-claude.ps1` / `CONFIGURAR_MCP_CLAUDE.cmd`: todas as pastas de configuração, UTF-8 sem BOM);
  correção do erro de desenho da janela durante a instalação.
- Instalador 1.3: o ZIP da skill é gravado com "/" nos caminhos (o Compress-Archive do Windows PowerShell 5.1 grava "\\" e o
  Claude recusava com "Zip file contains path with invalid characters"); `REFAZER_ZIP_SKILL.cmd` corrige um ZIP já gerado.

## 0.5.0 — 15/11/2026
Cotas e textos no padrão do projeto de referência, folhas de casa pequena em duas colunas, limpeza do template e instalador.

### Novo
- `06_anotar_planta.cs` (cotas da planta executiva): regra do escritório — nenhuma cota dentro do lote; nas 4 faces, por fora
  dos muros, 3 linhas: cadeia detalhada (limites de paredes e pisos, sem vãos), blocos (divisas, muros, extremos da
  edificação) e total. Linhas a 5 mm do desenho e a cada 6,5 mm; trechos curtos com texto escalonado.
- `06_cotar_pranchas.cs`: cotas por prancha — layout (dimensões só em texto no canto do ambiente), tomadas (parede → ponto →
  ponto → parede), iluminação (pela fileira; luminária linear pelas pontas), cobertura (corpo × beiral + total) e cortes
  (cadeia vertical por ambiente: piso → forro → laje + altura total).
- `06_textos.cs`: anotações de texto a partir do modelo — PROJEÇÃO COBERTURA/EDIFICAÇÃO, telha + inclinação, chamadas de
  telha/laje/forro nos cortes, REVEST./PISO por ambiente, materiais das fachadas, FORRO= h, tomadas fora da altura padrão,
  louças e material das bancadas no DET.
- `00_limpar_template.cs`: relatório e limpeza das sobras do projeto-exemplo no template master (só numa cópia).
- `05_montar_folhas_gerais.cs`: duas colunas quando as vistas cabem lado a lado (casa pequena).
- Instalador: `installer/instalar-skill.ps1` + `INSTALAR.cmd` (baixa a versão mais recente do GitHub, confere Revit/MCP/
  pasta-base, grava o perfil, gera o ZIP da skill e registra a versão) e `COMPILAR_EXE.cmd` (gera o .exe com ps2exe).
  Na primeira instalação, instala também o MCP do Revit (suplemento, Node.js e configuração do Claude Desktop) pelo
  instalador oficial do projeto do MCP; lê a porta do servidor em `mcp-port.txt`.
- Instalador 1.1 (`InstaladorSkillExecutivoRevit.exe`): a skill vai embutida no .exe (`montar-pacote.ps1`), todas as
  perguntas no começo, ZIP com a pasta no nome da skill (`executivo-revit`), guia em PDF e atalhos instalados, e ao final
  abre a pasta do ZIP e a página "Próximos passos" (o que só o usuário pode fazer no Claude e no Revit).
- Instalador 1.2: janela no visual do guia (Poppins embutida, lateral escura, acento vermelho), assistente em 5 telas
  (Boas-vindas, Configuração, Perfil, Instalação com progresso por etapa, Concluído com os passos manuais), ícone próprio,
  escala para telas de alta resolução e `-Previa <pasta>` para gerar imagens das telas.

### Corrigido
- Título das bancadas no DET com o ambiente ("BANCADA B02 - COZINHA").
- Cotas: `Dimension.Origin` lança exceção em cota de vários trechos; cotas não têm parâmetro Comentários (marcação por posição).

## 0.4.0 — 14/11/2026
Piloto num projeto novo: casa térrea 10 × 10 m criada do zero a partir do template master e levada da abertura à emissão
(PROPOSTA 1 folha, ARQ 7 folhas A1, DET 3 folhas A3). Relatório completo em `skill/references/relatorio-piloto.md`.

### Novo
- `02_desenhar_lote.cs`: desenha lote (das linhas de propriedade ou dos muros de divisa), projeção da edificação, cotas de
  testada/profundidade e recorte nas vistas de implantação — sem isso, num projeto sem linhas de propriedade, a implantação sai vazia.
- `08_regioes_areas.cs`: gera as regiões de área construída, permeável e de piscina a partir dos pisos do modelo
  (antes as áreas do carimbo só funcionavam com regiões desenhadas à mão).
- Regra 8 do SKILL.md: bloco que apaga avisos do Revit em todo script — avisos abriam janelas modais e travavam o MCP.

### Corrigido
- Fase 3: aviso de marcas de tipo fora do padrão e opção `renumerar_esquadrias` (P01…, J01…).
- Fase 4: porta entre ambientes internos vale o mais reservado (banheiro > quarto > serviço > cozinha).
- Fase 5: corte da área molhada pelo banheiro principal e afastado de paredes paralelas; implantação conferida por linhas de
  lote (não por telhados); legenda abaixo da vista quando à direita ela chegaria ao carimbo.
- Fases 2 e 9: CONTEÚDO DA PRANCHA preenchido; fase 9 remove as folhas DET vazias do template antes de numerar.
- Fase 10: vistas de pavimentos sem ambientes não entram no aviso de vistas fora de folha.

### Pendências do template master (encontradas no piloto)
Linhas de propriedade e regiões de área de um lote de exemplo; legenda de implantação com valores fixos; "Janela Padrão" de 2,50 m;
marcas de porta fora do padrão; famílias de porta sem ABERTURA/MATERIAL; arquivo de ~200 MB a expurgar.

## 0.3.0 — 14/11/2026
Todas as fases com rotina e documentação, testadas numa cópia do projeto de referência e conferidas por imagem
contra as folhas emitidas pelo arquiteto.

### Novas fases
- **Fase 2 — Proposta de layout** (`02_proposta.cs`): uma folha por pavimento, planta centralizada no alto, implantação
  e legenda na primeira folha. Posições a ~10 mm das do arquiteto.
- **Fase 3 — Conferência do modelo** (`03_conferir_modelo.cs`, `03_corrigir_modelo.cs`): pendências em três níveis
  (BLOQUEIA / CORRIGIR / ATENÇÃO), incluindo esquadrias sem marca de tipo; correção de ambientes não colocados e paredes curtas.
- **Fase 4 — Ambientes** (`04_ambientes.cs`): campo AMBIENTE por tipo de porta/janela a partir dos ambientes que cada
  uma liga; agrupa quartos, WCs e circulação; portas externas como "<ambiente>/ÁREA EXTERNA"; sem ambiente = "FACHADA <lado>".
  Só preenche vazios, a menos que o usuário peça para sobrescrever.
- **Fase 6 — Anotação** (`06_anotar_planta.cs`): identificadores de portas, janelas e ambientes, cotas de nível de piso
  e cotas internas; idempotente. No teste: mesmas 23 portas e 10 janelas identificadas pelo arquiteto.
- **Fase 7 — Tabelas** e **Fase 8 — Carimbo**: documentadas (tabelas vêm do template; dados das fases 3 e 4; áreas e índices).
- **Fase 9 — Caderno DET** (`09_montagens.cs`, `09_montar_folhas_det.cs`): montagens de esquadrias (1 por marca de tipo),
  bancadas (com louças) e pergolados/churrasqueira; elevação escolhida pela largura real da peça (algumas famílias têm
  eixos girados); recorte ajustado à peça; vista de topo girada para alinhar com a elevação; folhas A3 empacotadas
  com as tabelas de portas e janelas no rodapé; modo `refazer_elevacoes`.
- **Fase 10 — Conferência final** (`10_conferencia_final.cs`) e **Fase 11 — Emissão** (`11_emitir.cs`).

### Cuidados registrados
- Desfazer montagem por script abre um diálogo modal no Revit e trava o MCP: a skill nunca desfaz montagens.

### Continua manual
- Folha de telhado do DET (madeiramento), cotas L/H das esquadrias, textos de especificação das bancadas.
- Logotipo do escritório no carimbo; levar ao template master a hachura e a região do lote criadas pela fase 5.

### Pendente
- Piloto num projeto real; folha extra do pavimento superior em sobrados; instalador `.exe`.

## 0.2.0 — 14/11/2026
Fase 5 completa: vistas e montagem automática das 7 folhas do caderno ARQ, testadas numa cópia do projeto de referência
e conferidas por imagem contra as folhas emitidas pelo arquiteto.

### Vistas
- `05_plantas.cs`: usa as plantas que já vêm no template; cria só as que faltarem e avisa as que estão sem conteúdo.
- `05_situacao.cs` (novo): planta de situação vetorial a partir das medidas do lote no modelo e de dados do questionário.
- `05_situacao_importar.cs` (novo): alternativa com a imagem/PDF da planta do loteamento.
- `05_propor_cortes.cs`: **correção** — o plano de corte ficava na borda da casa e não no eixo pedido; margens verticais menores.
- `05_criar_fachadas.cs`: oculta o muro de divisa na frente de cada fachada; extensão sem a rua; recorte vertical mais justo.
- `05_vistas_ambientes.cs`: iluminação na parede com louças (áreas molhadas) ou com mais pontos elétricos; paginação com
  4 paredes num marcador só e isométricas mostrando o interior (caixa na face interna das paredes, abaixo do forro).
- `05_perspectivas.cs` (novo): perspectivas dos cortes (escolhe o corte que mostra a menor parte da casa) e das laterais.

### Folhas
- `05_montar_folhas.cs` (3/7 e 4/7), `05_montar_folhas_ambientes.cs` (6/7 e 7/7) e `05_montar_folhas_gerais.cs` (1/7, 2/7 e 5/7):
  vistas empilhadas pela altura real, tabelas ancoradas na base, legendas à direita ou embaixo, escala das perspectivas
  ajustada quando não cabem, conferência de área útil (moldura menos o carimbo) e de sobreposição.

### Documentação
- `references/fases/05-vistas.md` (novo) e `references/gabarito-folhas.md` (novo).
- Questionário: bloco "Situação do lote".

### Pendente
- Fases 2, 3, 4 e 6 a 11; folha extra para o pavimento superior em sobrados; instalador `.exe`.
- Incluir no template master o padrão "VZ-Hachura Cruzada 45" e a região "VZ-Lote do Projeto" (hoje criados pelo script).

## 0.1.0 — 14/11/2026
Primeira versão.

### Template master v1 (fora do repositório, na pasta-base)
- Template do escritório corrigido em 7 rodadas: carimbos unificados por caderno, parâmetros do carimbo refeitos (áreas, taxas em %, RRT, ART, conteúdo da prancha, observações gerais), parâmetro único AMBIENTE, fonte Arial, tipos de texto de 38 para 15, limpeza de ambientes e parâmetros órfãos.

### Skill
- `SKILL.md` com regras gerais e as 12 fases.
- Fase 0 (pré-voo) e fase 1 (abertura) documentadas e com scripts testados no Revit 2025.
- Rotina de áreas e índices validada contra o PDF emitido do projeto de referência.

### Instalador
- `PUXAR_PASTA_BASE.cmd` / `puxar-pasta-base.ps1`: unidade de rede, pasta-base, template, Revit e MCP; grava a configuração local.

### Pendente
- Fases 2 a 11 (proposta, vistas, folhas, DET, conferência, emissão).
- Instalador `.exe`.
