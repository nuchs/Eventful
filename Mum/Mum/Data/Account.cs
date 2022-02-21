namespace Mum.Data;

public sealed record Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = "";

    public string Name { get; set; } = "";

    public string Org { get; set; } = "";
}
