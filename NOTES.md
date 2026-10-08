# Notes

## Prerequisites

### Local development
- .NET 10 SDK
- Docker Desktop (with WSL 2 on Windows)
- Visual Studio 2026 or any editor (optional)

### AWS
- An AWS account and the AWS CLI (`aws configure`)
- Two ECR repositories: `banking-api` and `banking-web`
- A PostgreSQL database (for example RDS) reachable from the ECS tasks
- An ECS cluster with a service for the API (and one for the UI, optional)
- An IAM role that GitHub Actions can assume through OIDC

## One-time AWS setup

1. Create the ECR repositories:
   ```
   aws ecr create-repository --repository-name banking-api --region <region>
   aws ecr create-repository --repository-name banking-web --region <region>
   ```
2. Add GitHub as an OIDC identity provider in IAM (`token.actions.githubusercontent.com`, audience `sts.amazonaws.com`).
3. Create an IAM role trusted by your repository (`repo:<owner>/<repo>:ref:refs/heads/main`) with permissions for:
   - ECR push: `ecr:GetAuthorizationToken`, `ecr:BatchCheckLayerAvailability`, `ecr:InitiateLayerUpload`, `ecr:UploadLayerPart`, `ecr:CompleteLayerUpload`, `ecr:PutImage`
   - ECS deploy: `ecs:UpdateService`, `ecs:DescribeServices`

## GitHub repository settings

**Secrets** (Settings > Secrets and variables > Actions > Secrets)

| Name | Value |
|---|---|
| `AWS_ROLE_ARN` | ARN of the IAM role above |

**Variables** (same page, Variables tab)

| Name | Value | Required |
|---|---|---|
| `AWS_REGION` | for example `ap-southeast-1` | Yes |
| `ECR_API_REPOSITORY` | ECR repository name of the API, for example `banking-api` | Yes |
| `ECR_WEB_REPOSITORY` | ECR repository name of the UI, for example `banking-web` | Yes |
| `ECS_CLUSTER` | ECS cluster name | When the deploy job is enabled |
| `ECS_API_SERVICE` | ECS service name of the API | With `ECS_CLUSTER` |
| `ECS_WEB_SERVICE` | ECS service name of the UI | Optional |

## ECS task configuration

**API task**
- Image: `<account-id>.dkr.ecr.<region>.amazonaws.com/banking-api:latest`
- Container port: `8080`
- Environment `ConnectionStrings__DefaultConnection`: use a Secrets Manager secret via the task definition `secrets` block. The execution role needs `secretsmanager:GetSecretValue`.
- Load balancer health check path: `/health`

**UI task**
- Image: `.../banking-web:latest`, container port `8080`
- Environment `BankingApi__BaseUrl`: address the UI task can reach, for example `http://banking-api:8080/` with ECS Service Connect, or an internal load balancer DNS name. The browser never calls the API directly.

**Networking**
- Load balancer in public subnets, tasks in private subnets
- Security groups: load balancer to tasks on 8080, tasks to database on 5432, UI tasks to API tasks on 8080
- RDS: encrypted storage, not publicly accessible, TLS enforced (`SSL Mode=Require`)

## Good to know

- The API creates its tables on startup with `EnsureCreated()`. It does not handle later schema changes; use EF Core migrations if the schema will change.
- The deploy job uses `--force-new-deployment`, so the ECS task definition must reference the `latest` tag.
- .NET container images listen on port 8080 by default.
- Swagger is enabled in every environment. Disable it for production if you do not want it public.
- Do not commit real connection strings or passwords. `appsettings.json` only contains a placeholder.
- Do not use "GIC" in the public repository name.

## Troubleshooting

| Problem | Likely cause |
|---|---|
| `No suitable constructor was found for the type 'Account'` | The private parameterless constructor on `Account` is missing |
| Transient failure on startup | Database is not running or the connection string is wrong |
| UI says it cannot reach the API | `BankingApi__BaseUrl` is wrong or the security group blocks port 8080 |
| ECS tasks keep restarting | Check the CloudWatch log group of the task and the `/health` target group check |
| GitHub Action fails at AWS login | Role trust policy does not match the repository or branch |

## Variables reference

### GitHub Actions (repository settings)

| Name | Type | Example | Used by |
|---|---|---|---|
| `AWS_ROLE_ARN` | Secret | `arn:aws:iam::123456789012:role/github-actions-ecr` | Login to AWS |
| `AWS_REGION` | Variable | `ap-southeast-1` | Login to AWS, ECR |
| `ECR_API_REPOSITORY` | Variable | `banking-api` | API image push |
| `ECR_WEB_REPOSITORY` | Variable | `banking-web` | UI image push |
| `ECS_CLUSTER` | Variable | `banking-cluster` | Deploy job (when enabled) |
| `ECS_API_SERVICE` | Variable | `banking-api-service` | Deploy job (when enabled) |
| `ECS_WEB_SERVICE` | Variable | `banking-web-service` | Deploy job (when enabled) |

Use a **secret** for anything sensitive. Names, regions and repository names are not sensitive, so they are plain **variables**.

### Application environment variables (ECS task definitions)

| Name | Task | Example | Notes |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | API | `Host=<rds-endpoint>;Port=5432;Database=banking;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true` | Store in Secrets Manager and reference it under `secrets` |
| `BankingApi__BaseUrl` | UI | `http://banking-api:8080/` | Address of the API as seen from the UI task |
| `ASPNETCORE_ENVIRONMENT` | Both | `Production` | Optional |

The double underscore `__` in an environment variable name becomes `:` in .NET configuration, so `ConnectionStrings__DefaultConnection` overrides `ConnectionStrings:DefaultConnection`.
