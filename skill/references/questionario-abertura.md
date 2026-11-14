# Questionário de abertura

Faça em blocos. Itens marcados (perfil) vêm de `config/escritorio.json` — apenas confirme.
Coluna "Parâmetro" = nome exato usado em `01_preencher_dados.cs`.

## Bloco 1 — Cliente
| Pergunta | Parâmetro | Observação |
|---|---|---|
| Nome do cliente (como aparece no carimbo) | Nome do cliente | nativo |
| Proprietário(s) / razão social | Proprietario | |
| CPF ou CNPJ do proprietário (com o rótulo, ex.: "CPF: …") | CPF do Proprietario | |

## Bloco 2 — Obra e terreno
| Pergunta | Parâmetro | Observação |
|---|---|---|
| Endereço da obra (rua, lote, quadra, bairro/loteamento, cidade-UF), em até 3 linhas | Endereço do projeto | nativo; quebras de linha permitidas |
| Área do lote (m²) | Área do lote | número |
| Nome do projeto (ex.: "PROJETO UNIRESIDENCIAL") | Nome do projeto | nativo |
| Zona urbanística e limites de TO, TP e CA | — | grava no `<projeto>.skill.json` para a conferência final |

### Bloco 2b — Situação do lote (para a planta de situação, `05_situacao.cs`)
Testada e profundidade saem do modelo (linhas de propriedade ou paredes). Se o cliente/loteadora tiver a **planta do loteamento**
(imagem, PDF ou DWG), peça o arquivo: ela é inserida com `05_situacao_importar.cs` e o arquiteto só marca o lote.
| Pergunta | Parâmetro do script | Observação |
|---|---|---|
| Número do lote e quadra | lote, quadra | ex.: 12 / B |
| Rua da frente | rua da frente | nome completo, ex.: RUA DAS FLORES |
| Distância da divisa do lote até a esquina mais próxima (m) | distância | da matrícula ou da planta do loteamento |
| Nome da rua da esquina e de que lado ela fica, olhando o lote da rua (D/E) | rua da esquina, lado | |
| Números dos lotes vizinhos (esquerda e direita, olhando da rua) | vizinhos | opcional; sem eles o desenho mostra "—" |
| Para qual ponto cardeal a frente do lote está voltada (N, NE, L, SE, S, SO, O, NO) | cardeal | gira o símbolo de norte; o norte do modelo costuma não estar ajustado |
| Largura das ruas (m) | largura | padrão 12 |

## Bloco 3 — Projeto
| Pergunta | Onde vai | Observação |
|---|---|---|
| Obra nova ou reforma | Status do projeto + módulo reforma | reforma ativa as vistas de existente/demolir |
| Número de pavimentos | `01_ajustar_pavimentos.cs` | 1 folha de proposta por pavimento |
| Cota do piso acabado de cada pavimento (m) | `NIVEL:PAV. TERREO N.A`, `NIVEL:PAV. SUPERIOR N.A` | osso (N.O) = acabado − 0,05 |
| Cota da calçada (m) | `NIVEL:CALÇADA` | |
| Etapa contratada | ETAPA DO PROJETO | ex.: PROPOSTA, EXECUTIVO |
| Módulos opcionais: caderno de pinturas, hidrossanitário, iluminação externa, setorização | `<projeto>.skill.json` | usados na fase 5 |

## Bloco 4 — Responsáveis e documentos
| Pergunta | Parâmetro | Observação |
|---|---|---|
| Autor do projeto (perfil) | Autor do projeto | ex.: "ARQ. NOME SOBRENOME" |
| CAU (perfil) | Numero CAU | com rótulo: "CAU: 000000-0" |
| Executor da obra | Executor da obra | |
| CREA do executor | Numero CREA | com rótulo: "CREA: …" |
| Desenhista | Desenhista | |
| RRT de projeto | RRT | |
| ART de execução | ART | |
| Data de emissão | Data de emissão do projeto | nativo, dd/mm/aaaa |

## Do perfil do escritório (não pergunte, só confirme)
| Campo do perfil | Parâmetro |
|---|---|
| nome | Nome da organização |
| endereco_contato | Descrição da organização |
| observacoes_gerais | OBSERVAÇÕES GERAIS |
