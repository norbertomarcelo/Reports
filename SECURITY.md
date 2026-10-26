# Política de segurança

## Como reportar uma vulnerabilidade

Não abra uma issue pública. Use o **Private vulnerability reporting** do GitHub:
aba **Security** → **Report a vulnerability**. Só o mantenedor vê o relato.

Descreva o que encontrou, como reproduzir e o impacto esperado. A resposta inicial vem em até 7 dias.

## Escopo

- A API (`POST /azure-devops`) e a validação das requisições.
- O tratamento do PAT do Azure DevOps (`AzureDevOps:AccessToken`), que nunca deve aparecer em logs, respostas ou arquivos versionados.
- A geração do relatório (HTML e PDF), incluindo conteúdo vindo do Azure DevOps que acabe no documento.
- O pipeline de CI (`.github/workflows`).

## Versões suportadas

Só a versão mais recente da branch `main` recebe correções.
