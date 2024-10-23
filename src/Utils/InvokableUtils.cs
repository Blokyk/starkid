using StarKid.Generator.CommandModel;

namespace StarKid.Generator.Utils;

internal static class InvokableUtils
{
    public static IEnumerable<Group> TraverseGroupTree(Group node) {
        yield return node;

        foreach (var directChild in node.SubGroups) {
            // TraverseGroupTree will also return the root, no need to yield it here
            foreach (var child in TraverseGroupTree(directChild)) {
                yield return child;
            }
        }
    }

    public static IEnumerable<InvokableBase> TraverseInvokableTree(InvokableBase invokable) {
        yield return invokable;

        if (invokable is Group group) {
            foreach (var cmd in group.Commands)
                yield return cmd;

            foreach (var directChild in group.SubGroups) {
                // TraverseInvokableTree will also return the root, no need to yield it here
                foreach (var child in TraverseInvokableTree(directChild)) {
                    yield return child;
                }
            }
        }
    }
}