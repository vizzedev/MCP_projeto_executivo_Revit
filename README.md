# MCP Projeto Executivo Revit

Skill do Claude que conduz um projeto de arquitetura no **Revit**, do arquivo zero à emissão — proposta de layout, executivo arquitetônico (ARQ) e detalhamento (DET) — de forma padronizada, usando o **MCP do Revit** (suplemento mcp-servers-for-revit).

Genérica: serve a qualquer escritório. Dados do escritório ficam no perfil (`skill/config/escritorio.json`), nunca no código.

> **Status:** versão 0.5 — todas as fases (0 a 11) com rotina, testadas numa cópia do projeto de referência e num piloto criado do zero (casa térrea 10 × 10 m, da abertura à emissão). Continuam manuais: folha de telhado do DET, cotas L/H das esquadrias, textos de especificação das bancadas, logotipo no carimbo. Pendências do template master e próximos passos em `skill/references/relatorio-piloto.md`. Instalador em `installer/` (gera o `.exe` com `COMPILAR_EXE.cmd`).

## Como funciona

1. Cada escritório mantém, num servidor, uma **pasta-base** com o template master (.rte), o projeto de referência e a biblioteca de detalhes.
2. Em cada computador, o **instalador** prepara o acesso à pasta-base e grava a configuração local.
3. A **skill** é enviada ao Claude (Customize › Skills › enviar ZIP da pasta `skill/`).
4. Numa conversa no app desktop do Claude, com o Revit aberto e o servidor do MCP ligado, a skill conduz o projeto fase a fase, com ponto de aprovação do arquiteto ao fim de cada uma.

## Estrutura

```
skill/                     → o que vira o ZIP enviado ao Claude
  SKILL.md                 → instruções principais (regras e fases)
  config/escritorio.json   → perfil do escritório (preencher)
  references/              → fases, questionário, parâmetros, receitas das pranchas
  scripts/                 → rotinas C# executadas pelo MCP (send_code_to_revit)
installer/                 → preparação de cada computador (PUXAR_PASTA_BASE.cmd)
pasta-base-modelo/         → estrutura e arquivos de configuração da pasta-base
docs/ESPECIFICACAO.md      → especificação completa da skill
```

## Requisitos

- Autodesk Revit 2025 (o template master é 2025)
- Suplemento MCP do Revit (o instalador instala, se faltar) e o servidor ligado no Revit (Suplementos › Revit MCP Switch)
- App desktop do Claude, com a pasta-base conectada na conversa ("Adicionar pasta")
- Pasta-base acessível pelo mesmo caminho em todos os computadores

## Instalação (por computador)

1. Execute `InstaladorSkillExecutivoRevit.exe` (ou `installer/INSTALAR.cmd`). Na primeira vez ele pede o repositório
   (`usuario/repositorio`) e a pasta-base. Ele:
   - baixa a versão mais recente do GitHub;
   - instala o **MCP do Revit** se faltar (suplemento `mcp-servers-for-revit` no Revit 2025, Node.js e a configuração do
     Claude Desktop), rodando o instalador oficial do projeto do MCP (`-McpRepo`, padrão `LuDattilo/revit-mcp-server`);
     `-AtualizarMcp` reinstala, `-SemMcp` pula;
   - confere unidade de rede, pasta-base, template, Revit, suplemento do MCP e se o servidor do MCP responde;
   - pede o perfil do escritório (ou usa `<pasta-base>\_config\escritorio.json`, se já existir);
   - gera na Área de Trabalho o ZIP da skill com o perfil preenchido e abre a pasta;
   - registra a versão instalada (rodar de novo atualiza).
2. Envie o ZIP em **Claude › Personalizar › Skills** (o envio é manual em cada conta).
3. Para gerar o `.exe`: rode `installer/COMPILAR_EXE.cmd` numa máquina Windows (embute a skill com `montar-pacote.ps1`
   e compila com o módulo ps2exe). Para o .exe avisar de versões novas, crie `installer/repo.txt` com `usuario/repositorio` antes de compilar. O `.exe` funciona sem internet para a skill; usa a internet só para o MCP e avisos de versão.
   `PUXAR_PASTA_BASE.cmd` continua disponível só para conferir a pasta-base.

## Scripts

| Script | Fase | Função |
|---|---|---|
| `00_preflight.cs` | 0 | Confere conexão, arquivos abertos, rede, configuração e template (somente leitura) |
| `01_preparar_template_local.cs` | 1 | Copia o template master para o disco local, só se estiver desatualizado |
| `01_criar_projeto.cs` | 1 | Cria e salva o projeto a partir da cópia local (nunca sobrescreve) |
| `01_preencher_dados.cs` | 1 | Grava Informações do projeto (carimbo) e cotas dos níveis |
| `01_ajustar_pavimentos.cs` | 1 | Uma folha de proposta de layout por pavimento |
| `02_proposta.cs` | 2 | Folhas da proposta de layout, uma por pavimento, com a implantação na primeira |
| `02_desenhar_lote.cs` | 2/5 | Lote, projeção da edificação, cotas e recorte nas implantações quando não há linhas de propriedade |
| `03_conferir_modelo.cs` | 3 | Conferência do modelo (somente leitura): BLOQUEIA / CORRIGIR / ATENÇÃO |
| `03_corrigir_modelo.cs` | 3 | Correções automáticas aprovadas: ambientes não colocados, paredes curtas, renumerar esquadrias |
| `04_ambientes.cs` | 4 | Campo AMBIENTE das portas e janelas (propor / aplicar só vazios / aplicar tudo) |
| `05_plantas.cs` | 5 | Garante as plantas do caderno (executivo, tomadas, iluminação, forro, paginação, cobertura, implantação) e avisa as vazias |
| `05_situacao.cs` | 5 | Planta de situação vetorial: lote hachurado, vizinhos, ruas, distância à esquina e norte |
| `05_situacao_importar.cs` | 5 | Planta de situação a partir da imagem/PDF do loteamento |
| `05_propor_cortes.cs` | 5 | Propõe 4 cortes (2 transversais, 2 longitudinais) para aprovação do arquiteto |
| `05_criar_fachadas.cs` | 5 | 4 fachadas com frente detectada pela calçada; oculta o muro de divisa na frente de cada uma |
| `05_vistas_ambientes.cs` | 5 | Vistas de iluminação (1 por ambiente) e conjuntos de paginação (planta, 4 paredes, 2 isométricas) |
| `05_perspectivas.cs` | 5 | Perspectivas dos cortes e das laterais |
| `05_montar_folhas.cs` | 5 | Folhas 3/7 (cortes) e 4/7 (fachadas) |
| `05_montar_folhas_ambientes.cs` | 5 | Folhas 6/7 (forro e iluminação) e 7/7 (paginação) |
| `05_montar_folhas_gerais.cs` | 5 | Folhas 1/7 (planta executiva, implantação, tabelas de esquadrias), 2/7 (cobertura e situação) e 5/7 (iluminação, tomadas, tabela luminotécnica) |
| `06_anotar_planta.cs` | 6 | Identificadores de portas, janelas e ambientes, cotas de nível e cotas externas (3 linhas por face) da planta executiva |
| `06_cotar_pranchas.cs` | 6 | Cotas por prancha: layout, tomadas, iluminação, cobertura e cortes |
| `06_textos.cs` | 6 | Textos das pranchas a partir do modelo: chamadas de materiais, revestimentos, forro, tomadas, bancadas |
| `00_limpar_template.cs` | — | Relatório e limpeza das sobras do exemplo no template master (só numa cópia) |
| `09_montagens.cs` | 9 | Montagens (assemblies) de esquadrias, bancadas e elementos especiais, com elevação, topo e 3D |
| `09_montar_folhas_det.cs` | 9 | Folhas A3 do caderno DET a partir das montagens, com as tabelas de portas e janelas |
| `10_conferencia_final.cs` | 10 | Numeração i/N, folhas vazias, campos do carimbo, tabelas vazias, vistas fora da área ou sobrepostas |
| `11_emitir.cs` | 11 | PDF combinado por caderno (opcional: PDF por folha e DWG) e registro EMISSAO.txt |
| `08_regioes_areas.cs` | 8 | Regiões de área construída, permeável e piscina a partir dos pisos do modelo |
| `calcular_areas_indices.cs` | 8 | Áreas, taxas de ocupação/permeabilidade e índice de aproveitamento a partir das regiões preenchidas da implantação |

## Regras de segurança da skill

- Nunca trabalha no template master nem em projeto de outra pessoa; projetos nascem de uma cópia local do master.
- Não abre, cria nem salva arquivos Revit grandes pela rede (trava o Revit).
- Nada é salvo sem confirmação; toda alteração fica em transação nomeada "Skill: …" (desfazível).

## Parâmetros compartilhados

`pasta-base-modelo/_config/PARAMETROS_COMPARTILHADOS_SKILL.txt` — 22 parâmetros com GUIDs fixos (carimbo, áreas e AMBIENTE). Lista em `skill/references/parametros.md`.
