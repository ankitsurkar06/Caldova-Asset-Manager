# Recommendation configuration

Task008 externalizes the existing scoring URL on the actual **net472/MVC5**
runtime. This is **not** a .NET10 port or a new recommendation service. The
existing host-setting resolver is reused; no configuration service or package
was added. The future runtime port must retain these override/validation contracts.

| Public Web.config setting | Host environment override | Default |
|---|---|---|
| `MachineLearning.ScoringEndpoint` | `CALDOVA_MACHINELEARNING_SCORINGENDPOINT` | Existing legacy HTTPS scoring URL |
| `MachineLearning.ModelName` | `CALDOVA_MACHINELEARNING_MODELNAME` | `sampletest` (unchanged) |
| `ShowRecommendations` | `CALDOVA_SHOWRECOMMENDATIONS` | `false` (unchanged) |

The resolver reads the environment first, then `appSettings`. An explicitly
empty environment variable, if supplied by the host, does **not** fall back
to the file. Hosts that remove empty variables instead of retaining them must
use a whitespace value to test invalid/blank overrides. Recycle the application
after changing host settings so existing eagerly constructed clients are refreshed.
No `.env` loading, appsettings.json or alternate configuration service is implied.

## Required values and request contract

On a recommendation request, endpoint and model must be nonblank. The endpoint
must be an absolute HTTPS URL without embedded credentials, query or fragment;
it includes the existing scoring path, not a query template. Validation errors
name the invalid setting but do not include its value. Invalid configuration
fails before HTTP; it is not reported as a successful empty recommendation.

The configured endpoint is followed by
`?Id=%27{model}%27&Item=%27{productId}%27`. The parameter names, ordering and
quoted string semantics are unchanged. Values are URI-escaped; apostrophes
inside values are doubled as OData string literals. Ordinary model/product IDs
produce exactly the old URL. Changing the endpoint does not adapt the protocol:
any externally supplied replacement must support the existing request,
authentication and JSON response contract.

For example, an offline placeholder `https://scoring.example.invalid/v1/Score`
with model `catalog` and product `42` yields
`https://scoring.example.invalid/v1/Score?Id=%27catalog%27&Item=%2742%27`.
This is an illustration, not an endpoint to deploy or a new service.

## Feature gating and secrets

Recommendations remain disabled by default. Existing controller gating and
credential-client gating are unchanged. Endpoint/model validation is lazy,
not at Unity construction, so disabled recommendations require neither these
values nor Key Vault access.

Keep account credentials in the existing
`MachineLearning.AccountKeySecretName` / Key Vault managed-identity path described
in [KEY-VAULT-SECRETS.md](KEY-VAULT-SECRETS.md). Do not put credentials, API keys
or signed URLs in the public scoring endpoint/model settings.

## Acceptance limits

Local fake-client checks do not establish that the legacy endpoint is available.
No real scoring request, Key Vault call, provisioning or deployment was performed.
If the service is unavailable, an external owner must provide a compatible
replacement contract; no replacement was invented. Existing HTTP transport/error
behavior (including its catch-and-empty behavior) is unchanged and remains the
separate task009 responsibility. Upstream .NET10/Blob/Redis work is incomplete;
overall modernization/task008 dependency acceptance remains pending.
