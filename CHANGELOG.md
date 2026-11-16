# Changelog

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
