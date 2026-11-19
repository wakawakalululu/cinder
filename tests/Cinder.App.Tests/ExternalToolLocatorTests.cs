using Cinder.App.Services;
using FluentAssertions;
using Xunit;

namespace Cinder.App.Tests;

/// <summary>
/// The external-tool locator's layers, in order: override → tools.json → registry (off in
/// tests) → search dirs → defaults. Each layer is probed with temp dirs so the tests never
/// depend on what is actually installed on the machine running them.
/// </summary>
public sealed class ExternalToolLocatorTests : IDisposable
{
    private readonly string _dir;
    private const string WiresharkExe = "Wireshark.exe";
    private const string VmwareExe = "vmware.exe";

    public ExternalToolLocatorTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cinder-tools-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* temp cleanup */ }
    }

    private string Touch(string fileName)
    {
        var path = Path.Combine(_dir, fileName);
        File.WriteAllText(path, "not a real program");
        return path;
    }

    private string TouchSub(string subDir, string fileName)
    {
        Directory.CreateDirectory(Path.Combine(_dir, subDir));
        return Touch(Path.Combine(subDir, fileName));
    }

    private string WriteToolsJson(string content)
    {
        var path = Path.Combine(_dir, "tools.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Nothing_installed_reports_not_found()
    {
        // Every machine-dependent layer is off, so the result cannot depend on what this
        // machine happens to have installed.
        var info = new ExternalToolLocator(
            probeRegistry: false, pathDirs: [_dir], useSystemPath: false, defaultRoots: [])
            .Locate("vmware");
        info.Found.Should().BeFalse();
        info.Source.Should().Be("");
        info.SourceLabel.Should().Be("未检测到");
    }

    [Fact]
    public void A_saved_override_wins_over_everything()
    {
        var pinned = Touch("pinned-wireshark.exe");
        var inPath = Touch(WiresharkExe);   // same dir also looks like a PATH hit
        var info = new ExternalToolLocator(
            overrides: new Dictionary<string, string> { ["wireshark"] = pinned },
            toolsJsonPath: Path.Combine(_dir, "missing.json"),
            probeRegistry: false,
            pathDirs: [_dir],
            useSystemPath: false,
            defaultRoots: []).Locate("wireshark");

        info.Found.Should().BeTrue();
        info.Path.Should().Be(pinned);
        info.Source.Should().Be("override");
    }

    [Fact]
    public void A_broken_override_falls_through_to_the_next_layer()
    {
        var inPath = Touch(WiresharkExe);
        var info = new ExternalToolLocator(
            overrides: new Dictionary<string, string> { ["wireshark"] = Path.Combine(_dir, "no-such-file.exe") },
            probeRegistry: false,
            pathDirs: [_dir],
            useSystemPath: false,
            defaultRoots: []).Locate("wireshark");

        info.Found.Should().BeTrue();
        info.Path.Should().Be(inPath);
        info.Source.Should().Be("path");
    }

    [Fact]
    public void Tools_json_beside_the_executable_is_honoured()
    {
        var exe = TouchSub("Wireshark", WiresharkExe);
        var toolsJson = WriteToolsJson($$"""{"wireshark": "{{_dir.Replace("\\", "\\\\")}}\\Wireshark"}""");
        var info = new ExternalToolLocator(
            toolsJsonPath: toolsJson, probeRegistry: false, pathDirs: []).Locate("wireshark");

        info.Found.Should().BeTrue();
        info.Path.Should().Be(exe);
        info.Source.Should().Be("tools.json");
    }

    [Fact]
    public void A_directory_candidate_gets_the_default_exe_appended()
    {
        var vmwareDir = Path.Combine(_dir, "VMware Workstation");
        Directory.CreateDirectory(vmwareDir);
        var exe = Path.Combine(vmwareDir, VmwareExe);
        File.WriteAllText(exe, "x");

        var info = new ExternalToolLocator(
            overrides: new Dictionary<string, string> { ["vmware"] = vmwareDir },
            probeRegistry: false, pathDirs: []).Locate("vmware");

        info.Found.Should().BeTrue();
        info.Path.Should().Be(exe);
    }

    [Fact]
    public void Unknown_tool_ids_report_not_found_without_throwing()
    {
        var info = new ExternalToolLocator(
            probeRegistry: false, useSystemPath: false, defaultRoots: []).Locate("no-such-tool");
        info.Found.Should().BeFalse();
        info.SourceLabel.Should().Be("未检测到");
    }

    [Fact]
    public void Malformed_tools_json_is_ignored_not_fatal()
    {
        var toolsJson = WriteToolsJson("not json at all {{{");
        var info = new ExternalToolLocator(
            toolsJsonPath: toolsJson, probeRegistry: false, pathDirs: [],
            useSystemPath: false, defaultRoots: []).Locate("wireshark");
        info.Found.Should().BeFalse();
    }

    [Fact]
    public void LocateAll_covers_the_whole_catalog()
    {
        var all = new ExternalToolLocator(probeRegistry: false, pathDirs: []).LocateAll();
        all.Should().HaveCount(ExternalToolDefinition.All.Count);
        all.Select(t => t.Id).Should().BeEquivalentTo(
            ["vmware", "wireshark", "volatility3", "ida", "x64dbg", "x32dbg", "ocr"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_candidates_resolve_to_nothing(string? candidate)
    {
        var def = ExternalToolDefinition.ById("wireshark")!;
        ExternalToolLocator.ResolveCandidate(candidate!, def).Should().BeNull();
    }

    [Fact]
    public void A_quoted_candidate_is_trimmed_before_resolution()
    {
        var exe = Touch(WiresharkExe);
        var def = ExternalToolDefinition.ById("wireshark")!;
        var resolved = ExternalToolLocator.ResolveCandidate($"\"{exe}\"", def);
        resolved.Should().Be(exe);
    }
}
