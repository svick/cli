﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class StreamForwarderTests : TestBase
    {
        private static readonly string s_rid = RuntimeEnvironmentRidExtensions.GetLegacyRestoreRuntimeIdentifier();
        private static readonly string s_testProjectRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");

        private readonly TempDirectory _root;

        public static void Main()
        {
            Console.WriteLine("Dummy Entrypoint");
        }

        public StreamForwarderTests()
        {
            _root = Temp.CreateDirectory();
        }

        public static IEnumerable<object[]> ForwardingTheoryVariations
        {
            get
            {
                return new[]
                {
                    new object[] { "\n\n\n", new string[]{"\n", "\n", "\n"} },
                    new object[] { "\r\n\r\n\r\n", new string[]{"\r\n", "\r\n", "\r\n"} },
                    new object[] { "123", new string[]{"123"} },
                    new object[] { "123\n", new string[] {"123\n"} },
                    new object[] { "123\r\n", new string[] {"123\r\n"} },
                    new object[] { "1234\n5678", new string[] {"1234\n", "5678"} },
                    new object[] { "1234\r\n5678", new string[] {"1234\r\n", "5678"} },
                    new object[] { "1234\n\n5678", new string[] {"1234\n", "\n", "5678"} },
                    new object[] { "1234\r\n\r\n5678", new string[] {"1234\r\n", "\r\n", "5678"} },
                    new object[] { "1234\n5678\n", new string[] {"1234\n", "5678\n"} },
                    new object[] { "1234\r\n5678\r\n", new string[] {"1234\r\n", "5678\r\n"} },
                    new object[] { "1234\n5678\nabcdefghijklmnopqrstuvwxyz", new string[] {"1234\n", "5678\n", "abcdefghijklmnopqrstuvwxyz"} },
                    new object[] { "1234\r\n5678\r\nabcdefghijklmnopqrstuvwxyz", new string[] {"1234\r\n", "5678\r\n", "abcdefghijklmnopqrstuvwxyz"} },
                    new object[] { "1234\n5678\nabcdefghijklmnopqrstuvwxyz\n", new string[] {"1234\n", "5678\n", "abcdefghijklmnopqrstuvwxyz\n"} },
                    new object[] { "1234\r\n5678\r\nabcdefghijklmnopqrstuvwxyz\r\n", new string[] {"1234\r\n", "5678\r\n", "abcdefghijklmnopqrstuvwxyz\r\n"} }
                };
            }
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123\n")]
        public void TestNoForwardingNoCapture(string inputStr)
        {
            TestCapturingAndForwardingHelper(ForwardOptions.None, inputStr, null, new string[0]);
        }

        [Theory]
        [MemberData("ForwardingTheoryVariations")]
        public void TestForwardingOnly(string inputStr, string[] expectedWrites)
        {
            TestCapturingAndForwardingHelper(ForwardOptions.Write, inputStr, null, expectedWrites);
        }

        [Theory]
        [MemberData("ForwardingTheoryVariations")]
        public void TestCaptureOnly(string inputStr, string[] expectedWrites)
        {
            var expectedCaptured = string.Join("", expectedWrites);

            TestCapturingAndForwardingHelper(ForwardOptions.Capture, inputStr, expectedCaptured, new string[0]);
        }

        [Theory]
        [MemberData("ForwardingTheoryVariations")]
        public void TestCaptureAndForwardingTogether(string inputStr, string[] expectedWrites)
        {
            var expectedCaptured = string.Join("", expectedWrites);

            TestCapturingAndForwardingHelper(ForwardOptions.Write | ForwardOptions.Capture, inputStr, expectedCaptured, expectedWrites);
        }

        [Flags]
        private enum ForwardOptions
        {
            None = 0x0,
            Capture = 0x1,
            Write = 0x02,
        }

        private void TestCapturingAndForwardingHelper(ForwardOptions options, string str, string expectedCaptured, string[] expectedWrites)
        {
            var forwarder = new StreamForwarder();
            var writes = new List<string>();

            if ((options & ForwardOptions.Write) != 0)
            {
                forwarder.ForwardTo(write: s => writes.Add(s));
            }
            if ((options & ForwardOptions.Capture) != 0)
            {
                forwarder.Capture();
            }

            forwarder.Read(new StringReader(str));
            Assert.Equal(expectedWrites, writes);

            var captured = forwarder.CapturedOutput;
            Assert.Equal(expectedCaptured, captured);
        }

        private string SetupTestProject()
        {
            var sourceTestProjectPath = Path.Combine(s_testProjectRoot, "OutputStandardOutputAndError");

            var binTestProjectPath = _root.CopyDirectory(sourceTestProjectPath).Path;

            var buildCommand = new BuildCommand(Path.Combine(binTestProjectPath, "project.json"));
            buildCommand.Execute();

            var buildOutputExe = "OutputStandardOutputAndError" + Constants.ExeSuffix;
            var buildOutputPath = Path.Combine(binTestProjectPath, "bin/Debug/netcoreapp1.0", buildOutputExe);

            return buildOutputPath;
        }
    }
}
