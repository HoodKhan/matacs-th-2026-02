# AI Proposal Triage Design

**Full Name:** Hood Ilyas  
**Reference:** MATACS-TH-2026-02  
**Time Spent:** 6 hours

## Repository Files

- [Process Flow Diagram](Diagram/process-flow%20diagram.drawio.png)
- [Design Write-up](WRITEUP.md)
- [AI Prompts](PROMPTS.md)
- [Proposal Ranking](ranking.json)
- [C# Prototype](Prototype/Program.cs)
- [Proposal Data](Prototype/proposals.json)
- [Scoring Configuration](Prototype/scoring-config.json)
- [Prototype Instructions](Prototype/README-prototype.md)

## AI Tool Usage

I used ChatGPT to develop the C# prototype, work on `ranking.json` and `PROMPTS.md`, and refine my scoring logic. One point where I disagreed with ChatGPT was its suggestion to use fixed bands for scoring relevant experience. I chose relative scoring instead because it compares proposals directly against the strongest result in each factor.

Relevant experience has three factors: qualifying installations, installation size in kW, and years of experience. Fixed bands can give different proposals the same score when they fall within the same range. With relative scoring, I compare each proposal against the highest value for each factor using the formula `(proposal value / highest value) × 100`.

I assigned weights of 40% to qualifying installations, 40% to installation size, and 20% to years of experience. I then calculate the weighted average of these relative scores and apply the configured 30% weight for the overall experience criterion. This makes the scoring more consistent and allows proposals to be compared using the same formula.
