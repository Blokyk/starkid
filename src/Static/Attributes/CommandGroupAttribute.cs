namespace StarKid
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CommandGroupAttribute : System.Attribute
    {
        public string GroupName { get; }
        public string? DefaultCmdName { get; set; }
        public string? ShortDesc { get; set; }

        public CommandGroupAttribute(string groupName)
            => GroupName = groupName;

        public void Deconstruct(
            out string groupName,
            out string? defaultCmd,
            out string? shortDesc
        ) {
            groupName = GroupName;
            defaultCmd = DefaultCmdName;
            shortDesc = ShortDesc;
        }
    }
}