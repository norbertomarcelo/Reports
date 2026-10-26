# Design system — Relatórios de Ambientes

Guia de implementação para o app (ASP.NET Core + Razor) e para o relatório em PDF (HTML → PDF com Playwright). Escrito para ser lido por pessoas e pelo Claude Code antes de qualquer mudança de interface.

A identidade nasce do ícone **Nuvem → documento**: dados espalhados em várias fontes (Azure DevOps, GitHub, Azure, AWS) descem para a nuvem e viram um documento claro. Toda tela repete essa ideia: a fonte aparece pequena e identificada, o relatório é o protagonista.

## 1. Arquivos

```
design-system/
├── DESIGN.md                 este guia
├── CLAUDE-snippet.md         trecho para colar no CLAUDE.md do repositório
├── tools/gen_tokens.py       FONTE ÚNICA dos tokens; gera tokens.json, tokens.css e fonts.css
├── tokens/
│   ├── tokens.json           tokens em JSON (gerado; leitura por ferramentas e pelo Claude)
│   └── tokens.css            variáveis CSS (gerado)
├── css/
│   ├── fonts.css             @font-face das fontes locais
│   ├── ra.css                componentes (prefixo ra-)
│   └── report.css            folha A4, capa e regras de impressão
├── fonts/                    Sora, IBM Plex Sans, IBM Plex Mono (woff2, SIL OFL 1.1)
├── assets/                   app-icon.svg, mark.svg, favicon.svg
└── examples/
    ├── app.html              tela do app com todos os componentes (claro/escuro)
    └── relatorio.html        relatório completo: capa + 2 folhas A4

razor-starter/                (fica FORA do wwwroot) ponto de partida em ASP.NET Core
├── Models/ReportViewModels.cs
├── Services/ReportPdfRenderer.cs
└── Views/
    ├── Report/Pdf.cshtml
    └── Shared/{Ui,Report}/_*.cshtml
```

## 2. Como instalar no projeto

1. Copie a pasta `design-system/` inteira para `wwwroot/design-system/` do projeto web. Os caminhos relativos entre `css/`, `fonts/` e `examples/` precisam ser mantidos.
2. No `_Layout.cshtml` do app, nesta ordem:
   ```html
   <link rel="icon" href="~/design-system/assets/favicon.svg" type="image/svg+xml">
   <link rel="stylesheet" href="~/design-system/tokens/tokens.css" asp-append-version="true">
   <link rel="stylesheet" href="~/design-system/css/fonts.css" asp-append-version="true">
   <link rel="stylesheet" href="~/design-system/css/ra.css" asp-append-version="true">
   ```
   e `<body class="ra-root">`.
3. O relatório usa também `css/report.css` e é sempre claro: `<html data-theme="light">`. Veja `razor/Views/Report/Pdf.cshtml`.
4. Copie `razor-starter/Models`, `razor-starter/Services` e `razor-starter/Views` para a raiz do projeto web (nunca para dentro de `wwwroot`, senão o `.cs` entra na compilação como arquivo estático), ajuste o namespace (`Reports.Web`) e registre o renderer: `builder.Services.AddSingleton<ReportPdfRenderer>();`.
5. Abra `examples/app.html` e `examples/relatorio.html` no navegador para ter a referência visual.

Não há build de front-end, npm ou CDN: tudo é CSS e fontes locais. Isso é proposital, porque o Chromium que gera o PDF pode rodar sem acesso à internet.

## 3. Regras que não mudam

- **Só tokens.** Nenhuma cor, fonte, tamanho de texto, espaçamento, raio ou sombra literal em CSS novo. Use `var(--token)`. Se faltar um valor, adicione em `tools/gen_tokens.py`, rode `python tools/gen_tokens.py` e documente o uso na seção 4.
- **Classes `ra-` antes de CSS novo.** Antes de criar estilo, procure o componente em `ra.css`. CSS de página só para layout (grid, posição), nunca para recriar botão, badge, tabela.
- **Estado tem texto.** Saudável, atenção e falha aparecem como `ra-badge` com rótulo. Nunca só cor.
- **Fonte identificada por chip, não por logo.** Não usar logos oficiais de Azure, GitHub ou AWS na interface. Use `ra-source--ad|gh|az|aw` com a sigla.
- **Números e IDs em mono.** Valores, datas, códigos de relatório, nomes de recurso e pipeline usam `ra-num`/`ra-mono`/`ra-id`, com `tabular-nums`.
- **Um primário por tela.** `ra-btn--primary` só para a ação principal da tela.
- **Dois temas no app, um no PDF.** O app segue o sistema (claro/escuro) e aceita `data-theme` no `<html>`. O relatório força `data-theme="light"`.
- **Degradê só onde ele já existe:** capa, faixa do topo da folha, bloco de resumo e numeração das recomendações. Não usar em botões, cards ou fundos de tela.
- **Português do Brasil, frases curtas.** Botões começam com verbo (“Gerar relatório”, “Baixar PDF”). Erros dizem o que houve e como resolver. Números no formato pt-BR (1.204; 94,2%; R$ 48.320).

## 4. Tokens

Fonte única: `tools/gen_tokens.py`. `tokens.json`, `tokens.css` e `fonts.css` são gerados por ele; não edite à mão.

### Cores (claro / escuro)

| Token | Claro | Escuro | Uso |
| --- | --- | --- | --- |
| `surface` | #f4f8f8 | #0b1416 | Fundo da página |
| `surface-raised` | #ffffff | #12201f | Cards, folhas, inputs |
| `surface-sunken` | #e7efef | #071012 | Linhas alternadas, trilhas, desativado |
| `line` / `line-strong` | #d3e0e0 / #a9bfc0 | #1f3533 / #35514f | Bordas; `line-strong` em inputs e botões |
| `ink` / `ink-muted` | #0c1f22 / #4a5f62 | #e6f1f1 / #93abad | Texto principal / secundário |
| `teal` · `teal-soft` · `on-teal` | #0f766e · #ccfbf1 · #fff | #2dd4bf · #0b3a36 · #04201d | Marca, ação primária, foco |
| `indigo` · `indigo-soft` | #1e40af · #dbeafe | #93c5fd · #172554 | Links, informação, Azure |
| `orange` · `orange-soft` · `orange-fill` | #c2410c · #ffedd5 · #ea580c | #fb923c · #431407 · #f97316 | AWS e custo (`-fill` só em barras) |
| `success` · `warning` · `danger` (+ `-soft`) | #15803d · #a16207 · #b91c1c | #4ade80 · #facc15 · #f87171 | Estados |
| `sky` | #a5f3fc | #67e8f9 | A nuvem; só ilustração |
| `doc-line` | #cbd5e1 | #334155 | Linhas-esqueleto e carregamento |
| `grad-start` → `grad-end` | #0f766e → #1e40af | igual | Degradê da marca (`--grad`) |
| `on-grad` · `on-grad-accent` | #fff · #ccfbf1 | igual | Texto e rótulos sobre o degradê |

Cores por fonte (aliases): `provider-ad` = teal, `provider-gh` = ink, `provider-az` = indigo, `provider-aw` = orange; cada uma com `-soft` (fundo) e `-fill` (barras e medidores).

Todos os pares de texto acima passam 4,5:1 nos dois temas (verificado). `sky` e `orange-fill` nunca são usados como cor de texto.

### Tipografia

| Estilo | Família | Tamanho / linha | Peso | Uso |
| --- | --- | --- | --- | --- |
| `cover` | Sora | 48/52 | 600 | Título da capa |
| `display` | Sora | 40/44 | 600 | Destaque único, tela vazia |
| `h1` | Sora | 28/34 | 600 | Título da página |
| `h2` | Sora | 18/26 | 600 | Título de seção |
| `subtitle` | Sora | 22/30 | 400 | Período na capa |
| `body` | IBM Plex Sans | 15/24 | 400 | Texto do app |
| `report-body` | IBM Plex Sans | 13/20 | 400 | Texto do relatório A4 |
| `small` | IBM Plex Sans | 13/20 | 400 | Metadados, ajuda |
| `caption` | IBM Plex Sans | 11/16 | 400 | Rodapé, notas |
| `label` | IBM Plex Sans | 11/16 | 600, caixa alta, +0,06em | KPI, cabeçalho de tabela, eyebrow |
| `mono` | IBM Plex Mono | 12/18 | 400 | IDs, datas, números em tabela |
| `metric` | IBM Plex Mono | 24/30 | 500 | Valor de KPI |

Cada estilo vira `--text-<nome>` (atalho de `font`), por exemplo `font: var(--text-h2);`.

### Espaço, raio, sombra

Espaço em grade de 4 px: `space-1` 4 · `space-2` 8 · `space-3` 12 · `space-4` 16 · `space-6` 24 · `space-8` 32 · `space-12` 48 · `space-14` 56 · `space-16` 64.
Raio: `radius-sm` 4 (badge, chip) · `radius-md` 8 (botão, input, alerta) · `radius-lg` 14 (card, KPI, painel) · `radius-xl` 18 · `radius-pill` · `radius-icon` 22% (só o ícone).
Sombra: `shadow-card` (card-documento), `shadow-pop` (toast, menu), `shadow-page` (folha A4 na tela; nunca no PDF). Prefira borda `line` a sombra.

## 5. Componentes

Todos em `css/ra.css`. Os exemplos completos estão em `examples/`.

| Componente | Classes | Notas |
| --- | --- | --- |
| Botão | `ra-btn`, `--primary`, `--ghost`, `--danger`, `--sm` | `disabled` ou `aria-disabled="true"` |
| Badge de estado | `ra-badge`, `--ok`, `--warn`, `--fail`, `--brand` | Rótulos: Saudável, Atenção, Falhou, Sem dados |
| Chip de fonte | `ra-source ra-source--ad|gh|az|aw` + `ra-source__mark` | `--icon` só a sigla (com `title`); `--on-grad` na capa |
| Campo | `ra-field`, `ra-field__label`, `ra-input`, `ra-select`, `ra-textarea`, `ra-field__help`, `ra-field__error`, `ra-check` | Erro: `aria-invalid="true"` + `ra-field__error` |
| Card | `ra-card`, `ra-card--doc`, `ra-card__head` | `--doc` só para o card que representa um relatório |
| KPI | `ra-kpi --teal|success|indigo|orange`, `__label`, `__value`, `__delta` | Em grade de 4 (`ra-grid` com `--cols:4`) |
| Resumo | `ra-summary` | Fundo em degradê; `ra-label` e `ra-mono` viram `on-grad-accent` |
| Alerta | `ra-alert --fail|warn|ok|info` + `ra-badge` | Título em `<b>`, depois a ação recomendada |
| Saúde por fonte | `ra-health ra-source--xx`, `ra-meter`, `ra-meter__fill` (`--value: 96%`) | Medidor na cor da fonte |
| Tabela | `ra-table-wrap` > `ra-table ra-table--ad|gh|az|aw|app` | `ra-num` à direita, `ra-id` em mono |
| Custo | `ra-bars --az|aw`, `ra-bar`, `__track`, `__fill` (`--value`), `__value`, `ra-bars__total` | Mesma escala entre painéis |
| Recomendações | `ol.ra-steps` | Numeração automática em círculo com degradê |
| Seção | `ra-section` > `ra-section__title` (`--plain` quando abre com chip) | Marcador em degradê |
| Abas | `ra-tabs` > `ra-tab` (`aria-current="page"` ou `aria-selected="true"`) | |
| Estado vazio | `ra-empty`, `ra-empty__art` (mark.svg) | Sempre com a ação que resolve |
| Toast | `ra-toast` | `role="status"` |
| Carregamento | `ra-doclines` > `span` | Linhas do documento, larguras 100/100/62% |
| Shell | `ra-shell` > `ra-sidebar` (`ra-sidebar__brand`, `ra-nav`, `ra-nav__heading`, `ra-nav__item[aria-current]`) + `ra-main` (`ra-pagehead`) | Vira barra horizontal abaixo de 860 px |

Partials Razor prontos: `Ui/_Badge`, `Ui/_SourceChip`, `Ui/_Kpi`, `Ui/_Alert`, `Ui/_Mark`, `Report/_Cover`, `Report/_PageHeader`, `Report/_PageFooter`, `Report/_HealthGrid`, `Report/_DataTable`, `Report/_CostPanels`, `Report/_Recommendations`. Os enums e helpers de classe ficam em `Ra` (`ReportViewModels.cs`).

## 6. O relatório em PDF

Estrutura: **capa** → **visão geral** (KPIs, resumo, saúde por fonte, até 4 alertas) → **uma folha por fatia de tabela** → **custos e recomendações**.

- Cada `<article class="ra-page">` é exatamente uma folha A4 (210 × 297 mm, margem zero, `break-after: page`). O conteúdo **não pode passar da folha**: a altura é fixa na impressão e o excesso é cortado.
- Pagine no servidor. `Pdf.cshtml` fatia cada tabela em blocos de `MaxRows = 18` linhas. Ajuste se mudar a densidade.
- Cabeçalho de folha com código e `n/total`; rodapé com o código.
- Valores de barra (`--value`) em porcentagem com **ponto decimal** (`Ra.Pct`), porque CSS não aceita vírgula.
- Geração com Playwright (`Services/ReportPdfRenderer.cs`): abrir a URL interna da view, esperar `document.fonts.ready` e chamar `PdfAsync` com `Format = "A4"`, `PrintBackground = true` (sem isso o degradê e os fundos somem) e `PreferCSSPageSize = true`.
- Reaproveite um único navegador (singleton) e crie um contexto por relatório.
- A view do PDF não deve depender de JavaScript nem de recursos externos.

## 7. Acessibilidade

- Contraste mínimo 4,5:1 para texto em qualquer tema; 3:1 para bordas de controle e ícones.
- Foco visível: contorno de 2 px `focus-ring` com 2 px de afastamento (já vem em `.ra-root`).
- Rótulo em todo campo (`<label for>`), `aria-invalid` + mensagem em erro, `aria-current` na navegação.
- Chip só com sigla precisa de `title` ou `aria-label` com o nome da fonte.
- Respeitar `prefers-reduced-motion` (já tratado em `.ra-root`).

## 8. Checklist antes de abrir PR

- [ ] Nenhuma cor, tamanho ou espaço literal novo; só `var(--…)`.
- [ ] Componentes existentes reaproveitados (`ra-*`), nenhum recriado.
- [ ] Tela conferida em tema claro e escuro.
- [ ] Estados com badge e texto.
- [ ] Números em pt-BR e em mono.
- [ ] PDF gerado e conferido: nenhuma folha transbordando, cores e degradê presentes, fontes corretas.
