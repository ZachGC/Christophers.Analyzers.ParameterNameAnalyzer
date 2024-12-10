using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ParameterNameAnalyzer.ParameterNameAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;


namespace ParameterNameAnalyzer.Test;

public class ParameterNameAnalyzerTests
{
    // Test for no diagnostic
    [Fact]
    public async Task TestGeneral_NoDiagnostic()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x, int y)
                    {
                        TestMethod(x: 1, y: 2);
                    }
                }
            ";

        // Verify no diagnostics are raised for correctly named parameters
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }


    [Fact]
    public async Task NoDiagnosticWhenAllParameterNamesArePresent()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x, int y)
                    {
                        TestMethod(x: 1, y: 2);
                    }
                }
            ";

        // Verify no diagnostics are raised when parameter names are already present
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    // Test for missing parameter names
    [Fact]
    public async Task DiagnosticRaisedWhenParameterNamesAreMissing()
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

        DiagnosticResult[] expected = [
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 36, 6, 37)
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 39, 6, 40)
                .WithMessage("Parameter name is missing for argument '2'")
                .WithArguments("2")
        ];

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task NoDiagnosticWhenParameterNamesAreExplicit()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x, int y)
                    {
                        TestMethod(x: 1, y: 2);
                    }
                }
            ";

        // No diagnostics expected
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task DiagnosticForMultipleArgumentsWithoutParameterNames()
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

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 36, 6, 37)
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 39, 6, 40)
                .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task NoDiagnosticWhenDefaultParametersAreUsed()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x = 5, int y = 10)
                    {
                        TestMethod();
                    }
                }
            ";

        // No diagnostics expected because default values are used
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task DiagnosticForMixedNamedAndPositionalArguments()
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

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 36, 6, 37)
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(6, 45, 6, 46)
                .WithMessage("Parameter name is missing for argument '3'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticForConstructorCallsWithMissingParameterNames()
    {
        var testCode = @"
                class TestClass
                {
                    TestClass(int x, int y) { }

                    void Method()
                    {
                        var obj = new TestClass(1, 2);
                    }
                }
            ";

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 49, 8, 50)
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 52, 8, 53)
                .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task TestAnalyzerForOverloadedMethods()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x, int y) { }
                    void TestMethod(int x, int y, string z) { }
                
                    void Method()
                    {
                        TestMethod(x: 1, y: 2);           // No diagnostic (all parameter names provided)
                        TestMethod(x: 1, 2);              // Diagnostic for '2'
                        TestMethod(1, y: 2);              // Diagnostic for '1'
                        TestMethod(1, 2);                 // Diagnostics for '1' and '2'
                
                        TestMethod(x: 1, y: 2, z: ""test""); // No diagnostic (all parameter names provided)
                        TestMethod(x: 1, y: 2, ""test"");    // Diagnostic for '""test""'
                        TestMethod(x: 1, 2, z: ""test"");    // Diagnostic for '2'
                        TestMethod(1, y: 2, z: ""test"");    // Diagnostic for '1'
                        TestMethod(x: 1, 2, ""test"");       // Diagnostics for '2' and '""test""'
                        TestMethod(1, y: 2, ""test"");       // Diagnostics for '1' and '""test""'
                        TestMethod(1, 2, z: ""value"");      // Diagnostics for '1' and '2'
                        TestMethod(1, 2, ""test"");          // Diagnostics for '1', '2', and '""test""'
                    }
                }
            ";

        var expected = new DiagnosticResult[]
        {
            // TestMethod(x: 1, 2);
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(10, 42, 10, 43)
                .WithMessage("Parameter name is missing for argument '2'")
                .WithArguments("2"),
        
            // TestMethod(1, y: 2);
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(11, 36, 11, 37)
                .WithMessage("Parameter name is missing for argument '1'")
                .WithArguments("1"),
        
            // TestMethod(1, 2);
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(12, 36, 12, 37)
                .WithArguments("1")
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(12, 39, 12, 40)
                .WithArguments("2")
                .WithMessage("Parameter name is missing for argument '2'"),
        
            // TestMethod(x: 1, y: 2, "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(15, 48, 15, 54)
                .WithArguments("\"test\"")
                .WithMessage("Parameter name is missing for argument '\"test\"'"),
        
            // TestMethod(x: 1, 2, z: "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(16, 42, 16, 43)
                .WithArguments("2")
                .WithMessage("Parameter name is missing for argument '2'"),
        
            // TestMethod(1, y: 2, z: "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(17, 36, 17, 37)
                .WithArguments("1")
                .WithMessage("Parameter name is missing for argument '1'"),
        
            // TestMethod(x: 1, 2, "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(18, 42, 18, 43)
                .WithArguments("2")
                .WithMessage("Parameter name is missing for argument '2'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(18, 45, 18, 51)
                .WithArguments("\"test\"")
                .WithMessage("Parameter name is missing for argument '\"test\"'"),
        
            // TestMethod(1, y: 2, "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(19, 36, 19, 37)
                .WithArguments("1")
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(19, 45, 19, 51)
                .WithArguments("\"test\"")
                .WithMessage("Parameter name is missing for argument '\"test\"'"),
        
            // TestMethod(1, 2, z: "value");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(20, 36, 20, 37)
                .WithArguments("1")
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(20, 39, 20, 40)
                .WithArguments("2")
                .WithMessage("Parameter name is missing for argument '2'"),
        
            // TestMethod(1, 2, "test");
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(21, 36, 21, 37)
                .WithArguments("1")
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(21, 39, 21, 40)
                .WithArguments("2")
                .WithMessage("Parameter name is missing for argument '2'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(21, 42, 21, 48)
                .WithArguments("\"test\"")
                .WithMessage("Parameter name is missing for argument '\"test\"'")
        };


        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticForChainedMethodCallsWithoutParameterNames()
    {
        var testCode = @"
               class TestClass
               {
                   TestClass TestMethod(int x, int y) {  return this; }

                   void Method()
                   {
                       this.TestMethod(1, 2).TestMethod(3, 4);
                   }
               }
            ";

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 40, 8, 41)
                .WithMessage("Parameter name is missing for argument '1'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 43, 8, 44)
                .WithMessage("Parameter name is missing for argument '2'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 57, 8, 58)
                .WithMessage("Parameter name is missing for argument '3'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 60, 8, 61)
                .WithMessage("Parameter name is missing for argument '4'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task NoDiagnosticForExtensionMethodThisParameter()
    {
        var testCode = @"
                static class Extensions
                {
                    public static void Extend(this TestClass obj) { }
                }

                class TestClass
                {
                    void Method()
                    {
                        this.Extend(); // No diagnostics for 'this'
                    }
                }
            ";

        // No diagnostics expected
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task NoDiagnosticForGenericMethodWithNamedParameters()
    {
        var testCode = @"
                class TestClass
                {
                    void GenericMethod<T>(T arg1, int arg2) { }

                    void Method()
                    {
                        GenericMethod<int>(arg1: 1, arg2: 2); // No diagnostic expected
                    }
                }
            ";

        // No diagnostics expected since all parameters are named
        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task NoDiagnosticForOptionalParameters()
    {
        var testCode = @"
                class TestClass
                {
                    void TestMethod(int x, int y = 10) { }

                    void Method()
                    {
                        TestMethod(1); // y is omitted because it has a default value
                    }
                }
            ";

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(8, 36, 8, 37)
                .WithMessage("Parameter name is missing for argument '1'"),
        };

        // No diagnostics expected because 'y' has a default value
        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInLambdaExpressions_MissingParameterNames()
    {
        var testCode = @"
        using System;
        using System.Linq;

        class TestClass
        {
            void TestMethod(int x, int y)
            {
                Action action = () => TestMethod(1, 2);
                action.Invoke();
            }
        }
    ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(9, 50, 9, 51)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(9, 53, 9, 54)
            .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInAsyncMethods_MissingParameterNames()
    {
        var testCode = @"
            using System.Threading.Tasks;

            class TestClass
            {
                async Task TestMethodAsync(int x, int y)
                {
                    await TestMethodAsync(1, 2);
                }
            }
        ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(8, 43, 8, 44)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(8, 46, 8, 47)
            .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInMethodsWithParamsParameters_MissingParameterNames()
    {
        var testCode = @"
        class TestClass
        {
            void TestMethod(params int[] numbers)
            {
            }

            void Caller()
            {
                TestMethod(1, 2, 3);
            }
        }
    ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(10, 28, 10, 29)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(10, 31, 10, 32)
            .WithMessage("Parameter name is missing for argument '2'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(10, 34, 10, 35)
            .WithMessage("Parameter name is missing for argument '3'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInGenericMethods_MissingParameterNames()
    {
        var testCode = @"
        class TestClass
        {
            void TestMethod<T>(T x, T y)
            {
            }

            void Caller()
            {
                TestMethod<int>(1, 2);
            }
        }
    ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(10, 33, 10, 34)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(10, 36, 10, 37)
            .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInExtensionMethods_MissingParameterNames()
    {
        var testCode = @"
        using System;

        static class Extensions
        {
            public static void Extend(this TestClass obj, int x, int y)
            {
            }
        }

        class TestClass
        {
            void Caller()
            {
                this.Extend(1, 2);
            }
        }
    ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(15, 29, 15, 30)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(15, 32, 15, 33)
            .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }


    [Fact]
    public async Task DiagnosticInMethodsWithInParameters_MissingParameterNames()
    {
        var testCode = @"
class TestClass
{
    void TestMethod(in int x, in int y)
    {
    }

    void Caller()
    {
        int a = 1;
        int b = 2;
        TestMethod(in a, in b);
    }
}
";

        var expected = new DiagnosticResult[]
        {
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(12, 20, 12, 24) // Span for 'a'
                .WithArguments("in a")
                .WithMessage("Parameter name is missing for argument 'in a'"),
            new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(12, 26, 12, 30) // Span for 'b'
                .WithArguments("in b")
                .WithMessage("Parameter name is missing for argument 'in b'"),
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInOverriddenMethods_MissingParameterNames()
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

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(11, 33, 11, 34)
            .WithMessage("Parameter name is missing for argument '1'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(11, 36, 11, 37)
            .WithMessage("Parameter name is missing for argument '2'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DiagnosticInGenericClasses_MissingParameterNames()
    {
        var testCode = @"
        class GenericClass<T>
        {
            void TestMethod(T x, T y)
            {
                TestMethod(x, y);
            }
        }
    ";

        var expected = new DiagnosticResult[]
        {
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(6, 28, 6, 29)
            .WithMessage("Parameter name is missing for argument 'x'"),
        new DiagnosticResult(ParameterNameAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
            .WithSpan(6, 31, 6, 32)
            .WithMessage("Parameter name is missing for argument 'y'")
        };

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }
}
