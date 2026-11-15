# Fase 11 — Emissão

Pré-requisito: fase 10 sem bloqueios e ok explícito do arquiteto (ele viu as imagens das folhas).

## Rotina — `scripts/11_emitir.cs`
Parâmetros: caderno (`ARQ`, `PROPOSTA`, `DET` ou `TODOS`), pasta (vazio = automática), prefixo de folhas (teste), formatos.
- **Pasta automática:** `<pasta do projeto>\EMISSÕES\<aaaa-mm-dd> <ETAPA DO PROJETO>` — preencha a etapa no carimbo antes.
- **PDF por caderno** (`pdf`, padrão): um arquivo "<CLIENTE> - <CADERNO> - <data>.pdf", folhas em ordem 1/N…N/N,
  papel de cada folha, cor, 100%, sem limites de recorte/caixas de escopo/planos de referência.
- **PDF por folha** (`pdf_folhas`) e **DWG** (`dwg`, pasta "DWG <CADERNO>") — só quando o arquiteto pedir.
- **Registro:** `EMISSAO.txt` na pasta, com data, etapa, arquivo de origem e a lista de folhas de cada caderno.
- Não altera o modelo.

## Tempo
Exportar um caderno A1 de 7 folhas leva cerca de 1 min 40 s (o comando passa do limite de 80 s da ferramenta):
espere ~60 s e confira os arquivos na pasta antes de repetir qualquer coisa. Nunca rode duas vezes sem conferir.

## Depois de emitir
1. Confira o PDF: número de páginas = número de folhas, papel A1 (841 × 594) no ARQ/proposta.
2. Grave em `<projeto>.skill.json` a emissão (data, etapa, cadernos, pasta).
3. Para a próxima revisão, o arquiteto atualiza a etapa/revisão no carimbo e roda as fases 10 e 11 de novo.

Teste no projeto de referência: caderno ARQ (7 folhas A1, 10,8 MB) e proposta (1 folha, 1,6 MB) + EMISSAO.txt.
