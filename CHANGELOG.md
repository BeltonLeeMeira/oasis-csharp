# Histórico de versões

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/). As versões seguem o [Versionamento Semântico](https://semver.org/lang/pt-BR/): MAIOR.MENOR.CORREÇÃO.

## [1.1.0] - 2026-11-15

### Alterado
- A pontuação da busca ativa passou a ser calculada por critérios: a classe base `CriterioDePontuacao`, a intermediária `CriterioSimOuNao` e quatro critérios concretos (mora sozinha, condição de atenção, idade e sem contato recente). O resultado para o usuário é o mesmo.

### Adicionado
- Testes dos critérios e da pontuação máxima.

## [1.0.0] - 2026-11-15

### Alterado
- Versão estável para a entrega da Global Solution: todas as funções do painel reunidas e testadas.

## [0.4.0] - 2026-11-15

### Adicionado
- Aplicação Windows Forms: tela de senha (espera de 30 segundos depois de 5 erros) e janela principal com quatro abas.
- Aba Hoje: lista da busca ativa (Ligar hoje, Já contatada, Opcional) e registro de ligação.
- Aba Pessoas acompanhadas: cadastro, edição e remoção, com validação.
- Aba Pontos para aprovar: aprova ou rejeita os pontos sugeridos pelo app.
- Aba Configurações: endereço da API, chave do posto e teste de conexão.

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
