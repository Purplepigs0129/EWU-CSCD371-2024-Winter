﻿using IntelliTect.TestTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment.Tests;

[TestClass]
public class PingProcessTests
{
    PingProcess Sut { get; set; } = new();

    [TestInitialize]
    public void TestInitialize()
    {
        Sut = new();
    }

    [TestMethod]
    public void Start_PingProcess_Success()
    {
        Process process = Process.Start("ping", "localhost");
        process.WaitForExit();
        Assert.AreEqual<int>(0, process.ExitCode);
    }

    [TestMethod]
    public void Run_GoogleDotCom_Success()
    {
        int exitCode = Sut.Run("google.com").ExitCode;
        Assert.AreEqual<int>(0, exitCode);
    }


    [TestMethod]
    public void Run_InvalidAddressOutput_Success()
    {
        (int exitCode, string? stdOutput) = Sut.Run("badaddress");
        Assert.IsFalse(string.IsNullOrWhiteSpace(stdOutput));
        stdOutput = WildcardPattern.NormalizeLineEndings(stdOutput!.Trim());
        Assert.AreEqual<string?>(
            "Ping request could not find host badaddress. Please check the name and try again.".Trim(),
            stdOutput,
            $"Output is unexpected: {stdOutput}");
        Assert.AreEqual<int>(1, exitCode);
    }

    [TestMethod]
    public void Run_CaptureStdOutput_Success()
    {
        PingResult result = Sut.Run("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunTaskAsync_Success()
    {
        // Do NOT use async/await in this test.
        // Test Sut.RunTaskAsync("localhost");
        Task<PingResult> task = Sut.RunTaskAsync("localhost");
        task.Start();
        AssertValidPingOutput(task.Result);
    }

    //1
    [TestMethod]
    public void RunAsync_UsingTaskReturn_Success()
    {
        // Do NOT use async/await in this test.
       // PingResult result = default;
        // Test Sut.RunAsync("localhost");
        Task<PingResult> task = Sut.RunAsync("localhost");
        AssertValidPingOutput(task.Result);
    }

    //2
    [TestMethod]
//#pragma warning disable CS1998 // Remove this
    async public Task RunAsync_UsingTpl_Success()
    {
        // DO use async/await in this test.
        // PingResult result = default;
        // Test Sut.RunAsync("localhost");
        PingResult result = await Sut.RunAsync("localhost");
        AssertValidPingOutput(result);
    }
//#pragma warning restore CS1998 // Remove this

    //3
    [TestMethod]
    [ExpectedException(typeof(AggregateException))]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrapping()
    {
        CancellationTokenSource cancellationTokenSource = new();
        Task<PingResult> task = Task.Run(() => Sut.RunAsync("localhost", cancellationTokenSource.Token));
        cancellationTokenSource.Cancel();
        task.Wait();
    }

    //3
    [TestMethod]
    [ExpectedException(typeof(TaskCanceledException))]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrappingTaskCanceledException()
    {
        // Use exception.Flatten()
        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        Task<PingResult> task = Sut.RunAsync("localhost", cancellationTokenSource.Token);
        try
        {
            task.Wait();
        } catch (AggregateException aggregateException)
        {
            aggregateException = aggregateException.Flatten();

            if(aggregateException != null)
            {
                throw aggregateException.InnerException!;
            }
            throw;
        }
    }

    [TestMethod]
    async public Task RunAsync_MultipleHostAddresses_True()
    {
        // Pseudo Code - don't trust it!!!
        string[] hostNames = new string[] { "localhost", "localhost", "localhost", "localhost" };
        int expectedLineCount = PingOutputLikeExpression.Split(Environment.NewLine).Length*hostNames.Length;
        PingResult result = await Sut.RunAsync(hostNames);
        int? lineCount = result.StdOutput?.Split(Environment.NewLine).Length;
        Assert.AreEqual(expectedLineCount, lineCount);
    }

    [TestMethod]
#pragma warning disable CS1998 // Remove this
    async public Task RunLongRunningAsync_UsingTpl_Success()
    {
        PingResult result = default;
        // Test Sut.RunLongRunningAsync("localhost");
        AssertValidPingOutput(result);
    }
#pragma warning restore CS1998 // Remove this

    [TestMethod]
    public void StringBuilderAppendLine_InParallel_IsNotThreadSafe()
    {
        IEnumerable<int> numbers = Enumerable.Range(0, short.MaxValue);
        System.Text.StringBuilder stringBuilder = new();
        numbers.AsParallel().ForAll(item => stringBuilder.AppendLine(""));
        int lineCount = stringBuilder.ToString().Split(Environment.NewLine).Length;
        Assert.AreNotEqual(lineCount, numbers.Count()+1);
    }

    // Create a test for RunAsync(IEnumerable<string> hostNameOrAddresses, CancellationToken cancellationToken = default)
    [TestMethod]
    public void RunAsync_MultipleHostAddressesWithCancellation_True()
    {
        CancellationTokenSource cancellationTokenSource = new();
        string[] hostNames = new string[] { "localhost", "localhost", "localhost", "localhost" };
        int expectedLineCount = PingOutputLikeExpression.Split(Environment.NewLine).Length * hostNames.Length;
        PingResult result = Sut.RunAsync(hostNames, cancellationTokenSource.Token).Result;
        int? lineCount = result.StdOutput?.Split(Environment.NewLine).Length;
        Assert.AreEqual(expectedLineCount, lineCount);
    }

    // Create a test for public Task<int> RunLongRunningAsync(ProcessStartInfo startInfo, Action<string?>? progressOutput, Action<string?>? progressError, CancellationToken token)
    [TestMethod]
    public void RunLongRunningAsync_ProcessStartInfo_Success()
    {
        ProcessStartInfo startInfo = new("ping", "localhost");
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.ErrorDialog = false;
        startInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
        startInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.ErrorDialogParentHandle = IntPtr.Zero;
        startInfo.Verb = "runas";
        startInfo.UserName = Environment.UserName;
        startInfo.FileName = "ping";
        startInfo.Arguments = "localhost";
        startInfo.Verb = "runas";
        startInfo.UserName = Environment.UserName;
        startInfo.FileName = "ping";
        startInfo.Arguments = "localhost";
        startInfo.Verb = "runas";
    }

    


    readonly string PingOutputLikeExpression = @"
Pinging * with 32 bytes of data:
Reply from ::1: time<*
Reply from ::1: time<*
Reply from ::1: time<*
Reply from ::1: time<*

Ping statistics for ::1:
    Packets: Sent = *, Received = *, Lost = 0 (0% loss),
Approximate round trip times in milli-seconds:
    Minimum = *, Maximum = *, Average = *".Trim();
    private void AssertValidPingOutput(int exitCode, string? stdOutput)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(stdOutput));
        stdOutput = WildcardPattern.NormalizeLineEndings(stdOutput!.Trim());
        Assert.IsTrue(stdOutput?.IsLike(PingOutputLikeExpression)??false,
            $"Output is unexpected: {stdOutput}");
        Assert.AreEqual<int>(0, exitCode);
    }
    private void AssertValidPingOutput(PingResult result) =>
        AssertValidPingOutput(result.ExitCode, result.StdOutput);
}