# Changelog

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
