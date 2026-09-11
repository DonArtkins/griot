# Report Samples and Griot Template Mapping

**Inspected:** 2026-09-11. These four local DOCX documents supply layout and required sections, not production facts or automatic authorization. Extracted locally with Python 3 standard-library `zipfile`/`xml.etree.ElementTree`; no document contents were sent to an external service. The bootcamp PDF was read locally with Poppler `pdftotext`.

| Sample | Observed structure | Griot source/owner |
|---|---|---|
| `CAB_Deployment_Request_ESS_User_Creation.docx` | Header metadata; Purpose; Description of Change; Testing Summary; Risk Assessment; Rollback Plan; Requested Deployment Window; CAB Review and Approval table | backend 28 readiness/test evidence, ai 07 CAB template |
| `ESS_Post_Deployment_Report.docx` | Metadata/reference document; 5 checks, 4 pass, 1 critical; Introduction; Scope; Findings; Impact/Risk; Recommended Actions; Testing Challenges/Limitations; Deployment Status; Sign-Off | backend 28 deployment/test evidence plus permitted backend 20 telemetry; post-deployment template |
| `Finsights_Post_Deployment_Report (1).docx` | Metadata; 5 findings = 3 critical + 1 high + 1 medium; Introduction; Scope; Findings; Impact/Risk; Recommended Actions; Deployment Status; Sign-Off withheld while critical findings remain | severity/result totals from evidence rows; optional limitations section; post-deployment template |
| `ESS_User_Creation_Regression_Report.docx` | Metadata/reference CAB; 3 cases, 3 pass; Introduction; Scope; Test Results; Summary; Deployment Status; Sign-Off | baseline + retest evidence keyed by stable case IDs; regression template |

## Rules extracted from the samples

- Preserve the header metadata, numbered sections, tables, result/severity badges and human sign-off area. Populate people, dates and environments from authorized current evidence, never copy the sample stakeholders into Griot output.
- Counts come from rows: ESS 5/4/1 and Finsights 5/3/1/1 are fixture checks, not model-calculated prose. Distinguish pass/fail result from finding severity; do not conflate a test count with a finding count.
- Risk/impact narrative may explain evidence and explicitly label hypotheses. It must not invent a root cause, approval, reviewer signature, test execution or successful deployment.
- Samples describe emailing login credentials. That is source content, not a Griot security requirement; Griot continues its own human-only auth/OTP contract and never generates or distributes passwords in reports.
- QA/Test and Sprint/Status layouts are derived templates because no standalone sample of those types was supplied. Keep the common metadata, source window, scope, results, limitations and sign-off structure.
- Report availability is based on explicit readiness/deployment/test evidence (backend 28), not a new project phase enum or a generic Done task. The report-options endpoint and generation route enforce the same prerequisites.
- Role-check reminders are a confirmed in-app/optional-email notice (backend 27), with recipient count and scope preview. They do not use report storage or change roles.

## Privacy and fixture use

Use anonymized fixture copies of sections/counts for tests. Original samples remain local reference inputs and are not uploaded, published, committed as test seeds or fed wholesale into prompts. Source files can be distributed separately by their owner if desired.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
