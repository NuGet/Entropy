namespace GithubIssueTagger.Reports.CiReliability
{
    internal struct PipelineData
    {
        public string? PipelineName { get; init; }
        public string? BranchFilterQueryString { get; init; }
        public string? DatabaseName { get; init; }
        public string? OrganizationName { get; init; }
        public string? ProjectId { get; init; }
        public string? ProjectName { get; init; }
        public string? DefinitionId { get; init; }
        public string? SourceBranch { get; init; }
        public string? Reason { get; init; }
    }
}
