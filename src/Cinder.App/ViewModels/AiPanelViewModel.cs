using System.Collections.ObjectModel;
using System.Text;
using Cinder.AI;
using Cinder.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cinder.App.ViewModels;

/// <summary>
/// The docked AI panel (right side of the shell, beside the evidence). Unlike the AI Copilot
/// tool page, the panel sees the current examination context: the file open in the Hex viewer,
/// the row selected in the current parser grid, the first rows of that grid, and the case's
/// bookmarks. The examiner picks which of these to attach; the prompt carries structured facts
/// (parsed rows, digests, bookmark notes) — never raw bytes.
/// </summary>
public sealed partial class AiPanelViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _shell;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(2) };
    private readonly SettingsStore _settings = new();

    public ObservableCollection<ChatMessageVm> Messages { get; } = new();

    [ObservableProperty] private string _input = "";
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private string? _statusLine;

    // ---- context switches -------------------------------------------------------------
    [ObservableProperty] private bool _includeFileInfo = true;
    [ObservableProperty] private bool _includeSelectedRow = true;
    [ObservableProperty] private bool _includeGridRows = true;
    [ObservableProperty] private bool _includeBookmarks;

    /// <summary>How many grid rows the context may carry.</summary>
    [ObservableProperty] private int _gridRowLimit = 40;

    /// <summary>The rendered context as it will be sent — so the examiner can see exactly what leaves.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContextPreview))]
    private string? _contextPreview;

    public bool HasContextPreview => !string.IsNullOrEmpty(ContextPreview);

    // ---- model quick switch -------------------------------------------------------------
    [ObservableProperty] private string? _model;
    public ObservableCollection<string> ModelOptions { get; } = new();
    [ObservableProperty] private bool _isFetchingModels;

    [ObservableProperty]
    private string _systemPrompt =
        "你是数字取证助手，服务于一场取证竞赛。回答规则：只依据对话中给出的结构化上下文与问题作答；" +
        "上下文里没有的信息就明确说“上下文中没有”，不要编造；结论尽量给要点并引用键名、时间戳、路径等原文依据；" +
        "涉及下一步排查方向时给出具体到工具与筛选项的建议。回答使用简体中文。";

    public AiPanelViewModel(MainWindowViewModel shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        try
        {
            var s = _settings.Load();
            if (s.AiProvider.TryGetValue("model", out var m) && !string.IsNullOrEmpty(m))
            {
                Model = m;
            }
        }
        catch
        {
            // 首次启动没有设置文件：保持空模型，发送时会提示去设置。
        }
    }

    /// <summary>Called by the shell when the panel opens or the selected tool changes.</summary>
    public void RefreshContext() => ContextPreview = BuildCurrentContext();

    /// <summary>The panel's own "刷新上下文预览" button.</summary>
    [RelayCommand]
    private void RefreshContextPreview() => RefreshContext();

    /// <summary>
    /// Assembles the context request from the shell's live state: the hex buffer's file identity,
    /// the selected tool's selected row and grid rows, and the open case's bookmarks. Every part
    /// honours its toggle; a part with no data is simply left out.
    /// </summary>
    internal AiContextRequest BuildContextRequest()
    {
        string? fileInfo = null;
        var hex = _shell.Hex;
        if (IncludeFileInfo && hex?.SourcePath is { } path)
        {
            fileInfo = $"{hex.SizeDisplay}";
            if (hex.QuickHashSha256Full is { } sha)
            {
                fileInfo += $" · SHA-256 {sha}";
            }
            else
            {
                fileInfo += " · SHA-256 尚未计算（Hex 工具内 Ctrl+D 菜单可算）";
            }
        }

        var tool = _shell.Workspace.SelectedTool;
        string? selectedRow = null;
        IReadOnlyList<object>? rows = null;
        var total = 0;

        if (tool is Tools.SidecarToolViewModel sidecar)
        {
            if (IncludeSelectedRow)
            {
                selectedRow = sidecar.SelectedRow is null
                    ? null
                    : AiContextBuilder.RenderRow(sidecar.SelectedRow);
            }
            if (IncludeGridRows && sidecar.VisibleRows.Count > 0)
            {
                rows = sidecar.VisibleRows;
                total = sidecar.Rows.Count;
            }
        }

        IReadOnlyList<(string Title, string? Note, string? Tool)>? bookmarks = null;
        if (IncludeBookmarks &&
            _shell.Workspace.FindByKind("bookmarks") is Tools.BookmarksTool bt &&
            bt.Bookmarks.Count > 0)
        {
            bookmarks = bt.Bookmarks.Select(b => (b.Title, b.Note, (string?)b.Tool)).ToList();
        }

        var evidencePath = IncludeFileInfo ? hex?.SourcePath : null;
        return new AiContextRequest(
            ToolTitle: tool?.Title,
            EvidencePath: evidencePath,
            FileInfo: evidencePath is null ? null : fileInfo,
            SelectedRow: selectedRow,
            GridRows: rows,
            GridTotalRows: total,
            Bookmarks: bookmarks,
            MaxGridRows: Math.Clamp(GridRowLimit, 1, 200));
    }

    /// <summary>Sends the question with the requested context attached. Never throws.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input) || IsStreaming)
        {
            return;
        }

        var s = _settings.Load();
        var providerId = s.AiProvider.GetValueOrDefault("id", "disabled");
        if (providerId == "disabled")
        {
            StatusLine = "AI 未启用：到 设置 ▸ AI 助手 选择“模型运行位置”并填好地址与模型。";
            return;
        }
        if (!AiProviderFactory.TryBuild(providerId, s.AiProvider.GetValueOrDefault("endpoint"), Model ?? s.AiProvider.GetValueOrDefault("model"),
                s.AiProvider.GetValueOrDefault(SettingsStore.ApiKeyName), _http, out var provider, out var problem))
        {
            StatusLine = problem;
            return;
        }

        var context = BuildCurrentContext();
        var system = new StringBuilder(SystemPrompt);
        if (!string.IsNullOrEmpty(context))
        {
            system.Append("\n\n===== 以下是从 Cinder 当前界面收集到的上下文（结构化解析结果，可能被截断）=====\n");
            system.Append(context);
        }

        var prompt = new AiPrompt(
            SystemMessage: system.ToString(),
            Messages: Messages.Where(m => !m.IsError).Select(m => new AiMessage(m.Role, m.Content))
                .Append(new AiMessage("user", Input)).ToList(),
            Options: new AiPromptOptions(MaxTokens: 1500, Temperature: 0.2));

        Messages.Add(new ChatMessageVm("user", Input + (string.IsNullOrEmpty(context) ? "" : $"\n（已附上下文 {context.Length:N0} 字符）")));
        var assistant = new ChatMessageVm("assistant", "");
        Messages.Add(assistant);
        Input = "";

        IsStreaming = true;
        StatusLine = "等待模型回复…";
        var sb = new StringBuilder();
        try
        {
            await foreach (var chunk in provider.StreamCompletionAsync(prompt, ct))
            {
                sb.Append(chunk);
                assistant.Content = sb.ToString();
            }
            StatusLine = $"回复完成，{sb.Length:N0} 字符。";
        }
        catch (OperationCanceledException)
        {
            assistant.IsError = sb.Length == 0;
            if (sb.Length == 0)
            {
                assistant.Content = "（已在模型回复前停止）";
            }
            StatusLine = "已停止。";
        }
        catch (Exception ex)
        {
            assistant.IsError = true;
            assistant.Content = $"✗ 请求失败：{ex.Message}";
            StatusLine = $"请求失败：{ex.Message}";
        }
        finally
        {
            IsStreaming = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Messages.Clear();
        StatusLine = null;
    }

    /// <summary>Pulls the model list from the configured endpoint into the dropdown.</summary>
    [RelayCommand]
    private async Task RefreshModelsAsync(CancellationToken ct)
    {
        var s = _settings.Load();
        var providerId = s.AiProvider.GetValueOrDefault("id", "disabled");
        if (providerId == "disabled")
        {
            StatusLine = "AI 未启用：到 设置 ▸ AI 助手 先选择“模型运行位置”。";
            return;
        }
        IsFetchingModels = true;
        try
        {
            var models = await AiModelCatalog.FetchAsync(
                providerId, s.AiProvider.GetValueOrDefault("endpoint"),
                s.AiProvider.GetValueOrDefault(SettingsStore.ApiKeyName), ct).ConfigureAwait(true);
            ModelOptions.Clear();
            foreach (var m in models)
            {
                ModelOptions.Add(m);
            }
            StatusLine = $"已获取 {models.Count} 个模型。";
        }
        catch (OperationCanceledException)
        {
            StatusLine = "已取消获取模型列表。";
        }
        catch (Exception ex)
        {
            StatusLine = ex.Message;
        }
        finally
        {
            IsFetchingModels = false;
        }
    }

    /// <summary>Persists the picked model into settings.json so the Copilot page uses it too.</summary>
    [RelayCommand]
    private void ApplyModelToSettings()
    {
        if (string.IsNullOrWhiteSpace(Model))
        {
            return;
        }
        try
        {
            var s = _settings.Load();
            var ai = new Dictionary<string, string>(s.AiProvider, StringComparer.Ordinal);
            ai["model"] = Model.Trim();
            _settings.Save(s with { AiProvider = ai });
            StatusLine = $"模型已切换并保存：{Model}";
        }
        catch (Exception ex)
        {
            StatusLine = "模型未保存：" + ex.Message;
        }
    }

    /// <summary>The context text exactly as it would be sent right now.</summary>
    private string BuildCurrentContext() => AiContextBuilder.Build(BuildContextRequest());
}
