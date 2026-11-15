# Fase 1 — Abertura do projeto

Objetivo: criar o arquivo do projeto a partir do template master, preencher os dados do carimbo e ajustar níveis e pavimentos.

## Passos
1. **Questionário** — faça as perguntas de `references/questionario-abertura.md`, em blocos (no máximo 4 perguntas por vez). Dados do escritório e responsáveis padrão vêm do perfil; só confirme. Mostre um resumo final e peça confirmação antes de criar o arquivo.
2. **Template local** — rode `scripts/01_preparar_template_local.cs` com `[caminho do master no servidor]`. Ele copia o master para `%LOCALAPPDATA%\SkillExecutivoRevit\templates\` só se a cópia local não existir ou for mais antiga. A cópia pode levar 1–3 min: se a chamada passar de 80 s, aguarde e rode de novo — o script é seguro para repetir (não copia se já estiver atualizado).
3. **Criar o projeto** — rode `scripts/01_criar_projeto.cs` com `[template local, caminho do .rvt de destino]`. Destino: a pasta de trabalho indicada pelo usuário (padrão do perfil). O script cria, salva e abre o projeto. Se o destino já existir, o script para — nunca sobrescreve.
4. **Preencher dados** — rode `scripts/01_preencher_dados.cs` com uma linha `Nome do parâmetro=valor` por item (ver questionário). Áreas em m², níveis com `NIVEL:<nome>=<cota em m>`. O script relata o que gravou e o que não encontrou.
5. **Ajustar pavimentos** — rode `scripts/01_ajustar_pavimentos.cs` com `[número de pavimentos]`. Remove as folhas de proposta excedentes (uma folha de layout por pavimento).
6. **Estado** — grave `<projeto>.skill.json` ao lado do .rvt com as respostas do questionário, módulos escolhidos e `"fase_atual": 1`.
7. **Salvar** — pergunte e, com confirmação, salve o projeto.

## Ponto de aprovação
Mostre a folha 1/7 (carimbo) e as Informações do projeto. O arquiteto confere nome, endereço, responsáveis, RRT/ART e níveis.

## Erros comuns
- Parâmetro "não encontrado": o template usado não é o master da skill → pare e avise.
- Nível não encontrado: o nome no questionário difere do template → liste os níveis existentes e pergunte.
