# Notes

## Prerequisites

### Local development
- .NET 10 SDK
- Docker Desktop (with WSL 2 on Windows)
- Visual Studio 2026 or any editor (optional)

### AWS
- An AWS account and the AWS CLI (`aws configure`)
- Infrastructure already deployed by the [`banking-infra`](https://github.com/HninPhyuPhyuAung-hub/banking-infra) Terraform repository — VPCs, ECR repositories, RDS, ECS clusters/services, ALBs, Route 53 and the private CA. Make sure that stack has already been applied before continuing here.
- An IAM role that GitHub Actions can assume through OIDC (also provisioned by `banking-infra`'s bootstrap — see below)

## One-time AWS setup

ECR repositories, the OIDC identity provider, RDS, ECS clusters/services and networking are all provisioned by [`banking-infra`](https://github.com/HninPhyuPhyuAung-hub/banking-infra); this repository does not create any of that. Deploy that stack first, then:

1. The app ECR-push role is already provisioned by the infrastructure bootstrap (see `banking-infra/infra/s3/notes.md`). Set the app repository's `AWS_ROLE_ARN` secret to `arn:aws:iam::<account-id>:role/github-actions-banking-app-ecr-push`. The role trust must allow audience `sts.amazonaws.com` and subject `repo:HninPhyuPhyuAung-hub/banking-app:ref:refs/heads/main`. This workflow only assumes it from `main`; pull-request runs do not assume it.
2. The existing role permits ECR login and image push to `banking-api` and `banking-dashboard`. The `deploy` job in this workflow reuses the same role for ECS rollout, so its policy must also be extended (manually, in IAM — this role is bootstrap-managed, not Terraform-owned) with a statement scoped to just the two application services:
   ```json
   {
     "Effect": "Allow",
     "Action": ["ecs:UpdateService", "ecs:DescribeServices"],
     "Resource": [
       "arn:aws:ecs:ap-southeast-1:<account-id>:service/backend-cluster/banking-api",
       "arn:aws:ecs:ap-southeast-1:<account-id>:service/frontend-cluster/banking-dashboard"
     ]
   }
   ```
   It still grants no Terraform state access or repository management.

## GitHub repository settings

**Secrets** (Settings > Secrets and variables > Actions > Secrets)

| Name | Value |
|---|---|
| `AWS_ROLE_ARN` | `arn:aws:iam::<account-id>:role/github-actions-banking-app-ecr-push` |

**Variables** (same page, Variables tab)

| Name | Value | Required |
|---|---|---|
| `AWS_REGION` | for example `ap-southeast-1` | Yes |
| `ECR_API_REPOSITORY` | ECR repository name of the API, for example `banking-api` | Yes |
| `ECR_WEB_REPOSITORY` | ECR repository name of the UI, `banking-dashboard` | Yes |
| `ECS_API_CLUSTER` | ECS cluster name of the API, `backend-cluster` | Yes |
| `ECS_API_SERVICE` | ECS service name of the API, `banking-api` | Yes |
| `ECS_WEB_CLUSTER` | ECS cluster name of the UI, `frontend-cluster` | Yes |
| `ECS_WEB_SERVICE` | ECS service name of the UI, `banking-dashboard` | Yes |

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

## Troubleshooting

| Problem | Likely cause |
|---|---|
| `No suitable constructor was found for the type 'Account'` | The private parameterless constructor on `Account` is missing |
| Transient failure on startup | Database is not running or the connection string is wrong |
| UI says it cannot reach the API | `BankingApi__BaseUrl` is wrong or the security group blocks port 8080 |
| ECS tasks keep restarting | Check the CloudWatch log group of the task and the `/health` target group check |
| GitHub Action fails at AWS login | Role trust policy does not match the repository or branch |
| `Not authorized to perform sts:AssumeRoleWithWebIdentity` | Check that the app repository secret `AWS_ROLE_ARN` exactly matches the app ECR-push role ARN above. In IAM, verify the role still trusts provider `token.actions.githubusercontent.com` with audience `sts.amazonaws.com` and subject `repo:HninPhyuPhyuAung-hub/banking-app:ref:refs/heads/main` |

## Variables reference

### GitHub Actions (repository settings)

| Name | Type | Example | Used by |
|---|---|---|---|
| `AWS_ROLE_ARN` | Secret | `arn:aws:iam::<account-id>:role/github-actions-banking-app-ecr-push` | Login to AWS |
| `AWS_REGION` | Variable | `ap-southeast-1` | Login to AWS, ECR |
| `ECR_API_REPOSITORY` | Variable | `banking-api` | API image push |
| `ECR_WEB_REPOSITORY` | Variable | `banking-dashboard` | UI image push |
| `ECS_API_CLUSTER` | Variable | `backend-cluster` | API deploy job |
| `ECS_API_SERVICE` | Variable | `banking-api` | API deploy job |
| `ECS_WEB_CLUSTER` | Variable | `frontend-cluster` | UI deploy job |
| `ECS_WEB_SERVICE` | Variable | `banking-dashboard` | UI deploy job |

Use a **secret** for anything sensitive. Names, regions and repository names are not sensitive, so they are plain **variables**.

### Application environment variables (ECS task definitions)

| Name | Task | Example | Notes |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | API | `Host=<rds-endpoint>;Port=5432;Database=banking;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true` | Store in Secrets Manager and reference it under `secrets` |
| `BankingApi__BaseUrl` | UI | `http://banking-api:8080/` | Address of the API as seen from the UI task |
| `ASPNETCORE_ENVIRONMENT` | Both | `Production` | Optional |

The double underscore `__` in an environment variable name becomes `:` in .NET configuration, so `ConnectionStrings__DefaultConnection` overrides `ConnectionStrings:DefaultConnection`.
