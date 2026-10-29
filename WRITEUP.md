\# Design Write-up



\## 1. AI vs. Rules



AI extracts proposal details and assesses team/approach quality. C# handles mandatory checks, scoring formulas, weights, and ranking because these tasks require consistent, predictable rules.



\## 2. Consistency and Explainability



Fixed formulas and validated inputs make scoring repeatable. Each result includes the score, ranking, eligibility status, and reasons. 



\## 3. Human in the Loop



A reviewer checks flagged proposals, missing information, and the final ranking before awarding a contract. The system should log extracted data, scores, criteria versions, overrides, reviewer decisions, and timestamps.



\## 4. Changing Criteria



Criteria and weights are stored in configuration. Weights must total 100%. Changes create a new configuration version, and affected proposals must be rescored using the new version to ensure fair comparison.



\## 5. Risks and Safeguards



\- \*\*Incorrect extraction:\*\* Validate data and flag missing or contradictory information.

\- \*\*Made-up facts:\*\* Require evidence from proposals and never assume missing values.

\- \*\*Bias or inconsistent AI scores:\*\* Use defined scoring guidance and human review.

\- \*\*Prompt injection:\*\* Treat proposal content as untrusted data and ignore embedded instructions.

\- \*\*Cost and reliability:\*\* Use AI only where needed and keep eligibility checks and calculations in C#.

