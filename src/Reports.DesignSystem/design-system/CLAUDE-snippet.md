## Design system

Toda interface (app em Razor e relatório PDF) segue o design system em `wwwroot/design-system/`. Antes de criar ou alterar qualquer tela, view, partial ou CSS, leia `wwwroot/design-system/DESIGN.md`.

- Use só tokens (`var(--…)` de `tokens/tokens.css`); nada de cor, fonte, tamanho ou espaçamento literal.
- Reaproveite as classes `ra-*` de `css/ra.css` e os partials em `Views/Shared/Ui` e `Views/Shared/Report` antes de escrever CSS novo.
- Para mudar um token, edite `wwwroot/design-system/tools/gen_tokens.py`, rode o script e atualize a tabela no DESIGN.md.
- O relatório PDF é sempre `data-theme="light"`; cada `.ra-page` é uma folha A4 e não pode transbordar (pagine no servidor).
- Estado sempre com `ra-badge` e texto; fontes de dados com `ra-source--ad|gh|az|aw`, nunca logos oficiais.
- Textos em português do Brasil; números em pt-BR e em fonte mono.
- Referência visual: `wwwroot/design-system/examples/app.html` e `wwwroot/design-system/examples/relatorio.html`.
