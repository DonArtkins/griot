# Coverage Gate

- Threshold: **>=80% line coverage**, enforced in CI via `dotnet test --collect:"XPlat Code Coverage"` + jest/simple coverage + flutter coverage.
- Weighting: service-layer + auth FIRST; presentation second. A flat aggregate pass while auth is uncovered is a failure.
- Enforced: in `.github/workflows/ci-cd.yml` (infra feature 05) as a blocking job with the coverage report artifact.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
