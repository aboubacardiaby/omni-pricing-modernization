# Project dependency direction

Production dependencies point inward:

```text
Pricing.Api -> Pricing.Application, Pricing.Infrastructure, Pricing.Compatibility
Pricing.Infrastructure -> Pricing.Application, Pricing.Domain
Pricing.Compatibility -> Pricing.Application, Pricing.Domain
Pricing.Application -> Pricing.Domain
Pricing.Domain -> no other project
```

`Directory.Build.targets` rejects any direct reference from `Pricing.Domain` to
`Pricing.Api` or `Pricing.Infrastructure`. Project references and solution builds
enforce the remaining declared graph. Test projects may reference only the
production projects needed by their test scope.
