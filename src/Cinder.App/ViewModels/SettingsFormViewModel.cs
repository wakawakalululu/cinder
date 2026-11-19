using System.Collections.ObjectModel;
using Cinder.App.Services;
using Cinder.App.ViewModels.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cinder.App.ViewModels;

/// <summary>One row on the Settings page's external-tools block: what was detected and where.</summary>
public sealed partial class ExternalToolRowVm : ObservableObject
{
    private readonly SettingsFormViewModel _owner;

    public ExternalToolRowVm(SettingsFormViewModel owner, ExternalToolDefinition def, string? overridePath, string? detectedPath, string sourceLabel)
    {
        _owner = owner;
        Id = def.Id;
        DisplayName = def.DisplayName;
        Purpose = def.Purpose;
        _overridePath = overridePath;
        _detectedPath = detectedPath;
        _sourceLabel = sourceLabel;
        _isFound = detectedPath is not null;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Purpose { get; }

    [ObservableProperty] private string? _detectedPath;
    [ObservableProperty] private string _sourceLabel;
    [ObservableProperty] private bool _isFound;

    /// <summary>The path the examiner pinned, or null to follow auto-detection.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverrideDisplay))]
    private string? _overridePath;

    public string OverrideDisplay => string.IsNullOrWhiteSpace(OverridePath) ? "" : OverridePath;

    /// <summary>Re-runs detection for this tool (layers: override → tools.json → registry → PATH → defaults).</summary>
    [RelayCommand]
    private void Detect() => _owner.RefreshExternalTool(this);

    /// <summary>Picks an executable to pin, then re-detects so the result is visible at once.</summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        var picked = await ToolDialog.PickFileAsync($"选择 {DisplayName} 的可执行文件", "可执行文件", "*.exe");
        if (string.IsNullOrWhiteSpace(picked))
        {
            return;
        }
        OverridePath = picked;
        _owner.RefreshExternalTool(this);
    }

    /// <summary>Drops the pin so auto-detection applies again.</summary>
    [RelayCommand]
    private void ClearOverride()
    {
        OverridePath = null;
        _owner.RefreshExternalTool(this);
    }
}

public sealed partial class SettingsFormViewModel : ViewModelBase
{
    private readonly SettingsStore _store;

    [ObservableProperty]
    private string _theme = "Dark";

    // Density, Vim mode, reduce-motion, update check, Python executable and parsers directory
    // are not on the dialog (nothing in the app reads them yet). They are still loaded and
    // written back, so saving from the dialog never strips them out of an existing file.
    [ObservableProperty]
    private string _density = "Comfortable";

    [ObservableProperty]
    private bool _vimModeInHex;

    [ObservableProperty]
    private bool _respectReduceMotion = true;

    [ObservableProperty]
    private bool _checkForUpdates = true;

    [ObservableProperty]
    private string? _pythonExecutable;

    [ObservableProperty]
    private string? _parsersDirectory;

    // BYOM AI
    [ObservableProperty] private string _aiProviderId = "disabled";
    [ObservableProperty] private string? _aiEndpoint;
    [ObservableProperty] private string? _aiModel;
    [ObservableProperty] private string? _aiApiKey;

    /// <summary>Model ids fetched from the configured endpoint; the model box becomes a dropdown.</summary>
    public ObservableCollection<string> AiModelOptions { get; } = new();

    /// <summary>Feedback for the fetch-models button, shown beside it.</summary>
    [ObservableProperty] private string? _aiStatusLine;

    [ObservableProperty] private bool _isFetchingModels;

    /// <summary>External programs (VMware / Wireshark / Volatility 3 / IDA / x64dbg / x32dbg / OCR).</summary>
    public ObservableCollection<ExternalToolRowVm> ExternalToolRows { get; } = new();

    // Hash sets (NSRL) — shared by the Hash sets tool and the Filesystem walk.
    [ObservableProperty] private string? _hashSetDatabase;
    [ObservableProperty] private string? _timestampAuthorityUrl;
    [ObservableProperty] private bool _hashFilesOnEnumerate;
    [ObservableProperty] private int _hashSizeLimitMb = 64;

    // Cloud client_ids
    [ObservableProperty] private string? _googleDriveClientId;
    [ObservableProperty] private string? _oneDriveClientId;
    [ObservableProperty] private string? _dropboxAppKey;

    /// <summary>
    /// Why the last <see cref="SaveCommand"/> did not save, or — straight after opening — why
    /// the settings file could not be read. Null when there is nothing wrong. The view can
    /// bind this, and should keep the dialog open while it is set.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaveError))]
    private string? _saveError;

    public bool HasSaveError => !string.IsNullOrEmpty(SaveError);

    /// <summary>The settings file these values are read from and saved to.</summary>
    public string SettingsPath => _store.Path;

    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light", "System"];
    public IReadOnlyList<string> DensityOptions { get; } = ["Comfortable", "Compact"];
    public IReadOnlyList<string> AiProviderOptions { get; } =
        ["disabled", "ollama", "lm-studio", "openai-compatible", "openai", "mistral"];

    public SettingsFormViewModel(SettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        if (!_store.TryLoad(out var s, out var error))
        {
            // Shown as defaults, but Save will refuse to write them over the unreadable file.
            SaveError = $"The settings file could not be read ({error}). It will not be overwritten.";
        }
        Theme = s.Theme;
        Density = s.Density;
        VimModeInHex = s.VimModeInHex;
        RespectReduceMotion = s.RespectReduceMotion;
        CheckForUpdates = s.CheckForUpdates;
        PythonExecutable = s.PythonExecutable;
        ParsersDirectory = s.ParsersDirectory;
        AiProviderId = s.AiProvider.GetValueOrDefault("id", "disabled");
        AiEndpoint = s.AiProvider.GetValueOrDefault("endpoint");
        AiModel = s.AiProvider.GetValueOrDefault("model");
        // The store presents the key under one spelling whichever one the file used.
        AiApiKey = s.AiProvider.GetValueOrDefault(SettingsStore.ApiKeyName);
        GoogleDriveClientId = s.CloudClientIds.GetValueOrDefault("google-drive");
        OneDriveClientId = s.CloudClientIds.GetValueOrDefault("onedrive");
        DropboxAppKey = s.CloudClientIds.GetValueOrDefault("dropbox");
        HashSetDatabase = s.HashSetDatabase;
        HashFilesOnEnumerate = s.HashFilesOnEnumerate;
        HashSizeLimitMb = s.HashSizeLimitMb;
        TimestampAuthorityUrl = s.TimestampAuthorityUrl;
        RefreshAllExternalTools(s.ExternalToolOverrides);
    }

    /// <summary>
    /// Saves and applies the settings. Never throws: a failure (disk full, file locked, or a
    /// settings file that exists but cannot be read) is put in <see cref="SaveError"/> and
    /// shown in the shell's status bar, because the dialog that called this may close at once.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        try
        {
            // Start from what is on disk now, not from what was there when the dialog opened:
            // anything this dialog does not edit (enabled plug-ins, a hash-set database picked
            // in its own tool meanwhile, keys added by a newer build) is carried through.
            if (!_store.TryLoad(out var current, out var error))
            {
                throw new SettingsUnreadableException(_store.Path, error ?? "unknown error");
            }

            var ai = new Dictionary<string, string>(current.AiProvider, StringComparer.Ordinal);
            SetOrRemove(ai, "id", AiProviderId ?? "disabled");
            SetOrRemove(ai, "endpoint", AiEndpoint);
            SetOrRemove(ai, "model", AiModel);
            SetOrRemove(ai, SettingsStore.ApiKeyName, AiApiKey);

            var cloud = new Dictionary<string, string>(current.CloudClientIds, StringComparer.Ordinal);
            SetOrRemove(cloud, "google-drive", GoogleDriveClientId);
            SetOrRemove(cloud, "onedrive", OneDriveClientId);
            SetOrRemove(cloud, "dropbox", DropboxAppKey);

            _store.Save(current with
            {
                Theme = Theme,
                Density = Density,
                VimModeInHex = VimModeInHex,
                RespectReduceMotion = RespectReduceMotion,
                CheckForUpdates = CheckForUpdates,
                PythonExecutable = PythonExecutable,
                ParsersDirectory = ParsersDirectory,
                AiProvider = ai,
                CloudClientIds = cloud,
                HashSetDatabase = string.IsNullOrWhiteSpace(HashSetDatabase) ? null : HashSetDatabase,
                HashFilesOnEnumerate = HashFilesOnEnumerate,
                HashSizeLimitMb = Math.Clamp(HashSizeLimitMb, 1, 4096),
                TimestampAuthorityUrl = string.IsNullOrWhiteSpace(TimestampAuthorityUrl) ? null : TimestampAuthorityUrl.Trim(),
                ExternalToolOverrides = CollectToolOverrides(),
            });

            SaveError = null;
            ThemeService.Apply(Theme);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Settings were not saved");
            SaveError = "Settings were not saved: " + ex.Message;
            ShellNotice.Error(SaveError);
        }
    }

    private static void SetOrRemove(Dictionary<string, string> map, string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            map.Remove(key);
        }
        else
        {
            map[key] = value;
        }
    }

    // ============================================================ external tools ========

    /// <summary>Overrides the rows have pinned right now — the dictionary saved to settings.</summary>
    internal Dictionary<string, string> CollectToolOverrides()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in ExternalToolRows)
        {
            if (!string.IsNullOrWhiteSpace(row.OverridePath))
            {
                map[row.Id] = row.OverridePath;
            }
        }
        return map;
    }

    /// <summary>Builds the rows once: detect with the overrides already saved in settings.</summary>
    private void RefreshAllExternalTools(IReadOnlyDictionary<string, string> savedOverrides)
    {
        ExternalToolRows.Clear();
        foreach (var def in ExternalToolDefinition.All)
        {
            savedOverrides.TryGetValue(def.Id, out var pinned);
            var row = new ExternalToolRowVm(this, def, pinned, null, "未检测到");
            ExternalToolRows.Add(row);
            RefreshExternalTool(row);
        }
    }

    /// <summary>Re-detects one tool: layers are override → tools.json → registry → PATH → defaults.</summary>
    internal void RefreshExternalTool(ExternalToolRowVm row)
    {
        var info = new ExternalToolLocator(CollectToolOverrides()).Locate(row.Id);
        row.DetectedPath = info.Path;
        row.SourceLabel = info.SourceLabel;
        row.IsFound = info.Found;
        // Show what detection found even when the override is broken: the row must never look
        // configured while pointing at a file that is not there.
        if (row.OverridePath is not null && info.Source != "override" && info.Found)
        {
            row.DetectedPath = $"{info.Path}（指定的路径无效，已自动改用{info.SourceLabel}）";
        }
    }

    /// <summary>Re-detects every tool (the block's 自动检测全部 button).</summary>
    [RelayCommand]
    private void DetectAllExternalTools()
    {
        foreach (var row in ExternalToolRows)
        {
            RefreshExternalTool(row);
        }
    }

    // ============================================================ model list ========

    /// <summary>
    /// Asks the configured endpoint which models it serves and fills the model dropdown.
    /// Needs only what is already on the page: provider, address and (for hosted services) the
    /// API key. Never throws; the reason lands in <see cref="AiStatusLine"/>.
    /// </summary>
    [RelayCommand]
    private async Task FetchModelsAsync(CancellationToken ct)
    {
        if (AiProviderId is not ("ollama" or "lm-studio" or "openai-compatible" or "openai" or "mistral"))
        {
            AiStatusLine = "请先选择一个模型运行位置（disabled 表示未启用 AI）。";
            return;
        }
        IsFetchingModels = true;
        AiStatusLine = null;
        try
        {
            var models = await AiModelCatalog.FetchAsync(AiProviderId, AiEndpoint, AiApiKey, ct).ConfigureAwait(true);
            AiModelOptions.Clear();
            foreach (var m in models)
            {
                AiModelOptions.Add(m);
            }
            AiStatusLine = $"已获取 {models.Count} 个模型，从下拉框中选择。";
            if (models.Count > 0 && string.IsNullOrWhiteSpace(AiModel))
            {
                AiModel = models[0];
            }
        }
        catch (OperationCanceledException)
        {
            AiStatusLine = "已取消获取模型列表。";
        }
        catch (Exception ex)
        {
            AiStatusLine = ex.Message;
        }
        finally
        {
            IsFetchingModels = false;
        }
    }
}
