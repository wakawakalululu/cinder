namespace Cinder.App.ViewModels.Tools;

// =================================================================================
// The identity of every tool in the rail: id, title, one-line subtitle, and (for the tools
// that read a picked file or folder) the sentence shown before anything is loaded.
//
// Subtitles and hints are written for an examiner. Each is one sentence in sentence case
// that says what the tool shows or what to give it, as the tool works today. No library
// names, no implementation detail, no promises about future versions.
//
// [zh-cn] 简体中文汉化：Title/Subtitle/EmptyStateHint 及用户可见消息译为中文；
// Id/Kind/Icon/Phase 等标识符保持不变（代码依赖它们路由）。
// =================================================================================

// =================================================================================
// EXAMINE — one piece of evidence at a time（检查——逐个检材）
// =================================================================================

public sealed partial class HexTool : ToolViewModel
{
    public override string Id => "hex";
    public override string Title => "十六进制查看";
    public override string Icon => "0x";
    public override string Subtitle => "逐字节读取文件内容，支持搜索、跳转到偏移量，并解码光标所在位置的数值。";
    public override string Phase => "1";
    public override string Kind => "hex";
}

public sealed partial class StringsTool : ToolViewModel
{
    public override string Id => "strings";
    public override string Title => "字符串提取";
    public override string Icon => "Aa";
    public override string Subtitle => "列出任意文件中的可读文本，可按词语、正则表达式或标识符类型（邮箱/IP/卡号等）筛选。";
    public override string Phase => "1";
    public override string Kind => "strings";
}

public sealed partial class FilesystemTool : SidecarToolViewModel
{
    public override string Id => "filesystem";
    public override string Title => "文件系统";
    public override string Icon => "🗂";
    public override string Subtitle => "列出磁盘镜像内的文件与文件夹，包括 NTFS 上仍可找到的已删除条目。";
    public override string Phase => "3";
    public override string Kind => "filesystem";
    public override string EmptyStateHint => "选择一个磁盘或分区镜像：.dd、.raw、.E01、.aff4、.vhd、.vhdx 或 .iso。";
}

public sealed partial class RegistryTool : SidecarToolViewModel
{
    public override string Id => "registry";
    public override string Title => "注册表";
    public override string Icon => "📒";
    public override string Subtitle => "列出 Windows 注册表配置单元（hive）中的键和值，并显示每个键的最后写入时间。";
    public override string Phase => "4";
    public override string Kind => "registry";
    public override string EmptyStateHint => "选择一个注册表配置单元文件，如 NTUSER.DAT、SYSTEM、SOFTWARE、SAM 或 Amcache.hve。";
}

public sealed partial class EventLogTool : SidecarToolViewModel
{
    public override string Id => "evtx";
    public override string Title => "事件日志";
    public override string Icon => "📋";
    public override string Subtitle => "列出 Windows 事件日志文件中的事件：时间、事件 ID、来源、计算机、用户与描述。";
    public override string Phase => "4";
    public override string Kind => "evtx";
    public override string EmptyStateHint => "选择一个 .evtx 文件，通常位于 Windows\\System32\\winevt\\Logs。";
}

public sealed partial class PrefetchTool : SidecarToolViewModel
{
    public override string Id => "prefetch";
    public override string Title => "预取 Prefetch";
    public override string Icon => "▶";
    public override string Subtitle => "从 Windows 预取文件中显示哪些程序运行过、运行了多少次、何时运行。";
    public override string Phase => "4";
    public override string Kind => "prefetch";
    public override string EmptyStateHint => OperatingSystem.IsWindows()
        ? "选择 Prefetch 文件夹（Windows\\Prefetch）或单个 .pf 文件。"
        : "选择 Prefetch 文件夹（Windows\\Prefetch）或单个 .pf 文件。解析 Windows 10/11 的预取文件需要 Cinder 运行在 Windows 上。";
}

public sealed partial class ShellbagsTool : SidecarToolViewModel
{
    public override string Id => "shellbags";
    public override string Title => "Shellbags 外壳轨迹";
    public override string Icon => "🗄";
    public override string Subtitle => "显示用户在资源管理器中打开过哪些文件夹，包括已经不存在的文件夹。";
    public override string Phase => "4";
    public override string Kind => "shellbags";
    public override string EmptyStateHint => "选择用户的 NTUSER.DAT 或 UsrClass.dat 注册表配置单元。";
}

public sealed partial class JumplistsTool : SidecarToolViewModel
{
    public override string Id => "jumplists";
    public override string Title => "跳转列表";
    public override string Icon => "↗";
    public override string Subtitle => "从 Windows 跳转列表中显示用户最近在每个程序中打开过哪些文件和文件夹。";
    public override string Phase => "4";
    public override string Kind => "jumplists";
    public override string EmptyStateHint => "选择用户的 Recent\\AutomaticDestinations 或 Recent\\CustomDestinations 文件夹，或单个跳转列表文件。";
}

public sealed partial class LnkTool : SidecarToolViewModel
{
    public override string Id => "lnk";
    public override string Title => "LNK 快捷方式";
    public override string Icon => "🔗";
    public override string Subtitle => "读取 Windows 快捷方式文件：指向何处、目标文件的日期，以及创建时所用的驱动器和计算机。";
    public override string Phase => "4";
    public override string Kind => "lnk";
    public override string EmptyStateHint => "选择一个 .lnk 文件，或包含它们的文件夹（如用户的 Recent 文件夹）。";
}

public sealed partial class BrowserHistoryTool : SidecarToolViewModel
{
    public override string Id => "browser";
    public override string Title => "浏览器历史";
    public override string Icon => "🌐";
    public override string Subtitle => "列出 Chrome、Edge、Brave、Opera 和 Firefox 中访问过的网址，含最后访问时间与访问次数。";
    public override string Phase => "4";
    public override string Kind => "browser";
    public override string EmptyStateHint => "选择浏览器配置文件夹，或单个 History、places.sqlite 文件。";
}

public sealed partial class UsbHistoryTool : SidecarToolViewModel
{
    public override string Id => "usb";
    public override string Title => "USB 使用记录";
    public override string Icon => "🔌";
    public override string Subtitle => "列出 Windows SYSTEM 配置单元中记录的 USB 存储设备，含每个设备的序列号。";
    public override string Phase => "4";
    public override string Kind => "usb";
    public override string EmptyStateHint => "选择 SYSTEM 注册表配置单元（Windows\\System32\\config\\SYSTEM）。";
}

public sealed partial class WifiHistoryTool : SidecarToolViewModel
{
    public override string Id => "wifi";
    public override string Title => "网络历史";
    public override string Icon => "📶";
    public override string Subtitle => "列出 Windows 计算机接入过的无线与有线网络，含首次和最后连接时间。";
    public override string Phase => "4";
    public override string Kind => "wifi";
    public override string EmptyStateHint => "选择 SOFTWARE 注册表配置单元（Windows\\System32\\config\\SOFTWARE）。";
}

public sealed partial class SrumTool : SidecarToolViewModel
{
    public override string Id => "srum";
    public override string Title => "SRUM 资源监视";
    public override string Icon => "⏱";
    public override string Subtitle => "显示 Windows 为每个程序和用户记录的处理器占用、磁盘与网络流量以及电池读数。";
    public override string Phase => "4";
    public override string Kind => "srum";
    public override string EmptyStateHint => OperatingSystem.IsWindows()
        ? "选择 SRUDB.dat（Windows\\System32\\sru\\SRUDB.dat）。"
        : "选择 SRUDB.dat（Windows\\System32\\sru\\SRUDB.dat）。读取它需要 Cinder 运行在 Windows 上。";
}

public sealed partial class AmcacheTool : SidecarToolViewModel
{
    public override string Id => "amcache";
    public override string Title => "Amcache 执行缓存";
    public override string Icon => "🧾";
    public override string Subtitle => "列出 Windows 记录的程序文件，含路径、SHA-1 哈希、发布者和版本号。";
    public override string Phase => "4";
    public override string Kind => "amcache";
    public override string EmptyStateHint => "选择 Amcache.hve（Windows\\AppCompat\\Programs\\Amcache.hve）。";
}

public sealed partial class ShimcacheTool : SidecarToolViewModel
{
    public override string Id => "shimcache";
    public override string Title => "ShimCache 兼容缓存";
    public override string Icon => "🛡";
    public override string Subtitle => "列出 Windows 兼容性缓存中记录的程序文件，含每个文件的最后修改时间。";
    public override string Phase => "4";
    public override string Kind => "shimcache";
    public override string EmptyStateHint => "选择 SYSTEM 注册表配置单元（Windows\\System32\\config\\SYSTEM）。";
}

public sealed partial class RecycleBinTool : SidecarToolViewModel
{
    public override string Id => "recyclebin";
    public override string Title => "回收站";
    public override string Icon => "🗑";
    public override string Subtitle => "显示被放入回收站的内容：从哪里删除、大小、删除时间以及删除者。";
    public override string Phase => "4";
    public override string Kind => "recyclebin";
    public override string EmptyStateHint => "选择 $Recycle.Bin 文件夹、其中某个用户的文件夹，或单个 $I 文件。";
}

public sealed partial class LinuxArtifactsTool : SidecarToolViewModel
{
    public override string Id => "linux";
    public override string Title => "Linux 痕迹";
    public override string Icon => "🐧";
    public override string Subtitle => "从 Linux 系统中读取登录记录、系统与登录日志、shell 历史和账户文件。";
    public override string Phase => "5";
    public override string Kind => "linux";
    public override string EmptyStateHint => "选择已挂载 Linux 文件系统或应急采集包的顶层文件夹，或单个日志文件。";
}

public sealed partial class MemoryTool : SidecarToolViewModel
{
    public override string Id => "memory";
    public override string Title => "内存取证";
    public override string Icon => "🧠";
    public override string Subtitle => "列出捕获 Windows 内存镜像时正在运行的进程，以父进程与子进程树的形式呈现。";
    public override string Phase => "7";
    public override string Kind => "memory";
    public override string EmptyStateHint => "选择一个 Windows 内存镜像（.raw、.dmp 或 .vmem）。需要已安装 Volatility 3。";
    public override IReadOnlyList<string> RequiredPythonPackages => ["volatility3"];

    protected override Task<ParseResult> ParseAsync(string evidencePath, CancellationToken ct) => Task.Run(async () =>
    {
        if (!await Cinder.App.Services.Vol3Runner.IsAvailableAsync(ct))
        {
            throw new InvalidOperationException(
                "未找到 Volatility 3。内存取证需要装有 volatility3 包的 Python 3。" +
                "请先执行：  python -m pip install volatility3  ，然后重试。");
        }

        var pstree = await Cinder.App.Services.Vol3Runner.RunAsync(evidencePath, "windows.pstree.PsTree", ct);
        if (pstree.Error is not null)
        {
            // A failed run is an error, not a one-row result.
            throw new InvalidOperationException(pstree.Error);
        }
        var rows = ToRows(pstree.Rows);
        return new ParseResult(rows, false, null,
            rows.Count == 0 ? "Volatility 已运行但未列出任何进程。该镜像可能不是 Windows 内存镜像。" : null);
    }, ct);

    /// <summary>
    /// Maps flattened pstree records to grid rows. The process name is indented by its depth in
    /// the tree, and the depth is also its own column so it survives sorting and export.
    /// </summary>
    internal static List<object> ToRows(IReadOnlyList<IDictionary<string, object?>> records)
    {
        var rows = new List<object>(records.Count);
        foreach (var r in records)
        {
            var depth = (int)TryInt(r, Cinder.App.Services.Vol3Runner.DepthKey);
            rows.Add(new
            {
                Pid = TryInt(r, "PID"),
                ParentPid = TryInt(r, "PPID"),
                Depth = depth,
                Name = new string(' ', depth * 2) + TryStr(r, "ImageFileName"),
                Threads = TryInt(r, "Threads"),
                Handles = TryInt(r, "Handles"),
                Created = TryStr(r, "CreateTime"),
                Exited = TryStr(r, "ExitTime"),
                Path = TryStr(r, "Path"),
                CommandLine = TryStr(r, "Cmd"),
            });
        }
        return rows;
    }

    private static long TryInt(IDictionary<string, object?> d, string k) =>
        d.TryGetValue(k, out var v) ? v switch { long l => l, int i => i, _ => 0 } : 0;

    private static string TryStr(IDictionary<string, object?> d, string k)
        => d.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
}

public sealed partial class NetworkTool : SidecarToolViewModel
{
    public override string Id => "network";
    public override string Title => "网络流量 (PCAP)";
    public override string Icon => "📡";
    public override string Subtitle => "列出抓包文件中的数据包，含时间、地址、端口、协议与 TCP 标志位。";
    public override string Phase => "10";
    public override string Kind => "network";
    public override string EmptyStateHint => OperatingSystem.IsWindows()
        ? "选择 .pcap 或 .pcapng 抓包文件。需要已安装 Npcap。"
        : "选择 .pcap 或 .pcapng 抓包文件。需要已安装 libpcap。";
}

public sealed partial class MobileTool : SidecarToolViewModel
{
    public override string Id => "mobile";
    public override string Title => "手机备份";
    public override string Icon => "📱";
    public override string Subtitle => "列出 iPhone/iPad 备份文件夹或 Android .ab 备份文件中包含的文件。";
    public override string Phase => "10";
    public override string Kind => "mobile";
    public override string EmptyStateHint => "选择 iPhone/iPad 备份文件夹（含 Manifest.db 的那个）或 Android .ab 文件。";
}

public sealed partial class EmailTool : SidecarToolViewModel
{
    public override string Id => "email";
    public override string Title => "邮件";
    public override string Icon => "✉";
    public override string Subtitle => "列出邮件文件中的邮件，含发件人、收件人、主题与发送时间。";
    public override string Phase => "4";
    public override string Kind => "email";
    public override string EmptyStateHint => "选择 .msg、.eml 或 .mbox 文件。Outlook 的 .pst/.ost 文件还需要可选的 libpff-python 包。";
}

public sealed partial class DocumentsTool : ToolViewModel
{
    public override string Id => "documents";
    public override string Title => "文档解析";
    public override string Icon => "📄";
    public override string Subtitle => "读取 PDF、Word、Excel、PowerPoint 或 OpenDocument 文件中的文本，无需用对应程序打开。";
    public override string Phase => "1";
    public override string Kind => "documents";
}

// =================================================================================
// ANALYZE — across evidence（分析——贯穿全部检材）
// =================================================================================

public sealed partial class TimelineTool : ToolViewModel
{
    public override string Id => "timeline";
    public override string Title => "超级时间线";
    public override string Icon => "⏳";
    public override string Subtitle => "把一个检材文件夹中所有带时间戳的事件合并到一个列表，可按日期、来源、用户、文本或 ATT&CK 技术筛选。";
    public override string Phase => "6";
    public override string Kind => "timeline";
}

public sealed partial class MapTool : ToolViewModel
{
    public override string Id => "map";
    public override string Title => "GPS 地图";
    public override string Icon => "🗺";
    public override string Subtitle => "列出一组照片中记录的 GPS 位置，含时间及其来源时钟。";
    public override string Phase => "6";
    public override string Kind => "map";
}

public sealed partial class GraphTool : ToolViewModel
{
    public override string Id => "graph";
    public override string Title => "通信关系图";
    public override string Icon => "🕸";
    public override string Subtitle => "显示一组邮件文件中谁给谁写过信，联系最多的人排在最前。";
    public override string Phase => "6";
    public override string Kind => "graph";
}

public sealed partial class SearchTool : ToolViewModel
{
    public override string Id => "search";
    public override string Title => "全文搜索";
    public override string Icon => "🔎";
    public override string Subtitle => "对文件夹中的文件建立一次索引，然后同时搜索其中所有文件内的文本。";
    public override string Phase => "6";
    public override string Kind => "search";
}

public sealed partial class HashSetsTool : ToolViewModel
{
    public override string Id => "hashsets";
    public override string Title => "哈希样本集";
    public override string Icon => "#";
    public override string Subtitle => "维护一个已知文件哈希数据库，查询某个哈希以判断它是已知安全还是已知可疑。";
    public override string Phase => "6";
    public override string Kind => "hashsets";
}

public sealed partial class YaraTool : ToolViewModel
{
    public override string Id => "yara";
    public override string Title => "YARA 扫描";
    public override string Icon => "🔬";
    public override string Subtitle => "用基于文本和十六进制字符串的 YARA 规则扫描文件；规则语言的其他部分会被跳过并报告。";
    public override string Phase => "6";
    public override string Kind => "yara";
}

public sealed partial class VirusTotalTool : ToolViewModel
{
    public override string Id => "virustotal";
    public override string Title => "VirusTotal 查询";
    public override string Icon => "VT";
    public override string Subtitle => "用你自己的 API key 在 VirusTotal 上查询文件哈希；文件内容绝不外传。";
    public override string Phase => "6";
    public override string Kind => "virustotal";
}

public sealed partial class AiCopilotTool : ToolViewModel
{
    public override string Id => "ai";
    public override string Title => "AI 助手";
    public override string Icon => "✨";
    public override string Subtitle => "与你自备的语言模型对话：Ollama、LM Studio 或任何 OpenAI 兼容服务。";
    public override string Phase => "9";
    public override string Kind => "ai";
}

// =================================================================================
// ACQUIRE（采集）
// =================================================================================

public sealed partial class ImagerTool : ToolViewModel
{
    public override string Id => "imager";
    public override string Title => "磁盘镜像采集";
    public override string Icon => "💽";
    public override string Subtitle => "把磁盘或现有镜像复制为 raw、E01、AFF4、VHD 或 VHDX 镜像文件，读取每个字节时同步计算哈希。";
    public override string Phase => "2";
    public override string Kind => "imager";
}

public sealed partial class VerifyTool : ToolViewModel
{
    public override string Id => "verify";
    public override string Title => "镜像校验";
    public override string Icon => "✓";
    public override string Subtitle => "重新读取镜像，并将哈希与其制作时记录的哈希比对。";
    public override string Phase => "2";
    public override string Kind => "verify";
}

public sealed partial class MountTool : ToolViewModel
{
    public override string Id => "mount";
    public override string Title => "挂载镜像";
    public override string Icon => "📂";
    public override string Subtitle => "把磁盘镜像以只读方式挂载到本机，供其他程序浏览。";
    public override string Phase => "2";
    public override string Kind => "mount";
}

public sealed partial class ConvertTool : ToolViewModel
{
    public override string Id => "convert";
    public override string Title => "格式转换";
    public override string Icon => "↔";
    public override string Subtitle => "在 raw、E01、AFF4、VHD 和 VHDX 之间转换镜像，并校验副本与源文件哈希一致。";
    public override string Phase => "2";
    public override string Kind => "convert";
}

public sealed partial class ShadowCopyTool : ToolViewModel
{
    public override string Id => "shadowcopy";
    public override string Title => "卷影副本";
    public override string Icon => "📸";
    public override string Subtitle => "列出本机上存在的卷快照。";
    public override string Phase => "2";
    public override string Kind => "shadowcopy";
}

public sealed partial class RamCaptureTool : ToolViewModel
{
    public override string Id => "ramcapture";
    public override string Title => "RAM 采集";
    public override string Icon => "🧪";
    public override string Subtitle => "采集本机内存的功能尚未提供；本页说明可用哪些其他程序来完成。";
    public override string Phase => "7";
    public override string Kind => "ramcapture";
}

public sealed partial class CarverTool : ToolViewModel
{
    public override string Id => "carver";
    public override string Title => "文件雕刻";
    public override string Icon => "🪓";
    public override string Subtitle => "按文件签名从镜像中恢复文件，包括已删除空间和未使用空间中的文件。";
    public override string Phase => "3";
    public override string Kind => "carver";
}

public sealed partial class CloudPullTool : ToolViewModel
{
    public override string Id => "cloud";
    public override string Title => "云盘拉取";
    public override string Icon => "☁";
    public override string Subtitle => "从 Google Drive、OneDrive 或 Dropbox 下载的功能尚未提供；本页只生成登录链接。";
    public override string Phase => "10";
    public override string Kind => "cloud";
}

// =================================================================================
// CASE（案件管理）
// =================================================================================

public sealed partial class CasesTool : ToolViewModel
{
    public override string Id => "cases";
    public override string Title => "案件";
    public override string Icon => "📁";
    public override string Subtitle => "创建新的案件文件，或重新打开最近处理过的案件。";
    public override string Phase => "8";
    public override string Kind => "cases";
}

public sealed partial class ReportsTool : ToolViewModel
{
    public override string Id => "reports";
    public override string Title => "报告";
    public override string Icon => "📝";
    public override string Subtitle => "从模板撰写报告，把加书签的发现作为附件加入，并导出为 Markdown、HTML、PDF、Word 或 JSON。";
    public override string Phase => "8";
    public override string Kind => "reports";
}

public sealed partial class CustodyTool : ToolViewModel
{
    public override string Id => "custody";
    public override string Title => "监管链";
    public override string Icon => "🔐";
    public override string Subtitle => "查看案件的监管链日志，核验没有条目被篡改，并对最新条目签名。";
    public override string Phase => "0";
    public override string Kind => "custody";
}

public sealed partial class WorkflowsTool : ToolViewModel
{
    public override string Id => "workflows";
    public override string Title => "工作流";
    public override string Icon => "🔁";
    public override string Subtitle => "从工作流文件运行一串保存好的步骤，例如哈希、列文件、雕刻和出报告。";
    public override string Phase => "0";
    public override string Kind => "workflows";
}

public sealed partial class PluginsTool : ToolViewModel
{
    public override string Id => "plugins";
    public override string Title => "插件";
    public override string Icon => "🧩";
    public override string Subtitle => "从插件文件夹加载附加工具。";
    public override string Phase => "0";
    public override string Kind => "plugins";
}

public sealed partial class SettingsTool : ToolViewModel
{
    public override string Id => "settings";
    public override string Title => "设置";
    public override string Icon => "⚙";
    public override string Subtitle => "更改主题、哈希样本集数据库、监管链时间戳服务和 AI 模型。";
    public override string Phase => "0";
    public override string Kind => "settings";
}
