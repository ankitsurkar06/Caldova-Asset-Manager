# Output Format: SQL SKU Recommendations

Use this complete template only after generating verified SKU recommendations
from source configuration or completed performance data. Extract target, compute, storage and cost
from actual recommendation results. Include generation time. Use "Unavailable"
for fields absent from the evidence, never invented values.

The offline caller retains its full readiness template separately. Do not
reassess readiness, replace its findings or infer compatibility from SKU sizing.
There are no collection-started, progress or collection-complete templates.

## Skill Response Template

```text
SKILL: recommend-sql-sku-sizing  |  {instanceName}
======================================================================

SKU Recommendation
------------------
  SQL Server Instance:   {instanceName}
  Generated at (UTC):    {actual generation time}
  Sizing criterion:     {ConfigBased or PerformanceBased from validated execution}
  Sizing evidence:      {source configuration or actual selected performance data folder}

Recommended Target
------------------
  {actual recommended target and vCores}
  Compute configuration: {actual service tier, hardware generation or VM SKU}
  Storage configuration: {actual storage size and IOPS}
  Cost: {actual estimated monthly cost and selected cost option, or Unavailable}

REPORT
------
  {actual generated native SKU HTML path, or JSON path if no HTML exists}
```

Preserve all recommendations and their actual compute, storage and cost details
when the native result contains multiple target options. Do not reduce the
results to a link-only placeholder.

Configuration-based estimates use source capacity, not observed workload
utilization. Preserve native no-recommendation outcomes and reasons for any
target without inventing compute or prices.
