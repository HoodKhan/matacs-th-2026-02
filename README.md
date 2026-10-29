\# AI Proposal Triage Design



\*\*Full Name:\*\* Hood Ilyas

\*\*Reference:\*\* MATACS-TH-2026-02

\*\*Time Spent:\*\* 6 hours



\## Repository Files



\- \[Process Flow Diagram](diagram/)

\- \[Design Write-up](WRITEUP.md)

\- \[AI Prompts](PROMPTS.md)

\- \[Proposal Ranking](ranking.json)

\- \[C# Prototype](prototype/Program.cs)

\- \[Proposal Data](prototype/proposals.json)

\- \[Scoring Configuration](prototype/scoring-config.json)

\- \[Prototype Instructions](prototype/README-prototype.md)



\## AI Tool Usage



I used ChatGPT to develop the C# prototype and help with ranking.JSON and Prompts, and tried to polish my logic. There was one moment where chatgpt was more aligned towards using Fixed band approach for scoring the relevant experience which i disagreed with and instead i came up with relative scoring system for the problem because relative scoring method was more consistent than fixed band. \*\*Here is how:\*\* Relevant experience had three factors Qualifying installations, Size of installations in KW, and experience in number of years. with fixed bands we could reach problems like, more proposals having the same score due to lying in that specific range. But with Relative Scoring i compared every point with the highest in the list e.g. (No. installation / Highest no. of Installation)\* 100, for all three factors separately than i gave weighted score for every factor based on priority e.g. Qualifying installations = 40%, Size of installations in KW = 40%, experience in no. of years = 20%. and finally put them in below formula to come up with an efficient and consistent score, The formula is; Score = (Relative scoring \* Weighted score)+(Relative scoring \* Weighted score)+(Relative scoring \*Weighted score) fpr the three factors and you apply this formula for all proposals and convert to configured scoring criteria e.g. 30%.

