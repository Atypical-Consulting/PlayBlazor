namespace PlayBlazor.DemoHost;

public sealed record Person(string Name, string Role, int Age)
{
    public static readonly IReadOnlyList<Person> Samples =
    [
        new("Ada Lovelace", "Engineering", 36),
        new("Grace Hopper", "R&D", 85),
        new("Katherine Johnson", "Science", 101),
        new("Hedy Lamarr", "Wireless", 85),
    ];
}
