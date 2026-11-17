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
- **Dados de fora do modelo** (rua, largura, asfalto, lotes vizinhos, numeração, recuos, CPF, ART/RRT, tipo de construção,
  link do QR code) vêm do questionário de abertura (`references/questionario-abertura.md`).

## Vários pavimentos (frequente)
Toda rotina vale para casas de um ou mais pavimentos ("PAV. X N.A" com ambientes):
- Plantas por pavimento (executivo, layout, tomadas, iluminação, forro, paginação) — `05_plantas.cs` já cria uma de cada.
- Cotas externas em todos os pavimentos, pela regra do escritório: 3 linhas por fora dos muros do lote, de divisa a divisa.
  O lote vem do térreo; no superior, as divisas precisam aparecer na vista (linhas de propriedade ou subposição do térreo).
- Cortes: cadeia vertical por ambiente, até a laje acima dele; à esquerda, piso acabado de cada pavimento + topo.
- Pranchas: em geral **um pavimento por folha**; se couber, dois na mesma folha. Decida com o modo `"testar"` de
  `05_montar_folhas_gerais.cs` (ver `references/gabarito-folhas.md`) e numere `i/N` pela quantidade final de folhas.
- Áreas: construída por pavimento + total (`calcular_areas_indices.cs`).
- **Escada não é detalhada** pela skill (aparece nas vistas como modelada, sem cotas nem detalhe próprio).

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
9. **Nunca desfaça montagens (assemblies) por script** — o Revit pede confirmação numa janela modal ("a exclusão desta montagem removerá todas as vistas associadas"). Apagar uma vista de montagem é seguro.

## Fases
| # | Fase | Referência | Status |
|---|---|---|---|
| 0 | Pré-voo | `references/fases/00-pre-voo.md` | pronta |
| 1 | Recebimento do projeto modelado (dados do carimbo, estado da skill) | `references/fases/01-abertura.md` | pronta |
| 2 | Planta de layout: anotação e folha de proposta (+ `02_desenhar_lote.cs` quando não há linhas de propriedade) | `references/fases/02-proposta.md` | pronta — testada na referência e no piloto |
| 3 | Conferência do modelo + regras de projeto do checklist (corrige o que o arquiteto aprovar) | `references/fases/03-conferencia.md` | pronta — testada no projeto de referência |
| 4 | Ambientes de portas e janelas, nomes de ambientes | `references/fases/04-ambientes.md` | pronta — testada no projeto de referência |
| 5 | Vistas e folhas do caderno ARQ (1/N a N/N; 7 folhas numa casa térrea) | `references/fases/05-vistas.md` | pronta — testada no projeto de referência |
| 6 | Anotação: identificadores, níveis, cotas em 3 escalas, paisagismo oculto (`06_anotar_planta.cs`); cotas por prancha e eixos hidrossanitários (`06_cotar_pranchas.cs`); textos, paginação, ar e hidro (`06_textos.cs`); implantação e recuos (`06_implantacao.cs`); elementos externos em proposta (`06_elementos_externos.cs`) | `references/fases/06-anotacao.md` | pronta — testada no projeto de referência |
| 7 | Tabelas (vêm do template; dados das fases 3 e 4) | `references/fases/07-tabelas.md` | pronta |
| 8 | Carimbo: dados, áreas e índices (`08_regioes_areas.cs` + `calcular_areas_indices.cs`), QR code (`08_qrcode.cs`) | `references/fases/08-folhas-carimbo.md` | pronta — validada na referência e no piloto |
| 9 | Caderno DET (montagens + folhas A3) | `references/fases/09-caderno-det.md` | pronta — testada no projeto de referência (telhado manual) |
| 10 | Conferência final | `references/fases/10-conferencia-final.md` | pronta — testada no projeto de referência |
| 11 | Emissão | `references/fases/11-emissao.md` | pronta — testada no projeto de referência |

## Como começar uma conversa
1. Leia `config/escritorio.json`. Se não existir ou estiver com marcadores `<<…>>`, faça primeiro o cadastro do escritório (ver `references/perfil-escritorio.md`).
2. Rode a fase 0.
3. Pergunte qual é o **projeto modelado** a detalhar (caminho do .rvt) e se é o **início do detalhamento** (fase 1) ou uma
   **retomada** (leia `<projeto>.skill.json` e continue da fase registrada). Pergunte também quantos pavimentos a casa tem.
4. Ao fim de cada fase, grave o estado em `<projeto>.skill.json` e diga ao usuário qual é a próxima fase.

## Referências
- `references/questionario-abertura.md` — perguntas da abertura e onde cada resposta é gravada
- `references/perfil-escritorio.md` — campos do perfil do escritório
- `references/parametros.md` — parâmetros compartilhados da skill (GUIDs fixos)
- `references/receitas-pranchas.md` — conteúdo de cada prancha (do projeto de referência)
- `references/checklist-executivo.md` — checklist executivo do escritório × o que a skill faz, confere ou deixa com a equipe
- `references/relatorio-piloto.md` — piloto num projeto novo: o que foi corrigido e pendências do template master
- `scripts/` — rotinas C# para o MCP do Revit

**Manutenção do template:** `scripts/00_limpar_template.cs` (relatório; limpeza só numa cópia do master — ver `references/relatorio-piloto.md`).
