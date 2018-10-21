﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIndexOperator;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIndexOperator
{
    public class UseIndexOperatorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseIndexOperatorDiagnosticAnalyzer(), new CSharpUseIndexOperatorCodeFixProvider());

        private static readonly CSharpParseOptions s_parseOptions = 
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        private static readonly TestParameters s_testParameters =
            new TestParameters(parseOptions: s_parseOptions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
        public async Task TestNotInCSharp7()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length - 1];
    }
}", parameters: new TestParameters(
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestSimple()
        {
            await TestAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length - 1];
    }
}",
@"
class C
{
    void Goo(string s)
    {
        var v = s[^1];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestComplexSubtaction()
        {
            await TestAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length - (1 + 1)];
    }
}",
@"
class C
{
    void Goo(string s)
    {
        var v = s[^(1 + 1)];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestComplexInstance()
        {
            await TestAsync(
@"
using System.Linq;

class C
{
    void Goo(string[] ss)
    {
        var v = ss.Last()[[||]ss.Last().Length - 3];
    }
}",
@"
using System.Linq;

class C
{
    void Goo(string[] ss)
    {
        var v = ss.Last()[^3];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotWithoutSubtraction1()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length];
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotWithoutSubtraction2()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length + 1];
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestNotWithoutMultipleArgs()
        {
            await TestMissingAsync(
@"
class C
{
    void Goo(string s)
    {
        var v = s[[||]s.Length - 1, 2];
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestUserDefinedTypeWithLength()
        {
            await TestAsync(
@"
namespace System { public struct Index { } }
struct S { public int Length { get; } public int this[int i] { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[[||]s.Length - 2];
    }
}",
@"
namespace System { public struct Index { } }
struct S { public int Length { get; } public int this[int i] { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[^2];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestUserDefinedTypeWithCount()
        {
            await TestAsync(
@"
namespace System { public struct Index { } }
struct S { public int Count { get; } public int this[int i] { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[[||]s.Count - 2];
    }
}",
@"
namespace System { public struct Index { } }
struct S { public int Count { get; } public int this[int i] { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[^2];
    }
}", parseOptions: s_parseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestUserDefinedTypeWithNoLengthOrCount()
        {
            await TestMissingAsync(
@"
namespace System { public struct Index { } }
struct S { public int this[int i] { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[[||]s.Count - 2];
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestUserDefinedTypeWithNoInt32Indexer()
        {
            await TestMissingAsync(
@"
namespace System { public struct Index { } }
struct S { public int Length { get; } public int this[System.Index i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[[||]s.Count - 2];
    }
}", parameters: s_testParameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseDefaultLiteral)]
        public async Task TestUserDefinedTypeWithNoIndexIndexer()
        {
            await TestMissingAsync(
@"
namespace System { public struct Index { } }
struct S { public int Length { get; } public int this[int i] { get; } }
class C
{
    void Goo(S s)
    {
        var v = s[[||]s.Count - 2];
    }
}", parameters: s_testParameters);
        }
    }
}
