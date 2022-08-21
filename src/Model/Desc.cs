namespace Recline.Generator.Model;

public record Desc(string Name, string? Description);
public record WithArgsDesc(string Name, string[] ArgNames, string? Description)
    : Desc(Name, Description);
public record OptDesc(string LongName, char Alias, string ArgName, string? Description)
    : WithArgsDesc(LongName, new[] { ArgName }, Description);
public record FlagDesc(string LongName, char Alias, string? Description)
    : OptDesc(LongName, Alias, "", Description);