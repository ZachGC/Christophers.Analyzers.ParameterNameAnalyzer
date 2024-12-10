using System.Threading.Tasks;
using Xunit;

namespace ParameterNameAnalyzer.Test
{
    public class ParameterNameCodeFixTests
    {
        [Fact]
        public async Task AddsParameterNames_ForPositionalArguments()
        {
            var testCode = @"
class TestClass
{
    void TestMethod(int x, int y)
    {
        TestMethod(1, 2);
    }
}
";

            var fixedCode = @"
class TestClass
{
    void TestMethod(int x, int y)
    {
        TestMethod(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 20, 6, 21)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 23, 6, 24)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForConstructorCall()
        {
            var testCode = @"
class TestClass
{
    TestClass(int x, int y) {}

    void M()
    {
        var t = new TestClass(1, 2);
    }
}
";

            var fixedCode = @"
class TestClass
{
    TestClass(int x, int y) {}

    void M()
    {
        var t = new TestClass(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 31, 8, 32)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 34, 8, 35)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForMixedNamedAndPositionalArguments()
        {
            var testCode = @"
class TestClass
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(1, y: 2, 3);
    }
}
";

            var fixedCode = @"
class TestClass
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, y: 2, z: 3);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 20, 6, 21)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 29, 6, 30)
                    .WithMessage("Parameter name is missing for argument '3'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForChainedMethodCalls()
        {
            var testCode = @"
class TestClass
{
    TestClass TestMethod(int x, int y) => this;

    void M()
    {
        this.TestMethod(1, 2).TestMethod(3, 4);
    }
}
";

            var fixedCode = @"
class TestClass
{
    TestClass TestMethod(int x, int y) => this;

    void M()
    {
        this.TestMethod(x: 1, y: 2).TestMethod(x: 3, y: 4);
    }
}
";

            var expected = new[]
            {
                // For the first call (1, 2)
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 25, 8, 26)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 28, 8, 29)
                    .WithMessage("Parameter name is missing for argument '2'"),
                // For the second call (3, 4)
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 42, 8, 43)
                    .WithMessage("Parameter name is missing for argument '3'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 45, 8, 46)
                    .WithMessage("Parameter name is missing for argument '4'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForGenericMethods()
        {
            var testCode = @"
class TestClass
{
    void TestMethod<T>(T x, T y) { }

    void M()
    {
        TestMethod<int>(1, 2);
    }
}
";

            var fixedCode = @"
class TestClass
{
    void TestMethod<T>(T x, T y) { }

    void M()
    {
        TestMethod<int>(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 25, 8, 26)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(8, 28, 8, 29)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForExtensionMethods()
        {
            var testCode = @"
static class Extensions
{
    public static void Extend(this TestClass obj, int x, int y) { }
}

class TestClass
{
    void M()
    {
        this.Extend(1, 2);
    }
}
";

            var fixedCode = @"
static class Extensions
{
    public static void Extend(this TestClass obj, int x, int y) { }
}

class TestClass
{
    void M()
    {
        this.Extend(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(11, 21, 11, 22)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(11, 24, 11, 25)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_InLambdaExpressions()
        {
            var testCode = @"
using System;

class TestClass
{
    void TestMethod(int x, int y) { }
    void M()
    {
        Action action = () => TestMethod(1, 2);
        action();
    }
}
";

            var fixedCode = @"
using System;

class TestClass
{
    void TestMethod(int x, int y) { }
    void M()
    {
        Action action = () => TestMethod(x: 1, y: 2);
        action();
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(9, 42, 9, 43)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(9, 45, 9, 46)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_InAsyncMethods()
        {
            var testCode = @"
using System.Threading.Tasks;

class TestClass
{
    async Task TestMethodAsync(int x, int y) { }
    async Task M()
    {
        await TestMethodAsync(1, 2);
    }
}
";

            var fixedCode = @"
using System.Threading.Tasks;

class TestClass
{
    async Task TestMethodAsync(int x, int y) { }
    async Task M()
    {
        await TestMethodAsync(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(9, 31, 9, 32)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(9, 34, 9, 35)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_InOverriddenMethods()
        {
            var testCode = @"
class BaseClass
{
    public virtual void TestMethod(int x, int y) { }
}

class DerivedClass : BaseClass
{
    public override void TestMethod(int x, int y)
    {
        base.TestMethod(1, 2);
    }
}
";

            var fixedCode = @"
class BaseClass
{
    public virtual void TestMethod(int x, int y) { }
}

class DerivedClass : BaseClass
{
    public override void TestMethod(int x, int y)
    {
        base.TestMethod(x: 1, y: 2);
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(11, 25, 11, 26)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(11, 28, 11, 29)
                    .WithMessage("Parameter name is missing for argument '2'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_InGenericClasses()
        {
            var testCode = @"
class GenericClass<T>
{
    void TestMethod(T x, T y)
    {
        TestMethod(default(T), default(T));
    }
}
";

            var fixedCode = @"
class GenericClass<T>
{
    void TestMethod(T x, T y)
    {
        TestMethod(x: default(T), y: default(T));
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 20, 6, 30)
                    .WithMessage("Parameter name is missing for argument 'default(T)'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(6, 32, 6, 42)
                    .WithMessage("Parameter name is missing for argument 'default(T)'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task AddsParameterNames_ForParamsParameters()
        {
            var testCode = @"
class TestClass
{
    void TestMethod(params int[] numbers) { }
    void M()
    {
        TestMethod(1, 2, 3);
    }
}
";

            // The code fix should add names for each argument even though it's a params parameter.
            // Note: This might not be the ideal behavior for params, but we'll test it to ensure it does what's expected.
            var fixedCode = @"
class TestClass
{
    void TestMethod(params int[] numbers) { }
    void M()
    {
        TestMethod(numbers: new int[] { 1, 2, 3 });
    }
}
";

            var expected = new[]
            {
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(7, 20, 7, 21)
                    .WithMessage("Parameter name is missing for argument '1'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(7, 23, 7, 24)
                    .WithMessage("Parameter name is missing for argument '2'"),
                CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.Diagnostic(ParameterNameAnalyzer.DiagnosticId)
                    .WithSpan(7, 26, 7, 27)
                    .WithMessage("Parameter name is missing for argument '3'")
            };

            await CSharpCodeFixVerifier<ParameterNameAnalyzer, ParameterNameCodeFixProvider>.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }
    }
}
