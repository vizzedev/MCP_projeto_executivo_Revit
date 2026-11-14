# MCP Projeto Executivo Revit

Skill do Claude que conduz um projeto de arquitetura no **Revit**, do arquivo zero à emissão — proposta de layout, executivo arquitetônico (ARQ) e detalhamento (DET) — de forma padronizada, usando o **MCP do Revit** (suplemento mcp-servers-for-revit).

Genérica: serve a qualquer escritório. Dados do escritório ficam no perfil (`skill/config/escritorio.json`), nunca no código.

> **Status:** em construção — versão 0.1. Prontas: fases 0 (pré-voo) e 1 (abertura do projeto) e a rotina de áreas e índices do carimbo. Demais fases em desenvolvimento.

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
- Suplemento MCP do Revit instalado e com o servidor ligado
- App desktop do Claude, com a pasta-base conectada na conversa ("Adicionar pasta")
- Pasta-base acessível pelo mesmo caminho em todos os computadores

## Instalação (por computador)

1. Copie a pasta `installer/` para a pasta-base ou para o computador.
2. Execute `PUXAR_PASTA_BASE.cmd`. Ele confere a unidade de rede, a pasta-base, o template, o Revit e o MCP, e grava `%APPDATA%\SkillExecutivoRevit\config.json`.
3. Preencha `skill/config/escritorio.json`, compacte a pasta `skill/` em ZIP e envie em **Claude › Customize › Skills**.

## Scripts

| Script | Fase | Função |
|---|---|---|
| `00_preflight.cs` | 0 | Confere conexão, arquivos abertos, rede, configuração e template (somente leitura) |
| `01_preparar_template_local.cs` | 1 | Copia o template master para o disco local, só se estiver desatualizado |
| `01_criar_projeto.cs` | 1 | Cria e salva o projeto a partir da cópia local (nunca sobrescreve) |
| `01_preencher_dados.cs` | 1 | Grava Informações do projeto (carimbo) e cotas dos níveis |
| `01_ajustar_pavimentos.cs` | 1 | Uma folha de proposta de layout por pavimento |
| `calcular_areas_indices.cs` | 8 | Áreas, taxas de ocupação/permeabilidade e índice de aproveitamento a partir das regiões preenchidas da implantação |

## Regras de segurança da skill

- Nunca trabalha no template master nem em projeto de outra pessoa; projetos nascem de uma cópia local do master.
- Não abre, cria nem salva arquivos Revit grandes pela rede (trava o Revit).
- Nada é salvo sem confirmação; toda alteração fica em transação nomeada "Skill: …" (desfazível).

## Parâmetros compartilhados

`pasta-base-modelo/_config/PARAMETROS_COMPARTILHADOS_SKILL.txt` — 22 parâmetros com GUIDs fixos (carimbo, áreas e AMBIENTE). Lista em `skill/references/parametros.md`.
