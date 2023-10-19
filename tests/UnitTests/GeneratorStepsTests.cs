using StarKid.Generator;

namespace StarKid.Tests;

public static class GeneratorStepsTests
{
    public class CollectUsings {
        [Fact]
        public void Simple() {
            var source = @"
using System;
using System.IO;

[|class C1|] {}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System", "System.IO" }, usings);
        }

        [Fact]
        public void DoesntCaptureStatic() {
            var source = @"
using System;
using System.IO;

using static System.Console;

[|class C1|] {}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System", "System.IO" }, usings);
        }

        [Fact]
        public void DoesntCaptureAlias() {
            var source = @"
using System;
using io = System.IO;

[|class C1|] {}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System" }, usings);
        }

        [Fact]
        public void WithSingleNamespace() {
            var source = @"
using System.IO;

namespace Foo.Bar.Baz {

    [|class C1|] {}

}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System.IO", "Foo.Bar.Baz" }, usings);
        }

        [Fact]
        public void WithNestedNamespace() {
            var source = @"
using System.IO;

namespace Foo {
    namespace Bar {
        namespace Baz {
            [|class C1|] {}
        }
    }
}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System.IO", "Foo.Bar.Baz" }, usings);
        }

        [Fact]
        public void InsideSingleNamespace() {
            var source = @"
using System.IO;

namespace Foo {
    using System.Diagnostics;

    [|class C1|] {}
}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System.IO", "System.Diagnostics", "Foo" }, usings);
        }

        [Fact]
        public void InsideNestedNamespace() {
            var source = @"
using System.IO;

namespace Foo {
    using System.Diagnostics;

    namespace Bar {
        using Microsoft.CodeAnalysis;

        [|class C1|] {}
    }
}
";

            _ = SyntaxTree.WithMarkedNode(source, out var node);

            var usings = StarKidGenerator.GetReachableNamespaceNames(node);
            Assert.Equivalent(new[] { "System.IO", "System.Diagnostics", "Microsoft.CodeAnalysis", "Foo.Bar" }, usings);
        }
    }
}