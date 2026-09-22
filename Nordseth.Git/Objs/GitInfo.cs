namespace Nordseth.Git;

public record GitInfo
{
    public required string CommitId { get; init; }
    public string? CommitMessage { get; init; }
    public string? CommitAuthor { get; init; }
    public string? CommitDate { get; init; }
    public string? Branch { get; init; }
    public string? CommitDescription { get; set; }
    public string? OriginUrl { get; set; }

    public override string ToString()
    {
        return $@"CommitId: {CommitId}
CommitMessage:{CommitMessage}
CommitAuthor:{CommitAuthor}
CommitDate:{CommitDate}
Branch:{Branch}
CommitDescription:{CommitDescription}
OriginUrl:{OriginUrl}";
    }
}
