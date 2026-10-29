using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProposalTriage;

public sealed class ScoringConfig
{
    public decimal ExperienceWeight { get; set; } = 30m;
    public decimal PriceWeight { get; set; } = 25m;
    public decimal TimelineWeight { get; set; } = 15m;
    public decimal WarrantyWeight { get; set; } = 15m;
    public decimal TeamApproachWeight { get; set; } = 15m;

    // Subweights inside the overall Experience criterion.
    public decimal ExperienceCountWeight { get; set; } = 40m;
    public decimal ExperienceSizeWeight { get; set; } = 40m;
    public decimal ExperienceSpreadWeight { get; set; } = 20m;

    public decimal BudgetCad { get; set; } = 260000m;
    public int MaximumTimelineWeeks { get; set; } = 16;
    public int ExperienceWindowYears { get; set; } = 5;
}

public sealed class Proposal
{
    public string Proposal { get; set; } = "";
    public string Contractor { get; set; } = "";

    // Mandatory checks. false = known failure; null = not verified.
    public bool? OntarioElectricalLicenceHeldAndStated { get; set; }
    public bool? WsIbClearanceConfirmed { get; set; }
    public bool? LiabilityInsurance5MCadConfirmed { get; set; }

    public decimal? PriceCad { get; set; }
    public int? TimelineWeeks { get; set; }
    public int? PanelWarrantyYears { get; set; }
    public int? WorkmanshipWarrantyYears { get; set; }

    // Experience inputs must describe qualifying commercial installations
    // >=100 kW within the configured recent-year window.
    public int? QualifyingInstallations { get; set; }
    public decimal? LargestQualifyingInstallationKw { get; set; }

    // Number of distinct years in the assessment window in which the bidder
    // completed qualifying installations. Do not substitute "years in business".
    public int? ActiveQualifyingInstallationYears { get; set; }

    // Score supplied by the AI for Team & Approach, from 0 to 100.
    // The AI should provide evidence/reasons separately for the reviewer.
    public decimal? TeamApproachAiScore { get; set; }
}

public sealed class RankingResult
{
    public string Proposal { get; set; } = "";
    public int? Rank { get; set; }
    public string Status { get; set; } = "";
    public decimal? Score { get; set; }
    public string Reason { get; set; } = "";
}

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static int Main()
    {
        try
        {
            var config = ReadJson<ScoringConfig>("scoring-config.json");
            var proposals = ReadJson<List<Proposal>>("proposals.json");

            ValidateConfig(config);

            var results = ScoreProposals(proposals, config);
            var ranked = results
                .Where(r => r.Status == "ranked")
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Proposal, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;

            // Flagged/ineligible proposals remain in the output with null rank.
            var final = ranked
                .Concat(results.Where(r => r.Status != "ranked")
                    .OrderBy(r => r.Proposal, StringComparer.Ordinal))
                .OrderBy(r => r.Rank ?? int.MaxValue)
                .ThenBy(r => r.Proposal, StringComparer.Ordinal)
                .ToList();

            File.WriteAllText("ranking.json", JsonSerializer.Serialize(final, JsonOptions));
            Console.WriteLine($"Processed {proposals.Count} proposals. Output: ranking.json");
            foreach (var result in final)
                Console.WriteLine($"{result.Proposal}: {result.Status}, score={result.Score?.ToString("0.00") ?? "null"}, rank={result.Rank?.ToString() ?? "null"}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Required file '{path}' was not found in the program's working directory.");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidDataException($"Could not read valid JSON from '{path}'.");
    }

    private static void ValidateConfig(ScoringConfig c)
    {
        if (c.ExperienceWeight + c.PriceWeight + c.TimelineWeight +
            c.WarrantyWeight + c.TeamApproachWeight != 100m)
            throw new InvalidDataException("The five main criterion weights must total exactly 100%.");

        if (c.ExperienceCountWeight + c.ExperienceSizeWeight + c.ExperienceSpreadWeight != 100m)
            throw new InvalidDataException("Experience subweights must total exactly 100%.");

        if (new[] { c.ExperienceWeight, c.PriceWeight, c.TimelineWeight,
                    c.WarrantyWeight, c.TeamApproachWeight,
                    c.ExperienceCountWeight, c.ExperienceSizeWeight,
                    c.ExperienceSpreadWeight }.Any(w => w < 0m || w > 100m))
            throw new InvalidDataException("Weights must be between 0 and 100.");

        if (c.BudgetCad <= 0 || c.MaximumTimelineWeeks <= 0 || c.ExperienceWindowYears <= 0)
            throw new InvalidDataException("Budget, maximum timeline, and experience window must be positive.");
    }

    private static List<RankingResult> ScoreProposals(List<Proposal> proposals, ScoringConfig config)
    {
        var results = new List<RankingResult>();

        foreach (var p in proposals)
        {
            var mandatoryFailures = GetMandatoryFailures(p);
            if (mandatoryFailures.Count > 0)
            {
                results.Add(new RankingResult
                {
                    Proposal = p.Proposal,
                    Status = "ineligible",
                    Rank = null,
                    Score = null,
                    Reason = "Mandatory requirement failed: " + string.Join("; ", mandatoryFailures)
                });
                continue;
            }

            var missingMandatory = GetUnknownMandatoryChecks(p);
            if (missingMandatory.Count > 0)
            {
                results.Add(new RankingResult
                {
                    Proposal = p.Proposal,
                    Status = "flagged",
                    Rank = null,
                    Score = null,
                    Reason = "Human verification required for: " + string.Join("; ", missingMandatory)
                });
                continue;
            }

            var missing = GetMissingScoringFields(p);
            if (missing.Count > 0)
            {
                results.Add(new RankingResult
                {
                    Proposal = p.Proposal,
                    Status = "flagged",
                    Rank = null,
                    Score = null,
                    Reason = "Cannot calculate a comparable score until these fields are verified: " +
                             string.Join(", ", missing)
                });
                continue;
            }

            // The budget and timeline are treated as scoring constraints, not
            // mandatory failures, because the brief does not explicitly define
            // over-budget proposals as automatically ineligible.
            if (p.PriceCad!.Value > config.BudgetCad)
            {
                results.Add(new RankingResult
                {
                    Proposal = p.Proposal,
                    Status = "flagged",
                    Rank = null,
                    Score = null,
                    Reason = $"Price exceeds the configured budget of CAD {config.BudgetCad:0.00}; human review required."
                });
                continue;
            }

            if (p.TimelineWeeks!.Value > config.MaximumTimelineWeeks)
            {
                results.Add(new RankingResult
                {
                    Proposal = p.Proposal,
                    Status = "flagged",
                    Rank = null,
                    Score = null,
                    Reason = $"Timeline exceeds {config.MaximumTimelineWeeks} weeks; human review required."
                });
                continue;
            }

            results.Add(new RankingResult
            {
                Proposal = p.Proposal,
                Status = "ranked",
                Rank = null,
                Score = 0m,
                Reason = "Scored using the configured rules; final award decision remains with the human reviewer."
            });
        }

        var eligible = proposals
            .Where(p => results.Any(r => r.Proposal == p.Proposal && r.Status == "ranked"))
            .ToList();

        if (eligible.Count == 0)
            return results;

        decimal lowestPrice = eligible.Min(p => p.PriceCad!.Value);
        decimal shortestTimeline = eligible.Min(p => p.TimelineWeeks!.Value);
        decimal highestPanelWarranty = eligible.Max(p => p.PanelWarrantyYears!.Value);
        decimal highestWorkmanshipWarranty = eligible.Max(p => p.WorkmanshipWarrantyYears!.Value);
        decimal highestCount = eligible.Max(p => p.QualifyingInstallations!.Value);
        decimal highestSize = eligible.Max(p => p.LargestQualifyingInstallationKw!.Value);
        decimal highestSpread = eligible.Max(p => p.ActiveQualifyingInstallationYears!.Value);

        foreach (var p in eligible)
        {
            decimal experienceScore =
                RelativeScore(p.QualifyingInstallations!.Value, highestCount) * config.ExperienceCountWeight / 100m +
                RelativeScore(p.LargestQualifyingInstallationKw!.Value, highestSize) * config.ExperienceSizeWeight / 100m +
                RelativeScore(p.ActiveQualifyingInstallationYears!.Value, highestSpread) * config.ExperienceSpreadWeight / 100m;

            // Lowest eligible price gets 100; other prices score proportionally.
            decimal priceScore = lowestPrice / p.PriceCad!.Value * 100m;

            // Shortest eligible timeline gets 100; other timelines score proportionally.
            decimal timelineScore = shortestTimeline / p.TimelineWeeks!.Value * 100m;

            // Average the relative panel and workmanship warranty scores.
            decimal panelScore = RelativeScore(p.PanelWarrantyYears!.Value, highestPanelWarranty);
            decimal workmanshipScore = RelativeScore(p.WorkmanshipWarrantyYears!.Value, highestWorkmanshipWarranty);
            decimal warrantyScore = (panelScore + workmanshipScore) / 2m;

            decimal teamScore = p.TeamApproachAiScore!.Value;

            decimal total =
                experienceScore * config.ExperienceWeight / 100m +
                priceScore * config.PriceWeight / 100m +
                timelineScore * config.TimelineWeight / 100m +
                warrantyScore * config.WarrantyWeight / 100m +
                teamScore * config.TeamApproachWeight / 100m;

            var result = results.Single(r => r.Proposal == p.Proposal && r.Status == "ranked");
            result.Score = Math.Round(total, 2, MidpointRounding.AwayFromZero);
            result.Reason =
                $"Scored with configured weights. Experience {experienceScore:0.00}/100; " +
                $"price {priceScore:0.00}/100; timeline {timelineScore:0.00}/100; " +
                $"warranty {warrantyScore:0.00}/100; AI team/approach assessment {teamScore:0.00}/100. " +
                "Reviewer must verify evidence and make the final award decision.";
        }

        return results;
    }

    private static List<string> GetMandatoryFailures(Proposal p)
    {
        var failures = new List<string>();
        if (p.OntarioElectricalLicenceHeldAndStated == false)
            failures.Add("Ontario electrical contractor licence is not held and stated by the bidder");
        if (p.WsIbClearanceConfirmed == false)
            failures.Add("WSIB clearance not confirmed");
        if (p.LiabilityInsurance5MCadConfirmed == false)
            failures.Add("CAD $5M liability insurance not confirmed");
        return failures;
    }

    private static List<string> GetUnknownMandatoryChecks(Proposal p)
    {
        var unknown = new List<string>();
        if (p.OntarioElectricalLicenceHeldAndStated is null)
            unknown.Add("Ontario electrical contractor licence");
        if (p.WsIbClearanceConfirmed is null)
            unknown.Add("WSIB clearance");
        if (p.LiabilityInsurance5MCadConfirmed is null)
            unknown.Add("CAD $5M liability insurance");
        return unknown;
    }

    private static List<string> GetMissingScoringFields(Proposal p)
    {
        var missing = new List<string>();
        if (p.PriceCad is null || p.PriceCad.Value <= 0) missing.Add("price");
        if (p.TimelineWeeks is null || p.TimelineWeeks.Value <= 0) missing.Add("timeline");
        if (p.PanelWarrantyYears is null || p.PanelWarrantyYears.Value <= 0) missing.Add("panel warranty");
        if (p.WorkmanshipWarrantyYears is null || p.WorkmanshipWarrantyYears.Value <= 0) missing.Add("workmanship warranty");
        if (p.QualifyingInstallations is null || p.QualifyingInstallations.Value < 0) missing.Add("number of qualifying installations");
        if (p.LargestQualifyingInstallationKw is null || p.LargestQualifyingInstallationKw.Value <= 0) missing.Add("largest qualifying installation capacity");
        if (p.ActiveQualifyingInstallationYears is null ||
            p.ActiveQualifyingInstallationYears.Value < 1 ||
            p.ActiveQualifyingInstallationYears.Value > 5)
            missing.Add("number of distinct years with qualifying installations in the experience window");
        if (p.TeamApproachAiScore is null || p.TeamApproachAiScore.Value < 0 || p.TeamApproachAiScore.Value > 100)
            missing.Add("AI team/approach score (0-100)");
        return missing;
    }

    private static decimal RelativeScore(decimal value, decimal maximum)
        => maximum <= 0 ? 0m : value / maximum * 100m;
}
