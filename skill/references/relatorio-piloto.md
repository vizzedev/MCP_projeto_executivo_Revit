# Relatório do piloto — casa térrea 10 × 10 m (14/11/2026)

Projeto criado do zero a partir do template master: quarto, sala, cozinha e banheiro (≈ 41 m²), forro de gesso,
laje e cobertura em telha metálica (1 água, 10 %), muros de divisa, calçada. Todas as fases (0 a 11) rodaram;
emitidos 3 PDFs: PROPOSTA (1 folha), ARQ (7 folhas A1) e DET (3 folhas A3).

## Corrigido na skill durante o piloto
| Fase | Problema | Correção |
|---|---|---|
| geral | Avisos do Revit abriam janela modal e travavam o MCP | regra 8 do SKILL.md: bloco que apaga avisos em todo script que altera o modelo |
| 2/5 | Implantação vazia sem linhas de propriedade | novo `02_desenhar_lote.cs` (lote, projeção, cotas, recorte) |
| 2/9 | CONTEÚDO DA PRANCHA vazio na proposta e no DET | preenchido pelos scripts |
| 3 | Marcas de tipo fora do padrão (40, J24) | aviso + `renumerar_esquadrias` |
| 4 | Porta interna pegava o ambiente errado | prioridade banheiro > quarto > serviço > cozinha |
| 5 | Corte C a 10 cm de uma parede | corte pelo banheiro principal + afastamento de paredes |
| 5 | Implantação acusava "0 telhados" (falso) | confere linhas de divisa/lote |
| 5 | Legenda grande invadia o carimbo na 1/7 | legenda vai para baixo quando não cabe |
| 8 | Áreas do carimbo zeradas (sem regiões) | novo `08_regioes_areas.cs` |
| 9 | Folhas DET vazias do template contadas na numeração | removidas antes de numerar |
| 9 | Elevação de esquadria de lado (eixos girados) | escolha pela largura real da geometria |
| 10 | Vistas do pavimento superior (casa térrea) acusadas | ignoradas quando o pavimento não tem ambientes |

## Pendências do template master (resolver no master, não na skill)
1. ~~Regiões e áreas de exemplo~~ — **resolvido em 18/11/2026** (28 áreas, 69 linhas de limite de área e regiões apagadas). As 2 linhas
   de propriedade de exemplo foram **mantidas por decisão** (a equipe ajusta o lote e o recuo; a skill confere na abertura).
2. ~~Legenda da implantação com valores fixos~~ — **resolvido em 18/11/2026**: "A = —" no template, preenchida por `02_areas_legenda.cs`.
3. ~~"Janela Padrão" 2,50 × 2,50~~ — **resolvido em 18/11/2026**: 1,50 × 1,20 m e peitoril padrão 1,00 m.
4. ~~Marcas de porta fora do padrão~~ — **resolvido em 18/11/2026**: 13 tipos renumerados para Pnn livres (ver `01_TEMPLATE/LOG_MASTER_v1.md`).
5. Famílias de porta sem ABERTURA e MATERIAL preenchidos — as tabelas saem com colunas vazias.
6. Arquivo pesado: o projeto nasce com ~200 MB. Expurgar famílias e vistas não usadas do master.
7. Vista "PAVIMENTO TERREO PAGINAÇÃO DE PISO" pinta a grama de preto sólido — rever o modelo de vista.
8. Incluir a hachura e a região do lote criadas pela fase 5, e o logotipo por escritório.
9. Família "Calçada, Meio-Fio, Rua": a geometria não acompanha os parâmetros (meio-fio e sarjeta fixos em 19 m, rebaixo
   preso à posição original). A skill não a usa — gera "VZ-Meio-Fio com Rebaixo" (`06_meio_fio.cs`). Corrigir ou remover do master.
10. Filtro "VZ-GRAMA PREENCHIDA" só pega o tipo "VZ-Acabamento Grama Preenchida": no executivo a grama sai branca. Regra nova
    (corrigida no projeto teste): tipo contém "Grama" e não contém "Pisograma"/"Concregrama". Aplicar no master.
11. Nível "CALÇADA" a −1,45 no template: os cortes nascem com uma faixa vazia. A skill ajusta com `05_terra_cortes.cs`.
12. Recorte da vista "PAVIMENTO TERREO EXECUTIVO" vem desligado (e ancorado em x = −0,43): `06_anotar_planta.cs` liga e ajusta.

## Continua manual (por decisão)
Folha de telhado do DET, cotas L/H das esquadrias, textos de especificação das bancadas, ajuste fino de identificadores
em ambientes pequenos, cotas externas da planta executiva.

## Observado, não corrigido
- Cotas: regra nova do escritório — nada de cotas internas; cadeia integrada + total do lote por fora dos muros, nas 4 faces (`cotas_externas`). Falta validar no piloto.
- Resolvido: folhas 1/7, 2/7 e 5/7 de casa pequena montam em duas colunas (vista + legendas embaixo) quando cabem;
  `parameters[4] = "empilhar"` força o layout antigo.
- Resolvido: título das bancadas no DET com o ambiente (BANCADA B02 - COZINHA).

## Limpeza do template master — `scripts/00_limpar_template.cs`
Para o responsável pelo template, numa CÓPIA (o modo "aplicar" recusa arquivo sem COPIA/TESTE no nome):
1. `["relatorio"]` — lista linhas de propriedade, regiões das vistas de área, modelo do exemplo, janelas "Padrão"
   largas, marcas de porta/janela fora do padrão, tipos sem ABERTURA/MATERIAL, legendas com valores fixos e o
   número de elementos não usados.
2. `["aplicar", "divisas;regioes_area;modelo"]` — apaga as sobras do exemplo; conferir as vistas.
3. `["aplicar", "purgar"]` — remove não usados (até 3 rodadas); reduz o tamanho do arquivo.
4. Corrigir à mão o que o relatório aponta (largura da Janela Padrão, marcas, ABERTURA/MATERIAL, legendas) e salvar
   a cópia com outro nome antes de substituir o master.
Teste no piloto (só relatório): 4 tipos de janela acima de 2 m (Janela Padrão 2,50 m), 13 marcas de porta fora do
padrão (1, P2.1…, 41…), 3 legendas com valores fixos, 1080 elementos purgáveis.
