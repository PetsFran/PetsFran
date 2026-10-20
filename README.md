# PetsFran

Monorepositório do sistema de agendamentos da PetsFran. O frontend Angular e a API ASP.NET Core são desenvolvidos no mesmo repositório e continuam sendo serviços independentes na execução e no deploy.

## Estrutura

| Caminho | Responsabilidade |
| --- | --- |
| `frontend/` | Aplicação Angular 16, incluindo `package-lock.json` e configuração do Vercel |
| `PetsFran.API/` | API ASP.NET Core 10, autenticação, regras de agendamento e migrações |
| `PetsFran.sln` | Solução .NET existente |
| `Dockerfile` | Imagem da API usada no Railway |

O código em `frontend/` foi trazido do repositório `PetsFranAgendamento`. O repositório original continua disponível; novas mudanças no frontend devem ser feitas aqui para manter as duas partes sincronizadas.

## Desenvolvimento local

### Docker Compose

Na raiz do repositório, execute `docker compose up --build`. O comando sobe PostgreSQL, API e Angular com suas dependências. Acesse `http://localhost:4200`; a API também fica disponível em `http://localhost:5028`. As migrações do Entity Framework são aplicadas pela API na inicialização. Para encerrar, execute `docker compose down`. Os dados do PostgreSQL ficam no volume `postgres_data`.

`POSTGRES_PASSWORD` e `JWT_SECRET` podem ser definidos no ambiente antes da execução. Os valores padrão do Compose servem apenas ao desenvolvimento local. O frontend do Compose usa `/api` através do Nginx; o deploy Vercel continua com a configuração de produção existente.

Para consultar o banco pelo DataGrip (ou outra ferramenta PostgreSQL), crie uma conexão com host `127.0.0.1`, porta `55433`, banco `petsfran`, usuário `petsfran` e senha `petsfran_local`. A porta pode ser alterada com `POSTGRES_PORT` e a senha com `POSTGRES_PASSWORD` antes de subir o Compose. A porta fica acessível somente neste computador. No console SQL, por exemplo, execute `SELECT * FROM "PetShops" LIMIT 20;`. Para usar o terminal: `docker compose exec db psql -U petsfran -d petsfran`.

### Execução sem Docker

Requisitos: .NET SDK 10, Node.js compatível com Angular 16, npm e PostgreSQL.

1. Configure a API com variáveis de ambiente ou com o arquivo local ignorado `PetsFran.API/appsettings.Development.json`. A aplicação lê `ConnectionStrings__DefaultConnection` e `JwtSettings__Secret`, `JwtSettings__Issuer` e `JwtSettings__Audience`. O banco deve estar acessível para a migração automática na inicialização.
2. Em um terminal na raiz, execute `dotnet run --project PetsFran.API --launch-profile http`. A API usa `http://localhost:5028` nesse perfil.
3. Em outro terminal, execute `npm ci --prefix frontend` e `npm run start --prefix frontend`. Abra `http://localhost:4200`.

O ambiente de desenvolvimento Angular aponta para `http://localhost:5028/api` em `frontend/src/environments/environment.ts`. A API já permite a origem `http://localhost:4200` em desenvolvimento. Para usar outra porta ou domínio, atualize as configurações correspondentes dos dois lados.

## Verificação

```bash
dotnet build PetsFran.sln
npm run build --prefix frontend
```

O build de produção Angular usa `frontend/src/environments/environment.prod.ts`. Ele continua apontando para a API publicada no Railway; a unificação dos repositórios não troca automaticamente a URL de produção.

## Deploy

| Serviço | Configuração no mesmo repositório |
| --- | --- |
| Railway (API) | Diretório raiz `/`, Dockerfile `Dockerfile`. Configure `DATABASE_URL` ou `ConnectionStrings__DefaultConnection` e as chaves `JwtSettings__*` no serviço. |
| Vercel (Angular) | Root Directory `frontend`, comando de instalação `npm ci`, comando de build `npm run build`, Output Directory `dist/PetsFranAgendamento`. |

Após mudar o repositório conectado ao Vercel, confirme a URL da API em `environment.prod.ts`. Se o domínio do frontend mudar, inclua a nova origem na política de CORS da API. O arquivo `frontend/vercel.json` encaminha rotas da aplicação Angular para `index.html`.

O histórico anterior do frontend permanece no repositório `PetsFranAgendamento`; esta importação copia o estado do código, sem reescrever o histórico Git da API.
