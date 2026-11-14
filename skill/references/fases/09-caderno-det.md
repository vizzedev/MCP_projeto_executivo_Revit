# Fase 9 — Caderno DET (detalhamento)

Scripts: `scripts/09_montagens.cs` e `scripts/09_montar_folhas_det.cs`

O caderno DET do projeto de referência é feito de **montagens (assemblies)** do Revit com as
vistas automáticas de montagem ("Elevação de topo", "Elevação frontal/posterior/esquerda/direita",
"Ortogonal 3D"), em folhas A3 com o carimbo "Executivo Interiores/Detalhamento":

| Folha | Conteúdo | Origem |
|-------|----------|--------|
| Telhado | planta de madeiramento + 3D + vigas | **manual** (desenho do arquiteto) |
| Esquadrias (portas) | elevação + topo de cada tipo, 1:50, tabela de portas no rodapé | automático |
| Esquadrias (janelas) | idem, tabela de janelas | automático |
| Bancadas | 3D + elevação + topo, 1:25, título com ambiente | automático (cotas e textos de especificação: manual) |
| Pergolados / churrasqueira | 3D + frontal + topo, 1:50 | automático |

## Passo 1 — montagens

`09_montagens.cs`, parâmetros:

| # | Conteúdo |
|---|----------|
| 0 | `propor` (só lista) · `criar` · `refazer_elevacoes` |
| 1 | grupos: `portas;janelas;bancadas;genericos` (vazio = todos) |
| 2 | filtro de marcas/nomes (`P03;J06`); obrigatório em `refazer_elevacoes` |
| 3 | palavras-chave dos modelos genéricos (vazio = `PERGOL;CHURRASQ`) |

- Portas/janelas: **uma montagem por marca de tipo** (usa a primeira instância), nome = marca.
  Tipos sem marca são avisados (corrigir na fase 3).
- Bancadas: componentes de gabinete; marca de tipo ou, sem marca, B01, B02… livres. As peças
  hidrossanitárias apoiadas na bancada entram na montagem.
- Elevação escolhida = a vista em que **a geometria da peça é mais larga** (medida nos sólidos),
  e entre frente e fundo a do lado da `FacingOrientation`. Só a FacingOrientation não basta:
  no teste, 2 de 6 famílias tinham eixos girados e saíam de lado.
- Identificador da porta/janela é colocado sob a elevação.
- Idempotente: pula o que já está em montagem.

Rode primeiro `propor`, mostre a lista ao usuário, depois `criar`.

## Passo 2 — folhas

`09_montar_folhas_det.cs`, parâmetros: `[0]` grupos (`esquadrias;bancadas;genericos`),
`[1]` prefixo do número (teste `SKILL DET `), `[2]` filtro de montagens, `[3]` tipo de carimbo A3.

- Só usa montagens cujas vistas ainda não estão em folha (pode rodar de novo depois de criar mais).
- Liga o recorte das vistas ajustado à peça (+0,15 m) e o esconde — sem isso a vista leva
  linhas de referência e ocupa 120 mm na folha.
- Gira 90° a vista de topo quando a peça corre na vertical dela (montagem alinhada ao mundo).
- Janela de vista tipo "SEM TITULO" (procurada entre as janelas já usadas no projeto).
- Área útil A3: carimbo +18 mm à esquerda, −57 mm à direita (coluna do carimbo), +13 mm embaixo,
  −9 mm em cima.
- Empacota em linhas; quando não cabe, abre nova folha. Bloco mais largo que a folha dobra a escala.
- Numeração `i/N` com dois dígitos; se já havia folhas DET, avisa para atualizar o "/N" delas.

## Cuidados

- **Nunca desfaça montagens por script**: o Revit abre um diálogo modal ("a exclusão desta montagem
  removerá todas as vistas associadas") e o MCP trava até alguém clicar. Para corrigir a elevação,
  use `refazer_elevacoes`.
- Folha de telhado continua manual; avise o usuário.
- Cotas L/H das esquadrias e textos de especificação das bancadas (cuba, torneira, pedra) são
  manuais — liste para o usuário o que ficou faltando.

## Teste no projeto de referência

Montagens novas P03, P05, P100, J06, J07, J08, B07 → 3 folhas (portas + tabela, janelas + tabela,
bancada), layout equivalente às folhas 02/07 e 03/07 do arquiteto.
