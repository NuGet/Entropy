namespace NuGetDashboard;

public record RawPR(int Number, string Title, string Url, string Author, DateTime CreatedAt, DateTime MergedAt);

public record PRRecord(
    int Number, string Title, string Url, string Author,
    DateTime CreatedAt,
    DateTime EffectiveStart,  // ready_for_review → review_requested → created_at
    DateTime MergedAt,
    double HoursToMerge,      // EffectiveStart → MergedAt
    double? FirstApprovalHours,
    DateTime? FirstApprovedAt);

public record DashboardMetrics(
    int TotalPRs,
    double MedianHoursToComplete,
    double PercentApprovedUnder24h,
    double PercentMergedUnder24h);

public record DashboardData(
    string DateRange,
    string AsOf,
    int WindowDays,
    DashboardMetrics Metrics,
    List<PRRecord> SlowPRs,
    List<PRRecord> AllPRs);
