﻿using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class StringFormatTests : CodeFixTest<StringFormatAnalyzer, StringFormatCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresRegularStrings()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var string a = ""a"";
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringMethodsThatAreNotStringFormat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var result = string.Compare(""a"", ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledFormatThatAreNotStringFormat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class OtherString { public static string Format(string a, string b) { throw new NotImplementedException(); } }
        class TypeName
        {
            void Foo()
            {
                var result = OtherString.Format(""a"", ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringFormatWithArrayArgWith1Object()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var args = new object[] { noun };
                var s = string.Format(""This {0} is nice."", args);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringFormatWithArrayArgWith2Objects()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var args = new object[] { noun, adjective };
                var s = string.Format(""This {0} is {1}"", args);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsWithOnlyOneParameter()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var result = string.Format(""a"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledWithIncorrectParameterTypes()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var result = string.Format(1, ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsWithIncorrectNumberOfParameters()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var result = string.Format(""one {0} two {1}"", ""a"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StringFormatWithMoreThan2ArgsProducesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = string.Format(""This {0} is {1}"", noun, adjective);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringFormatAnalyzer.DiagnosticId,
                Message = StringFormatAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task StringFormatWithFullStringNameProducesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = System.String.Format(""This {0} is {1}"", noun, adjective);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringFormatAnalyzer.DiagnosticId,
                Message = StringFormatAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task StringFormatChangesToStringInterpolation()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                var s = string.Format(""This {0} is {1}"", noun, adjective);//comment right after
                //comment after
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                var s = ""This \{noun} is \{adjective}"";//comment right after
                //comment after
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare:false);
        }

        [Fact]
        public async Task WhenStringFormatHasMoreArgumentsThanNecessaryChangesToStringInterpolationAndIgnoresExtraArgument()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var otherAdjective = ""loves c#"";
                var s = string.Format(""This {0} is {1}"", noun, adjective, otherAdjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var otherAdjective = ""loves c#"";
                var s = ""This \{noun} is \{adjective}"";
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task WhenStringFormatHasEscapingItChangesToStringInterpolationAndRemovesEscapingSequence()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = string.Format(""This {{0}} {0} is {1}"", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = ""This {0} \{noun} is \{adjective}"";
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenFormatStringHasLineBreaksTheCodeFixKeepsThat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = string.Format(""This {0} is\n \r\f \f \r {1}"", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = ""This \{noun} is\n \r\f \f \r \{adjective}"";
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenFormatStringHasQuotesTheCodeFixKeepsThat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = string.Format(""This {0} is \""{1}\"""", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = ""This \{noun} is \""\{adjective}\"""";
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }
    }
}