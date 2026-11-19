using System.Runtime.Versioning;
using System.Text.Json;

namespace Cinder.App.Services;

/// <summary>
/// One externally installed program Cinder can detect, launch or hand evidence to:
/// VMware Workstation (镜像仿真), Wireshark (流量), Volatility 3 (内存), IDA Pro / x64dbg /
/// x32dbg (逆向), Tesseract (OCR). Detection is read-only: nothing is installed or downloaded.
/// </summary>
public sealed record ExternalToolDefinition(
    string Id,
    string DisplayName,
    string Purpose,
    string DefaultExeName,
    Func<string, string?> DescribeSubPath)   // given the tool's install dir, the exe inside it
{
    /// <summary>The catalog, in display order.</summary>
    public static IReadOnlyList<ExternalToolDefinition> All { get; } =
    [
        new("vmware", "VMware Workstation Pro", "镜像仿真取证：把磁盘镜像挂入虚拟机开机检查", "vmware.exe", dir => Path.Combine(dir, "vmware.exe")),
        new("wireshark", "Wireshark", "网络流量取证：打开 pcap/pcapng 抓包文件", "Wireshark.exe", dir => Path.Combine(dir, "Wireshark.exe")),
        new("volatility3", "Volatility 3", "内存取证：解析内存镜像中的进程、网络与注入", "vol.exe", dir => Path.Combine(dir, "vol.exe")),
        // IDA 9 起主程序叫 ida.exe（不再有 ida64.exe）；旧版本叫 ida64.exe。目录候选两种都认。
        new("ida", "IDA Pro", "静态逆向：反编译 PE/ELF 与固件样本", "ida64.exe",
            dir => File.Exists(Path.Combine(dir, "ida.exe")) ? Path.Combine(dir, "ida.exe") : Path.Combine(dir, "ida64.exe")),
        new("x64dbg", "x64dbg", "动态逆向：64 位程序调试", "x64dbg.exe", dir => Path.Combine(dir, "x64dbg.exe")),
        new("x32dbg", "x32dbg", "动态逆向：32 位程序调试", "x32dbg.exe", dir => Path.Combine(dir, "x32dbg.exe")),
        new("ocr", "Tesseract OCR", "AI/MCP 辅助：图片与扫描件文字识别", "tesseract.exe", dir => Path.Combine(dir, "tesseract.exe")),
    ];

    public static ExternalToolDefinition? ById(string id) => All.FirstOrDefault(t => t.Id == id);
}

/// <summary>The result of looking for one external tool.</summary>
public sealed record ExternalToolInfo(string Id, string? Path, string Source)
{
    public bool Found => !string.IsNullOrEmpty(Path);
    /// <summary>How the tool was found, in the examiner's words.</summary>
    public string SourceLabel => Source switch
    {
        "override" => "手动指定",
        "tools.json" => "程序目录 tools.json",
        "registry" => "注册表",
        "path" => "系统 PATH",
        "default" => "默认安装路径",
        _ => "未检测到",
    };
}

/// <summary>
/// Finds external programs in layers, first match wins:
/// ① the examiner's saved override (settings.json) → ② a <c>tools.json</c> file sitting next
/// to Cinder.exe (portable deployments pin their own tools there) → ③ the Windows registry /
/// App Paths → ④ the system PATH (via <see cref="Cinder.Core.Diagnostics.ExecutableResolver"/>)
/// → ⑤ well-known default install paths. Pure file probing: no installs, no downloads.
/// </summary>
public sealed class ExternalToolLocator
{
    private readonly IReadOnlyDictionary<string, string> _overrides;
    private readonly string? _toolsJsonPath;
    private readonly bool _probeRegistry;
    private readonly bool _useSystemPath;
    private readonly IReadOnlyList<string>? _pathDirs;
    private readonly IReadOnlyList<string>? _defaultRoots;

    /// <summary>
    /// The switches exist so tests can pin every layer down: turn off the registry, the system
    /// PATH and the machine's default install roots, then inject temp dirs for the rest.
    /// </summary>
    public ExternalToolLocator(
        IReadOnlyDictionary<string, string>? overrides = null,
        string? toolsJsonPath = null,
        bool probeRegistry = true,
        IReadOnlyList<string>? pathDirs = null,
        bool useSystemPath = true,
        IReadOnlyList<string>? defaultRoots = null)
    {
        _overrides = overrides ?? new Dictionary<string, string>();
        _toolsJsonPath = toolsJsonPath ?? DefaultToolsJsonPath();
        _probeRegistry = probeRegistry;
        _useSystemPath = useSystemPath;
        _pathDirs = pathDirs;
        _defaultRoots = defaultRoots;
    }

    /// <summary>tools.json sits beside the executable; deployments write it, never the app.</summary>
    public static string? DefaultToolsJsonPath()
    {
        try
        {
            return Path.Combine(AppContext.BaseDirectory, "tools.json");
        }
        catch
        {
            return null;
        }
    }

    public ExternalToolInfo Locate(string toolId)
    {
        var def = ExternalToolDefinition.ById(toolId);
        if (def is null)
        {
            return new ExternalToolInfo(toolId, null, "");
        }

        // ① Manual override saved in settings.
        if (_overrides.TryGetValue(toolId, out var pinned) && !string.IsNullOrWhiteSpace(pinned))
        {
            var resolved = ResolveCandidate(pinned, def);
            if (resolved is not null)
            {
                return new ExternalToolInfo(toolId, resolved, "override");
            }
        }

        // ② tools.json beside the executable.
        foreach (var pinned2 in ReadToolsJson(_toolsJsonPath, toolId))
        {
            var resolved2 = ResolveCandidate(pinned2, def);
            if (resolved2 is not null)
            {
                return new ExternalToolInfo(toolId, resolved2, "tools.json");
            }
        }

        // ③ Registry (Windows only; quietly skipped elsewhere).
        if (_probeRegistry && OperatingSystem.IsWindows())
        {
            foreach (var reg in ProbeRegistry(toolId))
            {
                var resolved3 = ResolveCandidate(reg, def);
                if (resolved3 is not null)
                {
                    return new ExternalToolInfo(toolId, resolved3, "registry");
                }
            }
        }

        // ④ Explicit search dirs first (tests / callers with their own layout), then the system PATH.
        foreach (var dir in _pathDirs ?? Array.Empty<string>())
        {
            var candidate = Path.Combine(dir, def.DefaultExeName);
            if (File.Exists(candidate))
            {
                return new ExternalToolInfo(toolId, candidate, "path");
            }
        }
        // ④ Explicit search dirs first (tests / callers with their own layout), then the system PATH.
        foreach (var dir in _pathDirs ?? Array.Empty<string>())
        {
            var candidate = Path.Combine(dir, def.DefaultExeName);
            if (File.Exists(candidate))
            {
                return new ExternalToolInfo(toolId, candidate, "path");
            }
        }
        if (_useSystemPath)
        {
            var fromPath = Cinder.Core.Diagnostics.ExecutableResolver.Resolve(def.DefaultExeName);
            if (fromPath is not null)
            {
                return new ExternalToolInfo(toolId, fromPath, "path");
            }
        }

        // ⑤ Well-known default install locations.
        foreach (var root in _defaultRoots ?? DefaultRoots())
        {
            var candidate = Path.Combine(root, def.DefaultExeName);
            if (File.Exists(candidate))
            {
                return new ExternalToolInfo(toolId, candidate, "default");
            }
        }

        return new ExternalToolInfo(toolId, null, "");
    }

    public IReadOnlyList<ExternalToolInfo> LocateAll() =>
        ExternalToolDefinition.All.Select(d => Locate(d.Id)).ToList();

    // ============================================================ layers ========

    /// <summary>Turns a candidate into a real executable: a file is used as-is; a directory gets the tool's own exe appended.</summary>
    internal static string? ResolveCandidate(string candidate, ExternalToolDefinition def)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }
            var expanded = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
            var path = Directory.Exists(expanded) ? def.DescribeSubPath(expanded) : expanded;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return null;
        }
    }

    /// <summary>Reads tools.json: {"tool-id": "path-or-dir", …}. One tool id per line; malformed file → nothing.</summary>
    internal static IReadOnlyList<string> ReadToolsJson(string? path, string toolId)
    {
        try
        {
            if (path is null || !File.Exists(path))
            {
                return Array.Empty<string>();
            }
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty(toolId, out var value))
            {
                return Array.Empty<string>();
            }
            return value.ValueKind == JsonValueKind.String
                ? [value.GetString() ?? ""]
                : (IReadOnlyList<string>)Array.Empty<string>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Registry probes per tool: uninstall entries, vendor keys and App Paths. Every probe is
    /// best-effort — a missing key or denied hive returns nothing rather than throwing.
    /// Only called from the OperatingSystem.IsWindows() branch in <see cref="Locate"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static IEnumerable<string> ProbeRegistry(string toolId)
    {
        foreach (var value in RegistryValues(toolId))
        {
            yield return value;
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> RegistryValues(string toolId)
    {
        // Each entry: (hive view, key path, value name or null for the default value).
        var probes = toolId switch
        {
            "vmware" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\WOW6432Node\VMware, Inc.\VMware Workstation", "InstallPath"),
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\VMware, Inc.\VMware Workstation", "InstallPath"),
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vmware.exe", null),
            },
            "wireshark" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Wireshark", "InstallLocation"),
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Wireshark", "InstallLocation"),
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\wireshark.exe", null),
            },
            "ida" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ida64.exe", null),
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Hex-Rays IDA", "InstallPath"),
            },
            "x64dbg" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\x64dbg.exe", null),
            },
            "x32dbg" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\x32dbg.exe", null),
            },
            "ocr" => new (Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)[]
            {
                (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\Tesseract-OCR", null),
            },
            // volatility3 is a Python package: no install path to read from the registry.
            _ => Array.Empty<(Microsoft.Win32.RegistryKey? Hive, string Key, string? Value)>(),
        };

        foreach (var (hive, key, valueName) in probes)
        {
            string? result = null;
            try
            {
                using var k = hive?.OpenSubKey(key);
                if (k is not null)
                {
                    result = (valueName is null ? k.GetValue(null) : k.GetValue(valueName)) as string;
                    // App Paths entries are exe paths; InstallPath keys point at a folder —
                    // ResolveCandidate appends the default exe for folders either way.
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or IOException or ArgumentException)
            {
                result = null;
            }
            if (!string.IsNullOrWhiteSpace(result))
            {
                yield return result;
            }
        }
    }

    /// <summary>Well-known install roots searched for the tool's own executable.</summary>
    private static IEnumerable<string> DefaultRoots()
    {
        var roots = new List<string>();
        foreach (var baseDir in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                 })
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                continue;
            }
            roots.Add(baseDir);
            // Tools that ship inside a named subfolder, e.g. %ProgramFiles%\Wireshark\Wireshark.exe.
            foreach (var sub in new[] { "Wireshark", "VMware\\VMware Workstation", "Tesseract-OCR",
                                        "IDA Pro 9.0", "IDA Pro 8.4", "IDA Pro 8.3", "IDA Pro 7.7",
                                        "Hex-Rays\\IDA Pro", "x64dbg\\release\\x64", "x64dbg\\release\\x32" })
            {
                roots.Add(Path.Combine(baseDir, sub));
            }
        }
        return roots;
    }
}
