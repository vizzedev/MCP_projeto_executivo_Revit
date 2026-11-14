# Pasta-base (modelo)

Estrutura que cada escritório cria no servidor. A skill e o instalador procuram estes caminhos.

```
<pasta-base>/
  01_TEMPLATE/
    TEMPLATE_MASTER_v1.rte        → template master do escritório (não vai para o GitHub)
  02_PROJETO_REFERENCIA/          → projeto de referência (.rvt) + PDFs emitidos
  03_DOCUMENTOS/                  → manuais, memoriais, modelos de documentos
  04_BIBLIOTECA_DETALHES/         → detalhes padrão
  05_FAMILIAS/                    → famílias que não estão no template
  _config/
    PARAMETROS_COMPARTILHADOS_SKILL.txt   → copie deste repositório (GUIDs fixos, não edite)
    maquinas/                     → gerado pelo instalador (um .json por computador)
  installer/                      → opcional: cópia da pasta installer/ do repositório
```

O arquivo `PARAMETROS_COMPARTILHADOS_SKILL.txt` está em UTF-16 (formato do Revit). Edite apenas pelo Revit (Gerenciar › Parâmetros compartilhados).
