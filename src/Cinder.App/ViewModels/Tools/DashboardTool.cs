using System.Collections.ObjectModel;
using Cinder.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cinder.App.ViewModels.Tools;

/// <summary>
/// The home screen. First thing the user sees when Cinder launches. Surfaces recent cases,
/// recent evidence, suggested next steps for beginners, and quick-action buttons that just
/// invoke the same commands as the title bar / menu / palette.
/// </summary>
public sealed partial class DashboardTool : ToolViewModel
{
    public override string Id => "dashboard";
    public override string Title => "主页";
    public override string Icon => "⌂";
    public override string Subtitle => "重新打开最近的案件或文件、开始新案件，并阅读工具如何配合使用的简短指引。";
    public override string Phase => "0";
    public override string Kind => "dashboard";

    public override string HelpMarkdown => """
## 这是什么
Cinder 启动时显示的主页。在这里可以新建案件、重新打开最近的案件或最近打开过的文件、
不建案直接打开单个文件，或阅读简短的"首次使用？"指引。

## 工作方式
最近案件和最近文件会列出来，方便一键回到它们。快捷操作覆盖了常见的开始方式：新建案件、
打开案件、打开文件。指引描述了大多数取证工作遵循的顺序：案件 → 检材 → 工具 → 时间线 → 报告。

## 需要知道
不建案也可以查看文件。但书签和监管链日志需要案件：没有打开的案件时，任何操作都不会被记录。
""";

    public ObservableCollection<RecentCaseRow> RecentCases { get; } = new();
    public ObservableCollection<RecentEvidenceRow> RecentEvidence { get; } = new();
    public ObservableCollection<GuideStep> Guide { get; } = new()
    {
        new("1", "新建案件",
            "案件是一个文件，存放你的书签和操作日志。" +
            "点击\"新建案件\"并选择保存位置。没有打开的案件时，" +
            "任何操作都不会被记录，也无法为结果行添加书签。"),
        new("2", "打开检材",
            "Cinder 不会把检材导入案件。每个工具都会让你选择它要读取的文件或" +
            "文件夹：磁盘镜像、注册表配置单元、事件日志、导出文件" +
            "的文件夹。\"打开文件\"可以在十六进制查看器中查看任意文件。"),
        new("3", "为检材选择合适的工具",
            "左侧工具栏按你要做的事分组：检查（查看单个检材）、分析（跨检材分析）、" +
            "采集（制作或校验镜像）、案件管理（书签、报告和监管链日志）。"),
        new("4", "给关键发现加书签并建立时间线",
            "在任何工具中选中一行并选择\"加入书签\"即可保存为发现。" +
            "在超级时间线中，\"导入文件夹\"会把一个检材文件夹中带时间戳的事件" +
            "按时间顺序读入一个列表。"),
        new("5", "导出报告",
            "报告工具让你按模板撰写各章节，把书签自动编号为附件（Exhibit），并导出 " +
            "Markdown、HTML、PDF、Word 或 JSON。每次导出都会记录到案件的监管链日志；" +
            "日志本身可在\"监管链\"中查看和校验。"),
    };

    /// <summary>True once at least one recent case/evidence row exists, used to swap empty states.</summary>
    public bool HasHistory => RecentCases.Count > 0 || RecentEvidence.Count > 0;

    private RecentsStore? _store;

    /// <summary>The case that is open, so Home can offer the next steps for it. Null when none is.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOpenCase))]
    private CaseSession? _openCase;

    public bool HasOpenCase => OpenCase is not null;

    /// <summary>Goes to a tool by its kind id. The buttons on the open-case panel use this.</summary>
    [RelayCommand]
    private static void ShowTool(string? kind)
    {
        if (!string.IsNullOrEmpty(kind))
        {
            AppNavigator.ShowTool?.Invoke(kind);
        }
    }

    public DashboardTool()
    {
        ActiveCaseContext.Changed += session => OpenCase = session;
        OpenCase = ActiveCaseContext.Current;
        RecentCases.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
        RecentEvidence.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
    }

    /// <summary>
    /// Attach a backing <see cref="RecentsStore"/> and hydrate from disk. Subsequent
    /// NoteCaseOpened / NoteEvidenceOpened / Clear calls persist back automatically.
    /// </summary>
    public void AttachStore(RecentsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        var snap = store.Load();
        RecentCases.Clear();
        foreach (var c in snap.Cases)
        {
            RecentCases.Add(new RecentCaseRow(c.Id, c.Name, c.Examiner, c.Path, c.OpenedUtc));
        }
        RecentEvidence.Clear();
        foreach (var e in snap.Evidence)
        {
            var name = System.IO.Path.GetFileName(e.Path);
            if (string.IsNullOrEmpty(name))
            {
                name = e.Path;
            }
            RecentEvidence.Add(new RecentEvidenceRow(e.Path, name, e.OpenedUtc));
        }
        OnPropertyChanged(nameof(HasHistory));
    }

    private void Persist()
    {
        _store?.Save(new RecentsSnapshot
        {
            Cases = RecentCases
                .Select(r => new RecentCaseEntry(r.Id, r.Name, r.Examiner, r.Path ?? "", r.OpenedUtc))
                .ToList(),
            Evidence = RecentEvidence
                .Select(r => new RecentEvidenceEntry(r.Path, r.OpenedUtc))
                .ToList(),
        });
    }

    /// <summary>
    /// Push a "just opened" case onto the top of the recent list. The <paramref name="path"/>
    /// is the .cinder file path; without it we can't reopen the case from the dashboard.
    /// </summary>
    public void NoteCaseOpened(Guid id, string name, string? examiner, string? path)
    {
        var existing = RecentCases.FirstOrDefault(r => r.Id == id);
        if (existing is not null)
        {
            RecentCases.Remove(existing);
        }
        RecentCases.Insert(0, new RecentCaseRow(id, name, examiner ?? "", path, DateTimeOffset.UtcNow));
        while (RecentCases.Count > 8)
        {
            RecentCases.RemoveAt(RecentCases.Count - 1);
        }
        Persist();
    }

    public void NoteEvidenceOpened(string path)
    {
        var existing = RecentEvidence.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentEvidence.Remove(existing);
        }
        var name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
        {
            name = path;
        }
        RecentEvidence.Insert(0, new RecentEvidenceRow(path, name, DateTimeOffset.UtcNow));
        while (RecentEvidence.Count > 8)
        {
            RecentEvidence.RemoveAt(RecentEvidence.Count - 1);
        }
        Persist();
    }

    [RelayCommand]
    private void Clear()
    {
        RecentCases.Clear();
        RecentEvidence.Clear();
        Persist();
    }
}

/// <summary>
/// One row in the Dashboard's "recent cases" pane. <see cref="Path"/> is the .cinder file
/// path — required to reopen the case from the dashboard.
/// </summary>
public sealed record RecentCaseRow(Guid Id, string Name, string Examiner, string? Path, DateTimeOffset OpenedUtc)
{
    public string OpenedDisplay => OpenedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>True only when we have a path to reopen from — drives IsEnabled on the click button.</summary>
    public bool CanReopen => !string.IsNullOrEmpty(Path);
}

/// <summary>One row in the Dashboard's "recent evidence" pane.</summary>
public sealed record RecentEvidenceRow(string Path, string FileName, DateTimeOffset OpenedUtc)
{
    public string OpenedDisplay => OpenedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

/// <summary>One step in the "first time?" guide on the Dashboard.</summary>
public sealed record GuideStep(string Number, string Title, string Body);
