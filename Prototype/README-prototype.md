# Proposal Triage Prototype

This is a small C# console prototype for the assessment. It reads `proposals.json` and `scoring-config.json`, applies mandatory checks and configurable scoring rules, then writes `ranking.json`.

## Important before running

- Fill missing fields in `proposals.json` from the actual proposal evidence / your AI extraction output.
- `ActiveQualifyingInstallationYears` means the number of distinct years in the last five-year window with qualifying commercial installations of at least 100 kW. It does **not** mean years in business.
- `LargestQualifyingInstallationKw` must come from evidence. Do not guess missing capacities.
- `TeamApproachAiScore` is the AI's score from 0 to 100 for team and approach. Keep the AI's evidence/reasoning available for the reviewer as well.
- In this prototype, a known failed mandatory requirement makes a proposal `ineligible`; an unknown mandatory check or missing scoring data makes it `flagged`.
- Over-budget and over-deadline proposals are flagged for human review rather than automatically marked ineligible, because the brief does not explicitly say they are mandatory pass/fail requirements. Change this only if your documented policy says otherwise.
- Rankings are calculated only for proposals with complete scoring data that pass mandatory checks and are within budget and timeline.
- This is a prototype, not a production application. It does not call an AI API; AI-extracted fields and the AI team/approach score are supplied in the JSON input.

## Run

Create a C# Console App in Visual Studio, replace its `Program.cs` with the provided file, and put `proposals.json` and `scoring-config.json` in the same working directory. Run the project. The program writes `ranking.json`.
