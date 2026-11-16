# Versionamento semântico com pull request

O Painel do Posto evolui em versões **MAIOR.MENOR.CORREÇÃO** ([semver.org](https://semver.org/lang/pt-BR/)):

- **CORREÇÃO** (1.0.**1**): conserta um erro sem mudar o que o painel faz;
- **MENOR** (1.**1**.0): função nova que não quebra nada;
- **MAIOR** (**2**.0.0): mudança que quebra algo (ex.: formato novo do arquivo do posto).

Cada parte nasce numa branch, entra na `main` por **pull request** e recebe uma **tag** com a versão. A versão fica em `Directory.Build.props` e aparece no título da janela. O conteúdo de cada versão está no [`CHANGELOG.md`](CHANGELOG.md).

| Versão | Branch | Pull request | O que entra |
| --- | --- | --- | --- |
| v0.1.0 | `feature/dominio-busca-ativa` | Domínio e testes do núcleo | Classes de domínio, regra da busca ativa, testes e CI |
| v0.2.0 | `feature/arquivo-criptografado` | Arquivo do posto criptografado | `CofreDeArquivo` e `SessaoDoPosto` |
| v0.3.0 | `feature/integracao-api` | Cliente da API Oásis | `ClienteApiOasis` e testes de integração |
| v0.4.0 | `feature/telas-windows-forms` | Telas em Windows Forms | Tela de senha e as quatro abas |
| v1.0.0 | `release/1.0.0` | Versão de entrega | Versão 1.0.0 |
| v1.1.0 | `feature/criterios-de-pontuacao` | Critérios de pontuação com herança e polimorfismo | `CriterioDePontuacao` e subclasses |

Cada branch parte da anterior, então os pull requests são mesclados **nessa ordem**, com o botão padrão **Merge pull request** (merge commit). A cada push e pull request, o GitHub Actions compila a solução e roda os testes do núcleo (aba **Actions**).

As evidências da entrega são os pull requests mesclados (aba **Pull requests → Closed**), a lista de tags e o CI verde.
