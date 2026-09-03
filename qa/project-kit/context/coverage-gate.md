# Coverage Gate

- Threshold: **>=80% line coverage**, enforced in CI via `dotnet test --collect:"XPlat Code Coverage"` + jest/simple coverage + flutter coverage.
- Weighting: service-layer + auth FIRST; presentation second. A flat aggregate pass while auth is uncovered is a failure.
- Enforced: in `.github/workflows/ci-cd.yml` (infra feature 05) as a blocking job with the coverage report artifact.
