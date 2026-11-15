# Histórico de versões

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/). As versões seguem o [Versionamento Semântico](https://semver.org/lang/pt-BR/): MAIOR.MENOR.CORREÇÃO.

## [0.3.0] - 2026-11-15

### Adicionado
- `ClienteApiOasis`: nível de calor das estações, previsão de amanhã e pontos pendentes da API Oásis.
- Mensagens de erro claras quando a API está fora do ar ou a chave do posto está errada.
- Testes de integração, que são pulados quando a API não está no ar.

## [0.2.0] - 2026-11-15

### Adicionado
- `CofreDeArquivo`: arquivo do posto criptografado (AES-256-GCM, chave derivada da senha com PBKDF2).
- `SessaoDoPosto`: abre, salva e altera os dados do posto, com validação do endereço da API.
- Testes de persistência: senha errada, arquivo alterado e senha curta.

## [0.1.0] - 2026-11-15

### Adicionado
- Domínio: `PessoaAcompanhada`, `RegistroContato`, `ValidadorPessoa` e `BuscaAtiva` (pontuação de 0 a 7 e lista do dia por nível de calor).
- Projeto de testes do núcleo, sem pacotes externos.
- Integração contínua: build e testes a cada push e pull request.
