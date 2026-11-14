---
name: executivo-revit
description: Conduz um projeto de arquitetura no Revit, do arquivo zero à emissão (proposta de layout + executivo ARQ + detalhamento DET), de forma padronizada, usando o MCP do Revit. Use quando o usuário quiser iniciar, retomar ou avançar um projeto de arquitetura no Revit com o padrão do escritório.
---

# Projeto executivo de arquitetura no Revit

Você conduz o projeto em **fases**. Em cada fase: executa a parte automática pelo MCP do Revit, mostra o resultado e **para no ponto de aprovação** do arquiteto antes de seguir. Nunca pule a aprovação.

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

## Fases
| # | Fase | Referência | Status |
|---|---|---|---|
| 0 | Pré-voo | `references/fases/00-pre-voo.md` | pronta |
| 1 | Abertura do projeto | `references/fases/01-abertura.md` | pronta |
| 2 | Proposta de layout | — | a construir |
| 3 | Conferência do modelo | — | a construir |
| 4 | Ambientes e acabamentos | — | a construir |
| 5 | Vistas e folhas do caderno ARQ (1/7 a 7/7) | `references/fases/05-vistas.md` | pronta — testada no projeto de referência |
| 6 | Anotação | — | a construir |
| 7 | Tabelas | — | a construir |
| 8 | Folhas ARQ + carimbo (áreas: `scripts/calcular_areas_indices.cs`) | — | rotina de áreas pronta |
| 9 | Caderno DET | — | a construir |
| 10 | Conferência final | — | a construir |
| 11 | Emissão | — | a construir |

## Como começar uma conversa
1. Leia `config/escritorio.json`. Se não existir ou estiver com marcadores `<<…>>`, faça primeiro o cadastro do escritório (ver `references/perfil-escritorio.md`).
2. Rode a fase 0.
3. Pergunte se é **projeto novo** (fase 1) ou **retomada** (leia `<projeto>.skill.json` e continue da fase registrada).
4. Ao fim de cada fase, grave o estado em `<projeto>.skill.json` e diga ao usuário qual é a próxima fase.

## Referências
- `references/questionario-abertura.md` — perguntas da abertura e onde cada resposta é gravada
- `references/perfil-escritorio.md` — campos do perfil do escritório
- `references/parametros.md` — parâmetros compartilhados da skill (GUIDs fixos)
- `references/receitas-pranchas.md` — conteúdo de cada prancha (do projeto de referência)
- `scripts/` — rotinas C# para o MCP do Revit
