using System.Text;

namespace Nordseth.Git;

public record Commit
{
    public required string Id { get; init; }
    public string? Tree { get; set; }
    public required IEnumerable<string> Parents { get; init; }
    public Signature? Author { get; set; }
    public Signature? Committer { get; set; }
    public string? Message { get; set; }
    public string? MessageShort { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (Id != null)
        {
            builder.AppendLine($"commit {Id}");
        }

        if (Tree != null)
        {
            builder.AppendLine($"tree {Tree}");
        }

        if (Parents != null)
        {
            foreach (var parent in Parents)
            {
                builder.AppendLine($"parent {parent}");
            }
        }

        if (Author != null)
        {
            builder.AppendLine($"author {Author}");
        }

        if (Committer != null)
        {
            builder.AppendLine($"committer {Committer}");
        }

        if (Message != null)
        {
            builder.AppendLine();
            builder.AppendLine(Message);
        }

        return builder.ToString();
    }
}

public record Signature
{
    public required string Name { get; init; }
    public string? Email { get; init; }
    public DateTimeOffset When { get; set; }

    public override string ToString()
    {
        return $"{Name} <{Email}> {When:u}";
    }
}