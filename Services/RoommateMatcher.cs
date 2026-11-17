using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

/// <summary>
/// Scores how compatible two roommate bios are, from 0 to 100.
/// Each factor is a similarity between 0 and 1, and the score is their weighted average.
/// Tune the weights here.
/// </summary>
public static class RoommateMatcher
{
    private const double SleepWeight = 3;
    private const double CleanlinessWeight = 3;
    private const double NoiseWeight = 2;
    private const double GuestsWeight = 2;
    private const double SmokingWeight = 2;
    private const double BuildingWeight = 2;
    private const double BudgetWeight = 2;
    private const double InterestsWeight = 1;

    // used when either student left a factor blank, so missing answers neither help nor hurt much
    private const double Unknown = 0.5;

    /// <summary>
    /// whether two students could room together at all: they must want the same move-in term
    /// when both have given one
    /// </summary>
    public static bool CanMatch(RoommateBio a, RoommateBio b) =>
        string.IsNullOrWhiteSpace(a.Term) || string.IsNullOrWhiteSpace(b.Term)
        || string.Equals(a.Term.Trim(), b.Term.Trim(), StringComparison.OrdinalIgnoreCase);

    public static int Score(RoommateBio a, RoommateBio b)
    {
        var factors = new (double Similarity, double Weight)[]
        {
            (Scale(a.SleepSchedule, b.SleepSchedule), SleepWeight),
            (Scale(a.Cleanliness, b.Cleanliness), CleanlinessWeight),
            (Scale(a.NoiseTolerance, b.NoiseTolerance), NoiseWeight),
            (Scale(a.GuestFrequency, b.GuestFrequency), GuestsWeight),
            (a.Smoker.HasValue && b.Smoker.HasValue ? (a.Smoker == b.Smoker ? 1 : 0) : Unknown, SmokingWeight),
            (Overlap(a.PreferredBuildingIds, b.PreferredBuildingIds, StringComparer.Ordinal), BuildingWeight),
            (Budget(a, b), BudgetWeight),
            (Overlap(a.Interests, b.Interests, StringComparer.OrdinalIgnoreCase), InterestsWeight),
        };

        var total = factors.Sum(f => f.Similarity * f.Weight) / factors.Sum(f => f.Weight);
        return (int)Math.Round(total * 100);
    }

    /// <summary>
    /// buildings both students listed
    /// </summary>
    public static List<string> SharedBuildings(RoommateBio a, RoommateBio b) =>
        (a.PreferredBuildingIds ?? new()).Intersect(b.PreferredBuildingIds ?? new()).ToList();

    /// <summary>
    /// interests both students listed, ignoring case
    /// </summary>
    public static List<string> SharedInterests(RoommateBio a, RoommateBio b) =>
        (a.Interests ?? new()).Intersect(b.Interests ?? new(), StringComparer.OrdinalIgnoreCase).ToList();

    // 1 when the answers are equal, 0 when they are at opposite ends of the scale
    private static double Scale(int? a, int? b) =>
        a.HasValue && b.HasValue
            ? 1 - Math.Abs(a.Value - b.Value) / (double)(RoommateBio.MAX_SCALE - RoommateBio.MIN_SCALE)
            : Unknown;

    // share of the shorter list that also appears in the other one
    private static double Overlap(List<string>? a, List<string>? b, StringComparer comparer)
    {
        if (a is null || b is null || a.Count == 0 || b.Count == 0)
        {
            return Unknown;
        }

        var shared = a.Intersect(b, comparer).Count();
        return shared / (double)Math.Min(a.Distinct(comparer).Count(), b.Distinct(comparer).Count());
    }

    // 1 when the budget ranges overlap, falling toward 0 as the gap between them grows
    private static double Budget(RoommateBio a, RoommateBio b)
    {
        if (a.BudgetMax is null || b.BudgetMax is null)
        {
            return Unknown;
        }

        var gap = Math.Max(a.BudgetMin ?? 0, b.BudgetMin ?? 0) - Math.Min(a.BudgetMax.Value, b.BudgetMax.Value);
        if (gap <= 0)
        {
            return 1;
        }

        var widest = Math.Max(a.BudgetMax.Value, b.BudgetMax.Value);
        return widest == 0 ? 0 : Math.Max(0, 1 - gap / (double)widest);
    }
}
