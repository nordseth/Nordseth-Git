namespace Nordseth.Git;

public record Tree
{
    public required string Mode { get; init; }
    public required string Name { get; init; }
    public required string Ref { get; init; }

    public override string ToString()
    {
        return $"{Mode} {Name} {Ref}";
    }
}
