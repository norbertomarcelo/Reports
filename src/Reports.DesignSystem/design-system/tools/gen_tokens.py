"""Fonte única dos tokens: as listas abaixo geram tokens/tokens.json, tokens/tokens.css e css/fonts.css.
Para mudar um token, edite aqui e rode: python tools/gen_tokens.py"""
import json, sys

import os
# Uso: python tools/gen_tokens.py   (a partir da pasta design-system/)
PKG = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

C = [
 # nome, claro, escuro, uso
 ("surface", "#f4f8f8", "#0b1416", "Fundo da página."),
 ("surface-raised", "#ffffff", "#12201f", "Cards, folhas do relatório, menus e inputs sobre surface."),
 ("surface-sunken", "#e7efef", "#071012", "Linhas alternadas de tabela, áreas de código, trilhas de barra e campos desativados."),
 ("line", "#d3e0e0", "#1f3533", "Bordas finas, divisórias e borda de inputs."),
 ("line-strong", "#a9bfc0", "#35514f", "Borda de input em hover e bordas que precisam de 3:1."),
 ("ink", "#0c1f22", "#e6f1f1", "Texto principal sobre surface, surface-raised e surface-sunken."),
 ("ink-muted", "#4a5f62", "#93abad", "Texto secundário, metadados, rótulos e legendas sobre qualquer surface."),
 ("teal", "#0f766e", "#2dd4bf", "Cor da marca: ação principal, item ativo e foco; como texto, sobre as surfaces ou teal-soft."),
 ("teal-soft", "#ccfbf1", "#0b3a36", "Fundo tingido atrás de texto teal (badges, KPI, cabeçalho de tabela, item de menu ativo)."),
 ("on-teal", "#ffffff", "#04201d", "Texto e ícones sobre preenchimento teal (botão primário)."),
 ("indigo", "#1e40af", "#93c5fd", "Segunda cor da marca: links, informação e Azure; como texto, sobre as surfaces ou indigo-soft."),
 ("indigo-soft", "#dbeafe", "#172554", "Fundo tingido atrás de texto indigo."),
 ("orange", "#c2410c", "#fb923c", "AWS e destaques de custo; como texto, sobre as surfaces ou orange-soft."),
 ("orange-soft", "#ffedd5", "#431407", "Fundo tingido atrás de texto orange."),
 ("orange-fill", "#ea580c", "#f97316", "Barras e medidores laranja. Só preenchimento, nunca texto."),
 ("sky", "#a5f3fc", "#67e8f9", "A nuvem do ícone. Só ilustração; nunca texto."),
 ("doc-line", "#cbd5e1", "#334155", "Linhas-esqueleto de documento e estados de carregamento."),
 ("success", "#15803d", "#4ade80", "Saudável, aprovado; como texto, sobre as surfaces ou success-soft."),
 ("success-soft", "#dcfce7", "#052e16", "Fundo de badge e alerta de sucesso."),
 ("warning", "#a16207", "#facc15", "Atenção, item a revisar; como texto, sobre as surfaces ou warning-soft."),
 ("warning-soft", "#fef9c3", "#332605", "Fundo de badge e alerta de atenção."),
 ("danger", "#b91c1c", "#f87171", "Falha, crítico, erro; como texto, sobre as surfaces ou danger-soft."),
 ("danger-soft", "#fee2e2", "#3f0d0d", "Fundo de badge e alerta crítico."),
 ("grad-start", "#0f766e", "#0f766e", "Início do degradê da marca (capa, faixa do topo, bloco de resumo). Igual nos dois temas."),
 ("grad-end", "#1e40af", "#1e40af", "Fim do degradê da marca. Igual nos dois temas."),
 ("on-grad", "#ffffff", "#ffffff", "Texto sobre o degradê."),
 ("on-grad-accent", "#ccfbf1", "#ccfbf1", "Rótulos, eyebrow e trechos mono sobre o degradê."),
]
ALIAS = [
 ("provider-ad", "teal", "Azure DevOps: chip, cabeçalho de tabela e medidor."),
 ("provider-ad-soft", "teal-soft", "Fundo do chip e do cabeçalho de tabela de Azure DevOps."),
 ("provider-gh", "ink", "GitHub."),
 ("provider-gh-soft", "surface-sunken", "Fundo do chip e do cabeçalho de tabela de GitHub."),
 ("provider-az", "indigo", "Azure."),
 ("provider-az-soft", "indigo-soft", "Fundo do chip, do cabeçalho e do painel de custo Azure."),
 ("provider-aw", "orange", "AWS."),
 ("provider-aw-soft", "orange-soft", "Fundo do chip, do cabeçalho e do painel de custo AWS."),
 ("focus-ring", "teal", "Contorno de foco do teclado (2px, com 2px de afastamento)."),
]
FILLS = {"provider-ad": "teal", "provider-gh": "ink", "provider-az": "indigo", "provider-aw": "orange-fill"}

FAMILIES = {
 "display": "\"Sora\", \"Segoe UI\", system-ui, sans-serif",
 "sans": "\"IBM Plex Sans\", \"Segoe UI\", system-ui, sans-serif",
 "mono": "\"IBM Plex Mono\", ui-monospace, \"Cascadia Code\", Consolas, monospace",
}
FONTS = [
 ("Sora", "fonts/sora-latin-400-normal.woff2", "400", "normal"),
 ("Sora", "fonts/sora-latin-600-normal.woff2", "600", "normal"),
 ("IBM Plex Sans", "fonts/ibm-plex-sans-latin-400-normal.woff2", "400", "normal"),
 ("IBM Plex Sans", "fonts/ibm-plex-sans-latin-400-italic.woff2", "400", "italic"),
 ("IBM Plex Sans", "fonts/ibm-plex-sans-latin-600-normal.woff2", "600", "normal"),
 ("IBM Plex Sans", "fonts/ibm-plex-sans-latin-600-italic.woff2", "600", "italic"),
 ("IBM Plex Mono", "fonts/ibm-plex-mono-latin-400-normal.woff2", "400", "normal"),
 ("IBM Plex Mono", "fonts/ibm-plex-mono-latin-500-normal.woff2", "500", "normal"),
]
TYPE = [
 ("Títulos", "display", [
  ("cover", "48px", "52px", 600, "-0.02em", "Título da capa do relatório."),
  ("display", "40px", "44px", 600, "-0.02em", "Título de tela vazia ou destaque único."),
  ("h1", "28px", "34px", 600, "-0.01em", "Título da página."),
  ("h2", "18px", "26px", 600, None, "Título de seção."),
  ("subtitle", "22px", "30px", 400, None, "Subtítulo da capa (período)."),
 ]),
 ("Texto", "sans", [
  ("body", "15px", "24px", 400, None, "Texto da interface."),
  ("body-strong", "15px", "24px", 600, None, "Ênfase no texto da interface."),
  ("report-body", "13px", "20px", 400, None, "Texto corrido dentro do relatório A4."),
  ("small", "13px", "20px", 400, None, "Metadados e ajuda."),
  ("caption", "11px", "16px", 400, None, "Rodapé, notas de escala."),
  ("label", "11px", "16px", 600, "0.06em", "Rótulos em caixa alta: KPI, cabeçalho de tabela, eyebrow."),
 ]),
 ("Dados", "mono", [
  ("mono", "12px", "18px", 400, None, "IDs, nomes de recurso, datas e números em tabela."),
  ("metric", "24px", "30px", 500, None, "Valor de KPI."),
 ]),
]
SPACING = [(1, 4, "Entre ícone e rótulo."), (2, 8, "Gap de chips e badges; gap entre alertas."),
 (3, 12, "Gap entre itens de uma linha; gap interno de seções."), (4, 16, "Padding de cards e células."),
 (6, 24, "Entre blocos de um relatório."), (8, 32, "Entre seções da interface."),
 (12, 48, "Padding vertical da folha A4 e da capa."), (14, 56, "Padding lateral da folha A4."), (16, 64, "Margem de página em telas largas.")]
RADIUS = [("radius-sm", "4px", "Badges, chips, trilhas de barra."), ("radius-md", "8px", "Botões, inputs, alertas, tabelas."),
 ("radius-lg", "14px", "Cards, KPI, painéis e bloco de resumo."), ("radius-xl", "18px", "Documento da ilustração da capa."),
 ("radius-pill", "9999px", "Medidores, numeração das recomendações."), ("radius-icon", "22%", "Só o ícone do app (22 de 100).")]
SHADOW = [("shadow-card", "0 1px 2px rgba(12,31,34,.06), 0 4px 16px rgba(12,31,34,.06)", "Card que representa um documento. Prefira borda line."),
 ("shadow-pop", "0 4px 12px rgba(12,31,34,.12), 0 16px 40px rgba(12,31,34,.12)", "Menus, toasts e diálogos."),
 ("shadow-page", "0 2px 6px rgba(12,31,34,.08), 0 12px 32px rgba(12,31,34,.10)", "Folha A4 na pré-visualização em tela. Nunca no PDF.")]

def tok(n, l, d, u): return {"name": n, "value": {"light": l, "dark": d}, "usage": u}
colors = [tok(*c) for c in C] + [tok(n, "{%s}" % a, "{%s}" % a, u) for n, a, u in ALIAS]
groups = []
for g, fam, styles in TYPE:
  st = []
  for n, fs, lh, fw, ls, u in styles:
    s = {"name": n, "fontSize": fs, "lineHeight": lh, "fontWeight": fw}
    if ls: s["letterSpacing"] = ls
    s["usage"] = u
    st.append(s)
  groups.append({"name": g, "family": fam, "styles": st})
tokens = {"name": "Relatórios de Ambientes", "version": 2,
 "color": {"themes": [{"id": "light", "name": "Claro"}, {"id": "dark", "name": "Escuro"}], "tokens": colors},
 "type": {"fonts": [{"family": f, "file": p, "weight": w, "style": s} for f, p, w, s in FONTS], "families": FAMILIES, "groups": groups},
 "spacing": {"tokens": [{"name": f"space-{k}", "value": f"{v}px", "usage": u} for k, v, u in SPACING]},
 "radius": {"tokens": [{"name": n, "value": v, "usage": u} for n, v, u in RADIUS]},
 "shadow": {"tokens": [{"name": n, "value": v, "usage": u} for n, v, u in SHADOW]}}

json.dump(tokens, open(f"{PKG}/tokens/tokens.json", "w", encoding="utf-8"), ensure_ascii=False, indent=2)

# ---------- tokens.css ----------
def block(theme):
  out = []
  for n, l, d, u in C: out.append(f"  --{n}: {l if theme == 'light' else d};")
  return "\n".join(out)
alias_css = "\n".join(f"  --{n}: var(--{a});" for n, a, u in ALIAS)
fill_css = "\n".join(f"  --{n}-fill: var(--{a});" for n, a in FILLS.items())
type_css = []
for g, fam, styles in TYPE:
  for n, fs, lh, fw, ls, u in styles:
    type_css.append(f"  --text-{n}: {fw} {fs}/{lh} var(--font-{fam});" + (f" --text-{n}-tracking: {ls};" if ls else ""))
css = f"""/* Relatórios de Ambientes — tokens. GERADO a partir de tokens.json; não edite à mão. */
:root {{
  color-scheme: light;
{block('light')}
{alias_css}
{fill_css}
  --grad: linear-gradient(135deg, var(--grad-start) 0%, var(--grad-end) 100%);

  --font-display: {FAMILIES['display']};
  --font-sans: {FAMILIES['sans']};
  --font-mono: {FAMILIES['mono']};
{chr(10).join(type_css)}

{chr(10).join(f"  --space-{k}: {v}px;" for k, v, u in SPACING)}

{chr(10).join(f"  --{n}: {v};" for n, v, u in RADIUS)}

{chr(10).join(f"  --{n}: {v};" for n, v, u in SHADOW)}
}}

/* Tema escuro: segue o sistema, a menos que data-theme="light" force o claro. */
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{
    color-scheme: dark;
{chr(10).join('  ' + l for l in block('dark').splitlines())}
  }}
}}
:root[data-theme="dark"] {{
  color-scheme: dark;
{block('dark')}
}}
"""
open(f"{PKG}/tokens/tokens.css", "w", encoding="utf-8").write(css)

fonts_css = "/* Relatórios de Ambientes — fontes locais (SIL OFL 1.1). Caminhos relativos a css/. */\n" + "\n".join(
  f"@font-face {{ font-family: \"{f}\"; src: url(\"../{p}\") format(\"woff2\"); font-weight: {w}; font-style: {s}; font-display: swap; }}"
  for f, p, w, s in FONTS) + "\n"
open(f"{PKG}/css/fonts.css", "w", encoding="utf-8").write(fonts_css)
print("ok", len(colors), "cores")
