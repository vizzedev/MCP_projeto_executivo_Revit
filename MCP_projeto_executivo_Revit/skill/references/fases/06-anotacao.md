# Fase 6 — Anotação das plantas

Script: `scripts/06_anotar_planta.cs`

Coloca, numa planta já criada na fase 5, as anotações que o arquiteto faz à mão:
identificadores de portas, janelas e ambientes, cotas de nível de piso e as cotas
externas em cadeia. Roda em uma transação nomeada ("Skill: anotar planta"), não salva o arquivo.

## Parâmetros

| # | Conteúdo | Exemplo |
|---|----------|---------|
| 0 | Nome da vista de planta | `PAVIMENTO TERREO EXECUTIVO` |
| 1 | Opções (vazio = todas) | `portas;janelas;ambientes;niveis;cotas_externas` |

Outras opções: `refazer_cotas` (apaga as cotas lineares fora do lote e cria de novo) e
`apagar_cotas_internas` (apaga as cotas lineares com origem dentro do lote — usada para limpar
versões antigas da skill, que cotavam por dentro dos ambientes).

## O que faz

- **Portas e janelas**: identificador ("Identificador da porta" / "Identificador de Janela")
  0,45 m para o lado interno do vão. Só os elementos visíveis na vista.
- **Ambientes**: "Identificador de Ambiente" no ponto de locação do ambiente
  (apenas ambientes do nível da vista, com área).
- **Níveis**: cota de nível tipo "VZ-Nível Planta" na face superior do piso,
  0,9 m abaixo do identificador do ambiente.
- **Cotas (regra do escritório)**: nenhuma cota dentro do lote. Todas ficam por fora dos
  muros/divisas, nas quatro faces (frente, fundo, esquerda, direita), em duas linhas:
  1. **Cadeia integrada**, de divisa a divisa, sem trecho sem cotar: limites de todas as
     paredes (faces e vãos de portas/janelas) e de todos os pisos do pavimento, projetados
     naquele eixo. Referências a menos de 1 cm umas das outras viram uma só.
  2. **Total do lote** naquela face.
  Linhas a 12 mm e 20 mm (no papel) além da divisa; tipo de cota linear terminado em
  "1-<escala>" (ex.: "1-50"), senão o primeiro "VZ". O lote é lido como no
  `02_desenhar_lote.cs`: linhas de propriedade > muros de divisa (paredes até 3 m) >
  retângulo das paredes; a calçada fica de fora. Se o recorte da vista não alcança as cotas,
  ele é ampliado. Referências que o Revit não aceita na vista são descartadas uma a uma.
  Trechos abaixo de 20 cm são contados no relatório (o texto pode precisar ser afastado à mão).
  Se a cadeia não chega até a divisa (lado sem muro e sem linha de propriedade), o relatório avisa.

## Idempotência

Pode ser rodado de novo: elementos que já têm identificador na vista são pulados; cotas de
nível a menos de 1,5 m de uma existente são puladas. Se a vista já tem cotas lineares fora do lote,
elas não são recriadas (use `refazer_cotas`).

## Teste no projeto de referência (térreo, escala 1:75)

| Item | Arquiteto | Skill |
|------|-----------|-------|
| Portas | 23 | 23 |
| Janelas | 10 | 10 |
| Ambientes | 21 | 29 (a skill também identifica áreas externas) |
| Cotas de nível | 27 | 28 |
| Cotas internas | 53 | 26 (versão antiga; substituídas pelas cotas externas em cadeia) |

## Depois de rodar

1. Exporte a vista como imagem e mostre ao usuário.
2. Avise que identificadores podem sobrepor mobiliário ou cotas em ambientes pequenos (WC):
   o ajuste fino de posição é manual.
3. Confira na imagem se a cadeia de cada face chegou de divisa a divisa e se há textos
   sobrepostos em trechos curtos.

## Cotas das demais pranchas — `scripts/06_cotar_pranchas.cs`

As cotas seguem o conteúdo de cada prancha. Parâmetros: `[0]` vistas separadas por ";",
`[1]` modo (`auto` pelo nome da vista/modelo de vista), `[2]` `refazer`, `[3]` tipo de cota (opcional).

| Prancha | Modo | O que cota |
|---------|------|------------|
| Layout / setorização | `layout` | largura e comprimento de cada ambiente (faces de acabamento), tipo só com texto ("VZ-Cota Interna" ou um tipo com "LAYOUT" no nome) |
| Pontos de tomada | `tomadas` | por trecho de parede com tomadas/interruptores: limite da parede → peça → peça → limite da parede |
| Iluminação | `iluminacao` | por ambiente, eixo e fileira: parede → luminária → luminária → parede, com a linha passando pela fileira; luminária linear (> 0,60 m) pelas pontas |
| Cobertura | `cobertura` | por fora, nas 4 faces: beiral × corpo (faces externas da edificação) + total |
| Cortes | `corte` | só na vertical: uma cadeia por ambiente cortado (piso acabado → forro → laje), encostada na parede esquerda do ambiente, + altura total à esquerda |

Tipo padrão (exceto layout): "VZ-Cota 1-<n>" com a escala mais próxima da vista. As peças usam o
plano central da família (Centro esq/dir ou frente/trás, conforme a rotação); peça sem esse
plano ou fora de ambiente é avisada e não cotada. Vista que já tem cota linear é pulada
(`refazer` apaga as cotas lineares da vista e cria de novo).

### Piloto (casa 10×10)
- Executivo 1:75: 4 cadeias + 4 totais, 10,00 × 10,00, fora da calçada; ombreiras de vão ficam fora
  (regra: limites de paredes e pisos); trechos curtos com texto escalonado.
- Tomadas: 10 cadeias; iluminação: 8 (2 por ambiente); cobertura: 4; cortes A–D: cadeia + total.
- Pendências: no corte, as camadas do piso geram trechos de 2–5 cm no pé da cadeia; o tipo
  "VZ-Cota Interna" (ordenada) põe o texto na ponta — confirmar qual é o tipo de layout do escritório.

### Padrão do projeto de referência (análise das pranchas ARQ)
- Executivo: 3 linhas por face, por fora do lote — **regra atual (checklist executivo): três escalas**:
  micro (vãos de portas e janelas da fachada + extremos da edificação) a 5 mm do desenho, médio (ambiente por ambiente:
  paredes e pisos, sem ombreiras) e macro (total do terreno), a cada 6,5 mm. A linha de blocos (divisas, muros) da
  referência foi substituída pela micro. Sem vão externo na face, a micro não é criada.
  Dimensões dos ambientes só em texto no canto ("VZ-Cota Interna"). Regra do escritório: nenhuma
  cadeia atravessando os ambientes (a referência ainda tinha — foi descartado).
- Cortes: cadeia vertical por ambiente (2,70 | 0,50 | 0,62 na referência) + altura total.
- Iluminação: linha pela fileira; perfis/trilhos pelas pontas. Tomadas: parede → ponto → ponto → parede.
- Cobertura: corpo × beiral (17,95 | 0,97 na referência).
- Pendências: cotas da 1ª linha podem cruzar marcadores de corte/fachada (afastar à mão);
  pontos externos (balizadores, espetos) ainda não são cotados; largura de cada água do telhado.

## Textos das pranchas — `scripts/06_textos.cs`

Levantamento das anotações do projeto de referência (14 pranchas ARQ + DET) e o que a skill gera a partir do modelo:

| Prancha | Na referência | Gerado (fonte no modelo) |
|---------|---------------|---------------------------|
| Executivo | PROJEÇÃO COBERTURA, LINHA DE RECUO, ACESSO, notas de padrão/esgoto, notas sob as tabelas | PROJEÇÃO COBERTURA (telhado) |
| Cobertura | TELHA METÁLICA + inclinação, PINGADEIRA, CALHA, PROJEÇÃO EDIFICAÇÃO | tipo do telhado + "i = x%", PROJEÇÃO EDIFICAÇÃO |
| Cortes | chamadas vermelhas (telha, laje, forro, pingadeira, calha); "REVEST. …" e "PISO …" por ambiente | chamadas pelo nome do tipo (coluna à direita, escalonadas 4 mm); REVEST./PISO por ambiente cortado |
| Fachadas | materiais externos (pintura, pedra, vidro, porta), telha | material da camada externa das paredes + telhado |
| Forro | "FORRO= 2,70m" por ambiente | altura do forro sobre o piso |
| Tomadas/iluminação | "Tomada h= 70cm", "Ponto p/ geladeira h= 1,10m", "Ponto de espera para LED" | peças fora das alturas da legenda (0,30/1,10/2,20) ou com Comentários |
| DET bancadas | cubas/torneiras com fabricante, pedra, rodabase, "Medidas livres, não considerado 2cm embutimento." | louças (família/tipo/fabricante), material da bancada, a nota de medidas livres |

Regras: o texto vem do modelo (nome do tipo sem "VZ-"/"Acabamento", material, Comentários, parâmetros de acabamento do
ambiente); o que não está no modelo vira aviso, nunca texto inventado. Chamadas usam o tipo de texto vermelho de ~1,8 mm;
notas, o preto de ~1,8 mm. `refazer` apaga só o que a skill gera.

Ficam com o arquiteto (decisão de especificação que não está no modelo): notas sob as tabelas (granito de pingadeiras e
soleiras), LINHA DE RECUO, ACESSO, notas de padrão de energia/esgoto, rodabase e textos de marcenaria do DET, "PERFIL
NATURAL DO TERRENO". Para automatizar, a especificação precisa entrar no modelo (materiais nomeados, Comentários).

Piloto: cortes A–D com telha/laje/forro + REVEST./PISO; 4 fachadas com TELHA METÁLICA e REBOCO; forro com 4 alturas;
bancadas B02/B03 com cuba e material; tomadas sem rótulo (todas nas alturas padrão).

## Vários pavimentos
- **Cotas externas** (`06_anotar_planta.cs`, `cotas_externas`) valem em todos os pavimentos, pela mesma regra: 3 linhas por fora dos
  muros, de divisa a divisa. O lote (divisas e muros) vem do térreo; a cadeia detalhada e os extremos da edificação vêm do
  pavimento da vista. Muro = parede do térreo até 3 m que não delimita ambiente interno (ou tipo com "MURO" no nome).
- No **pavimento superior** os muros e as divisas só entram na cota se aparecerem na vista. Deixe as linhas de propriedade
  visíveis ou use o térreo como subposição; senão o relatório avisa e a cadeia fica só na edificação.
- **Cortes** (`06_cotar_pranchas.cs`, modo `corte`): cada ambiente cortado tem a cadeia do piso acabado até a laje logo acima
  (com a espessura dela); o que fica acima é do ambiente de cima. À esquerda, uma linha com o piso acabado de cada pavimento e o
  topo (pé-direito por pavimento + total).
- **Textos** (`06_textos.cs`): tomadas, iluminação e forro por planta de cada pavimento; nos cortes, REVEST. e PISO em cada
  ambiente cortado de todos os pavimentos; LAJE entre pavimentos aparece como chamada.
- **Escada:** sem cotas nem detalhe próprio (fora do escopo).

## Checklist executivo (novidades da 0.7)
- `06_anotar_planta.cs`:
  - cotas externas em três escalas (micro, médio, macro), descritas acima;
  - identificador de ambiente com perímetro: usa o tipo com "PERÍMETRO" no nome, se o template tiver; senão avisa;
  - `paisagismo` (padrão): esconde Plantação e Entorno nas plantas que não são de layout. Se o modelo de vista controla as
    categorias, só avisa (corrija no modelo de vista).
- `06_textos.cs`:
  - executivo: "PROJEÇÃO PERGOLADO" em cada pergolado ("PERGOL" no nome) e X tracejado nos vazios ("VAZIO", "PÉ DIREITO
    DUPLO", "POÇO"). Ao refazer, apague antes os X antigos (linhas de detalhe não são apagadas pelo `refazer`);
  - tomadas: alturas padrão 0,30 / 0,70 / 1,10 / 1,40 / 2,20 m;
  - iluminação: luminária de parede ou prateleira sempre com a altura;
  - modos novos `ar` (BTU/h e modelo; distância evaporadora–condensadora em linha reta) e `hidro` (torneiras, água
    quente/fria, gás e nichos com chamada e altura).

## Executivo completo (checklist, etapa 1)
1. **Implantação** — `06_implantacao.cs` com os dados do questionário:
   `["rua=RUA DAS FLORES;largura=12;asfalto=sim;numero=123;lote_esq=11;lote_dir=13;recuo_frente=4;recuo_lateral=1.5;recuo_fundo=2"]`.
   - Escreve a rua, a largura, a pavimentação, os lotes vizinhos, o número e o norte.
   - Desenha os recuos em azul tracejado (estilo "VZ-RECUO") na implantação e no executivo do térreo.
   - `"refazer"` em `parameters[1]` gera de novo.
2. **Elementos externos** — `06_elementos_externos.cs`:
   - `["propor"]` insere abrigo de medidores, gás, caixa de captação (1,5 m), lixeira, rebaixo do meio-fio, ducha (com
     piscina) e faixa de piso tátil, marcados como "SKILL: proposta".
   - Mostre a imagem da implantação ao arquiteto. Depois rode `["aprovar"]` (ele pode ajustar no Revit antes) ou
     `["desfazer"]`.
   - Família que não estiver carregada vira aviso: peça para carregar e rode `["propor","<item>"]`.
3. **Eixos hidrossanitários** — `06_cotar_pranchas.cs` modo `hidro`: cadeia parede → eixo da louça → parede, na planta de
   hidro e gás (ou na executiva, se não houver).
4. **Paginação** — `06_textos.cs` modo `paginacao` (automático pelo nome da vista):
   - "PISO <acabamento>" em cada ambiente;
   - "INÍCIO DA PAGINAÇÃO" na porta principal;
   - nota de ralos e, se houver piso com "CANALETA", a nota das canaletas.
