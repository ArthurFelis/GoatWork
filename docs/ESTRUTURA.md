# Estrutura do projeto GoatWork

```
GoatWork/
├── backend/                  ← solução .NET (abrir GoatWork.sln no Visual Studio)
│   ├── src/
│   │   ├── GoatWork.Api/            Controllers, Program.cs, appsettings.json, Middlewares
│   │   ├── GoatWork.Application/    Services (regras de negócio), DTOs, Validators, Interfaces
│   │   ├── GoatWork.Domain/         Entities (18 tabelas), Enums (ENUMs do banco), Exceptions
│   │   └── GoatWork.Infrastructure/ AppDbContext, Repositories, IA, gateway de pagamento, JWT/hash
│   └── tests/GoatWork.Tests/
├── frontend/                 ← HTML + CSS + JS puro
│   ├── index.html                   Home
│   ├── pages/                       1 .html por tela do Figma
│   ├── css/  base/ components/ pages/
│   ├── js/   api/ components/ pages/ utils/
│   └── assets/ images/ icons/ fonts/
├── database/
│   ├── GoatWork.sql                 script completo (MySQL 8)
│   └── scripts/                     seeds e ajustes futuros (ex.: 02-seed-categorias.sql)
└── docs/                     ← MER, diagramas, este arquivo
```

## Dependência entre as camadas (regra de ouro)

`Api → Application → Domain` e `Infrastructure → Application + Domain`.
O **Domain não depende de ninguém**. Controllers só chamam Services; Services usam interfaces; quem implementa
as interfaces (banco, IA, pagamento) é a Infrastructure.

## Criando os projetos no Visual Studio / terminal

Na pasta `backend/`:

```bash
dotnet new sln -n GoatWork
dotnet new webapi    -n GoatWork.Api            -o src/GoatWork.Api            --force
dotnet new classlib  -n GoatWork.Application    -o src/GoatWork.Application    --force
dotnet new classlib  -n GoatWork.Domain         -o src/GoatWork.Domain         --force
dotnet new classlib  -n GoatWork.Infrastructure -o src/GoatWork.Infrastructure --force
dotnet new xunit     -n GoatWork.Tests          -o tests/GoatWork.Tests        --force
dotnet sln add src/*/*.csproj tests/*/*.csproj

dotnet add src/GoatWork.Api reference src/GoatWork.Application src/GoatWork.Infrastructure
dotnet add src/GoatWork.Application reference src/GoatWork.Domain
dotnet add src/GoatWork.Infrastructure reference src/GoatWork.Application src/GoatWork.Domain
dotnet add tests/GoatWork.Tests reference src/GoatWork.Application

dotnet add src/GoatWork.Infrastructure package Pomelo.EntityFrameworkCore.MySql
dotnet add src/GoatWork.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/GoatWork.Infrastructure package BCrypt.Net-Next
dotnet add src/GoatWork.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

> `--force` mantém os arquivos que já estão nas pastas e só cria o que falta. Se o template sobrescrever algo, restaure pelo git.

Como o banco já existe em SQL, use **Database First**: `dotnet ef dbcontext scaffold` ou escreva as entidades à mão
a partir de `database/GoatWork.sql`.

## Mapa: tela do Figma → front-end → back-end → tabelas

| # | Tela | Arquivo front | Controller | Tabelas |
|---|---|---|---|---|
| 1 | Home (landing) | `index.html` | Trabalhadores (top avaliados) | trabalhador, usuario |
| 1 | Entrar / Cadastro (cliente e trabalhador) + Termos | `pages/entrar.html`, `cadastro.html`, `termos.html` | Auth, Termos | usuario, cliente, trabalhador, termo_uso, aceite_termos |
| 2 | Perfil do trabalhador | `perfil-trabalhador.html` | Trabalhadores | trabalhador, atuacao, categoria |
| – | Encontre profissionais | `profissionais.html` | Trabalhadores, Categorias | trabalhador, atuacao, categoria |
| 3 | Novo pedido | `novo-pedido.html` | Pedidos | pedido, categoria |
| 4 | Conversa com a IA | `chat-ia.html` | Briefings | mensagem_ia |
| 5 | Revisão do briefing | `revisao-briefing.html` | Briefings | briefing |
| 6 | Convites recebidos | `convites.html` | Convites | convite, pedido |
| 7 | Chat interno | `chat.html` | Chats | chat, mensagem |
| 8 | Contrato | `contrato.html` | Contratos | contrato |
| 9 | Pagamento | `pagamento.html` | Pagamentos | pagamento |
| 10 | Entrega | `entrega.html` | Entregas | entrega |
| 11 | Disputa | `disputa.html` | Disputas | disputa |
| 12 | Avaliação | `avaliacao.html` | Avaliacoes | avaliacao |
| 13 | Painel do admin | `admin.html` | Admin | disputa, contrato, mensagem |

## Ordem sugerida de construção (segue as 5 fases)

1. **Fase 1:** banco rodando → Auth (cadastro/login com hash + JWT) → aceite dos termos → telas Entrar/Cadastro → Encontre profissionais.
2. **Fase 2:** Pedido + integração com a IA → briefing e revisão.
3. **Fase 3:** Convites + Chat (com o filtro de contato externo).
4. **Fase 4:** Contrato → Pagamento (começar com gateway simulado) → Entrega.
5. **Fase 5:** Disputa + Avaliação + painel admin.
