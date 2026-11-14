# Fase 0 — Pré-voo

Objetivo: garantir que o ambiente está pronto antes de qualquer alteração. Não altera nada.

## Passos
1. **Rodar `scripts/00_preflight.cs`** (send_code_to_revit, transactionMode "none", sem parâmetros).
   - Se a chamada falhar com "Connect to Revit client failed" ou não responder:
     - peça para ligar o servidor do MCP (aba de suplementos do Revit);
     - peça para fechar qualquer caixa de diálogo aberta no Revit (avisos, vínculos não encontrados);
     - se o Revit estiver carregando um arquivo grande, aguarde e tente de novo uma vez.
2. **Ler o relatório** e conferir:
   - versão do Revit = versão do template (hoje 2025);
   - lista de arquivos abertos: identifique pelo caminho qual é o projeto de trabalho; avise se houver arquivo com alterações não salvas que não seja o projeto;
   - `config.json` local (`%APPDATA%\SkillExecutivoRevit\config.json`) encontrado — criado pelo instalador/`PUXAR_PASTA_BASE.cmd`;
   - pasta-base e template master acessíveis;
   - cópia local do template: existe e está atualizada? Se não, ela será preparada na fase 1.
3. **Pasta no Claude:** confirme que a pasta-base está conectada na conversa (o usuário adiciona pelo botão "Adicionar pasta"; unidades de rede não podem ser pedidas pelo Claude).
4. **Relatar** ao usuário em poucas linhas: o que está ok e o que precisa ser resolvido.

## Ponto de aprovação
Nenhum — mas não avance se houver item bloqueante (sem conexão, template inacessível, versão do Revit diferente).
