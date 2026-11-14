# Especificação — Skill de Projeto Executivo de Arquitetura no Revit

Versão 0.4 · 13/11/2026 · Documento-base para a conversa de construção da skill.
Levantamento detalhado do projeto de referência: mantido fora do repositório (contém dados de cliente). Resumo das pranchas em `skill/references/receitas-pranchas.md`.

---

## 1. Objetivo e escopo

Skill do Claude que conduz um projeto de arquitetura no Revit, do arquivo zero à emissão, de forma padronizada e automatizada via MCP do Revit.

- **Genérica:** serve a qualquer escritório. Nada de nome, prefixo ou padrão de um escritório no código da skill; tudo isso vive no perfil do escritório.
- **Etapas cobertas:** proposta de layout + projeto executivo (caderno ARQ + caderno DET).
- **Ponto de partida:** arquivo novo criado a partir de um template base, usando um projeto de referência como modelo de resultado.
- **Ferramenta de execução:** MCP do Revit (ferramentas nativas + `send_code_to_revit` para rotinas C#).

## 2. Arquitetura em três camadas

| Camada | O que contém | Onde vive |
|---|---|---|
| **Método** | fases, pontos de aprovação, receitas de prancha, checklists, parâmetros de normas, scripts C# | dentro da skill (igual para todos) |
| **Perfil do escritório** | nome, logo, endereço, contatos, responsável técnico, CAU, prefixo de nomenclatura, carimbos, caminho do template, configuração de exportação, observações-padrão | `config/escritorio.json` (preenchido uma vez) |
| **Dados do projeto** | cliente, obra, lote, programa, decisões, estado das fases | parâmetros do próprio .rvt + `projeto.json` |

O estado de cada projeto (fase atual, decisões, pendências) é gravado dentro do .rvt com `store_project_data` / `query_stored_data`, para qualquer conversa nova retomar de onde parou.

## 3. Distribuição

- **Plano:** contas individuais do Claude (sem provisionamento por organização).
- **Fonte:** repositório GitHub.
- **Instalador/atualizador `.exe`:**
  1. baixa a versão mais recente do GitHub;
  2. confere pré-requisitos: Revit instalado, MCP do Revit instalado e respondendo, acesso à pasta-base;
  3. pede/grava o perfil do escritório (primeira instalação);
  4. gera o ZIP da skill já configurado;
  5. abre a tela de Skills do Claude (Customize > Skills > Upload) com o ZIP pronto — **o upload é manual** em cada conta;
  6. registra a versão instalada e avisa quando houver atualização.
- **Pasta-base no servidor:** mesmo caminho em todas as máquinas. Unidade de rede mapeada **não pode ser solicitada pelo Claude**; o usuário precisa conectá-la manualmente no app desktop ("Adicionar pasta"). O instalador e a fase 0 da skill orientam isso.

### Estrutura do repositório
```
skill-executivo-revit/
├── skill/                         → vira o ZIP
│   ├── SKILL.md                   → orquestrador (curto)
│   ├── references/
│   │   ├── fases/                 → um arquivo por fase
│   │   ├── receitas-pranchas.md
│   │   ├── carimbo-e-parametros.md
│   │   ├── catalogo-ambientes.md
│   │   ├── normas.md
│   │   ├── correcoes-master.md
│   │   └── checklists/
│   ├── scripts/                   → rotinas C# testadas para send_code_to_revit
│   └── config/escritorio.json
├── installer/                     → código-fonte do .exe
├── pasta-base-modelo/             → estrutura que o servidor deve ter
└── CHANGELOG.md
```

### Estrutura da pasta-base (servidor)
```
[pasta-base]/
├── 01_TEMPLATE/              → .rte master
├── 02_PROJETO_REFERENCIA/    → .rvt + PDFs emitidos
├── 03_DOCUMENTOS/            → manuais, checklists, memoriais, modelos
├── 04_BIBLIOTECA_DETALHES/
└── 05_FAMILIAS/
```

## 4. Perfil do escritório (`escritorio.json`)

- Identidade: nome, razão social, CNPJ, endereço, telefone, e-mail, rede social, logo
- Responsável técnico padrão: nome, CAU; executor/engenheiro padrão: nome, CREA
- Prefixo de nomenclatura (ex.: `VZ-`)
- Caminhos: pasta-base, template, projeto de referência, biblioteca de detalhes
- Carimbos: família por caderno (proposta, ARQ, DET) e formato de numeração (`n/N` ou `0n/0N`)
- Configuração de exportação PDF
- Texto-padrão das observações gerais do DET
- Fonte e tipos de texto padrão

## 5. Questionário de abertura

Feito uma vez, no início. Cada resposta tem destino definido:

| Bloco | Campos | Destino |
|---|---|---|
| Escritório | confirma perfil (ou cria) | `escritorio.json` |
| Cliente | nome/razão social, CPF/CNPJ, contato | Informações do projeto: Nome do cliente, Proprietário, CPF/CNPJ |
| Obra | rua, lote, quadra, loteamento/bairro, cidade/UF, matrícula, inscrição imobiliária | Endereço do projeto |
| Terreno | área do lote, zona urbanística, limites de TO / TP / CA da zona | Área do lote + `projeto.json` (para conferência) |
| Projeto | tipologia, obra nova ou reforma, nº de pavimentos, pé-direito por pavimento, cota da calçada, etapa contratada | níveis, fases, `projeto.json` |
| Responsáveis | autor do projeto + CAU, executor + CREA, desenhista, RRT de projeto, ART de execução | Informações do projeto |
| Módulos opcionais | caderno de pinturas, hidrossanitário, iluminação externa, setorização, conjunto de reforma | define quais vistas/folhas manter |

## 6. Fluxo de fases

Cada fase: o Claude executa a parte automática → apresenta o resultado → **ponto de aprovação** do arquiteto → grava o estado no modelo.

| Fase | Automático (Claude via MCP) | Aprovação humana |
|---|---|---|
| **0. Pré-voo** | testa conexão com o Revit; confere pasta-base conectada; garante que o arquivo aberto **não** é original de projeto em andamento | — |
| **1. Abertura** | cria o arquivo a partir do template; questionário; grava parâmetros; ajusta níveis (pé-direito, calçada); configura fases | confere dados do carimbo |
| **2. Proposta de layout** | 1 folha por pavimento (a 1ª com implantação de layout); aplica modelos de vista; posiciona vistas | aprova layout |
| **3. Conferência do modelo** | o arquiteto modela; o Claude audita paredes, pisos, forros, esquadrias, ambientes, avisos | corrige pendências |
| **4. Ambientes e acabamentos** | nomes pelo catálogo padrão; acabamentos de piso/parede/forro/rodapé; códigos e quadro de acabamentos | confere acabamentos |
| **5. Vistas** | gera tudo que o template não traz (seção 7) e aplica modelos de vista; organiza o navegador pelos parâmetros de organização | confere enquadramentos |
| **6. Anotação** | etiquetas, cotas, identificadores; busca de elementos sem cota/sem etiqueta | revisão gráfica |
| **7. Tabelas** | portas, janelas, soleiras, pingadeiras, bancadas, nichos, luminotécnico, áreas | confere quantitativos |
| **8. Folhas ARQ + carimbo** | monta as 7 pranchas pelas receitas; calcula áreas e índices; preenche conteúdo do carimbo | aprova pranchas |
| **9. Caderno DET** | calcula nº de folhas (esquadrias, bancadas, telhado, pergolados, elementos especiais); gera vistas de detalhe e monta folhas | aprova detalhes |
| **10. Conferência final** | checklist gráfico + normativo + índices × limites da zona; avisos; vistas não posicionadas; limpeza | libera emissão |
| **11. Emissão** | revisão; exportação PDF pela configuração do escritório; registro da emissão | — |

## 7. Receitas de prancha

Detalhe completo em `skill/references/receitas-pranchas.md`. Resumo:

- **Proposta (n/N):** uma folha por pavimento; 1ª folha com implantação de layout.
- **ARQ (n/7):** 1 planta executivo + implantação + tabelas de esquadrias · 2 cobertura + situação · 3 cortes + perspectivas de corte · 4 fachadas + perspectivas · 5 iluminação/interruptores + tomadas · 6 forro + vistas de iluminação por ambiente · 7 paginação de piso + áreas molhadas.
  Edificação com mais de um pavimento: as pranchas de planta se repetem por pavimento (numeração recalculada).
- **DET (0n/0N, quantidade variável):** telhado · esquadrias · bancadas · pergolados · demais elementos.

**O template já traz:** folhas-esqueleto, vistas de planta por pavimento com modelo aplicado, legendas, tabelas, esquemas de área.
**A skill gera:** cortes, fachadas, perspectivas, vistas de iluminação e de paginação por ambiente, planta de situação, vistas de detalhe, posicionamento nas folhas e conteúdo do carimbo.

## 8. Carimbos e parâmetros

- **Regra:** cada caderno usa um único carimbo em todas as suas folhas.
  - Proposta → carimbo de proposta de layout
  - ARQ → carimbo **Executivo Arquitetônico** em todas as folhas (o mesmo da 1ª folha)
  - DET → carimbo **A3 Executivo Interiores/Detalhamento** em todas as folhas
  - O carimbo A1 "Detalhes Arquitetônico" não entra no fluxo (confirmar remoção do master)
- **Campos calculados do modelo:** área ocupada, área construída total e por pavimento, área da piscina, área permeável, taxa de ocupação, taxa de permeabilidade, índice de aproveitamento.
- **Conteúdo da prancha:** gerado a partir das vistas posicionadas, não digitado.
- **Parâmetros novos necessários:** RRT de projeto, ART de execução, área da piscina, índice de aproveitamento, conteúdo da prancha.
- **Correções:** ver seção 10.

## 9. Normas e legislação

A skill carrega **parâmetros extraídos**, citando norma e item — nunca o texto das normas.
Candidatas (versões a confirmar na construção): NBR 6492 (representação de projetos de arquitetura), NBR 9050 (acessibilidade), NBR 15575 (desempenho), NBR 16636 (elaboração de projetos), código de obras e plano diretor do município da obra (por perfil/projeto).

## 10. Correções do master (antes de virar template oficial)

1. Trazer do projeto os modelos de vista **ELEVAÇÃO ILUMINAÇÃO** e **MODELAGEM FACHADA**.
2. Aplicar o carimbo Executivo Arquitetônico nas folhas 2/7–7/7 do caderno ARQ (hoje estão com "Detalhes Arquitetônico").
3. Remover carimbo duplicado ("A3Executivo") e família "Detalhamento" solta.
4. Apagar os 404 ambientes não posicionados → substituir pelo catálogo padrão de nomes.
5. Limpar ~50 parâmetros de família baixada (BIMobject, Uniclass, Region… em Guarda-corpos).
6. Unificar parâmetros de ambiente (AMBIENTE, .AMBIENTE, AMBIENTE DA PORTA, Porta Ambiente, PORTAS E JANELAS_AMBIENTE, Ambiente) e "Local de aplicação" duplicado.
7. Corrigir parâmetros do carimbo: "ÁREA CONSRUÍDA", "ÁREA PERMEÁVL"; taxas como número/porcentagem, não m².
8. Eliminar o ajuste manual "Área do Lote (AJUSTAR*)" nas tabelas de área (ler do parâmetro do projeto).
9. Padronizar tipos de texto (hoje 5 fontes).
10. Corrigir nomes: "MONTAFEM 3D", "PLANTA  BAIXA LAYOUT" (espaço duplo), "Escritro", "Spor", "Pramétrico".
11. Renomear famílias sem prefixo e remover estilo de linha duplicado ("Direção de abertura - Portas").
12. Manter as folhas DET como esqueleto mínimo; a skill cria as demais.

## 11. Regras de segurança

- Trabalhar sempre em cópia; **nunca** no arquivo original de um projeto em andamento.
- Leitura antes de escrita; toda alteração em transação nomeada e reversível (desfazer no Revit).
- Nada é salvo sem confirmação do usuário.
- Antes de cada fase: testar a conexão com o Revit; se falhar, orientar (ligar o servidor do MCP, fechar caixas de diálogo, aguardar carregamento) em vez de seguir.

## 12. Lições do levantamento

- O servidor do MCP desliga quando o Revit trava ou reinicia → precisa ser religado por sessão.
- Caixas de diálogo abertas no Revit bloqueiam o MCP.
- Arquivos de ~240 MB: fechar um modelo antes de abrir outro.
- Unidade de rede mapeada só conecta pelo seletor de pasta do app desktop.
- Arquivos Revit no servidor são lentos para abrir/salvar e podem travar o PC → a skill trabalha em cópia local e só publica o resultado no servidor; testes nunca criam projeto a partir do template pela rede.
- O Revit pode estar instalado fora do caminho padrão (ex.: `D:\Arquivos e Programas\Autodesk`) → detecção por registro/processo, nunca por caminho fixo.

### Ambiente validado (13/11/2026)
- Pasta-base em unidade de rede mapeada (mesma letra em todos os computadores)
- Revit 2025 e 2026 · suplemento MCP: `mcp-servers-for-revit.addin` (2025 e 2026)
- Script de preparação validado: `SCRIPTS\PUXAR_PASTA_BASE.cmd` (v0.2) → grava `%APPDATA%\SkillExecutivoRevit\config.json` e registra a máquina em `_config\maquinas\` — entra no instalador `.exe`

## 13. Decisões em aberto

1. Documentos auxiliares do escritório (manuais, checklists, memoriais) — ainda não enviados.
2. Lista final de normas e legislação municipal por cidade.
3. Escopo dos módulos de interiores (o projeto de referência tem modelo de interiores vinculado).
4. Integração futura com sistemas de gestão (ex.: emissão de etapa como marco de cobrança).

## 14. Ordem de construção sugerida (próxima conversa)

1. Corrigir o template → **master v1** (seção 10), trabalhando numa cópia.
2. Escrever `SKILL.md` + referências de fases 0 e 1 (pré-voo e abertura) e testar num projeto vazio.
3. Receitas de prancha + scripts de geração de vistas e folhas (fases 5 e 8).
4. Cálculo de áreas/índices e carimbo.
5. Caderno DET (fase 9).
6. Checklists de conferência (fase 10) e emissão (fase 11).
7. Instalador `.exe` e publicação no GitHub.
8. Piloto com um projeto real e ajuste.
