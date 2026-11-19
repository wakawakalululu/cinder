using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cinder.App.ViewModels;
using Cinder.App.Views;

namespace Cinder.App.Services;

/// <summary>Built-in command palette actions registered at startup.</summary>
public static class CommandRegistration
{
    public static void RegisterBuiltIns(CommandRegistry registry, MainWindowViewModel mainVm)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(mainVm);

        registry.Register(new CommandDescriptor(
            Id: "file.open",
            Title: "打开文件…",
            Subtitle: "在十六进制查看器中打开任意文件。",
            Category: "文件",
            Invoke: async ct =>
            {
                var window = ResolveMainWindow();
                if (window is null)
                {
                    return;
                }
                var picked = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "在 Cinder 中打开文件",
                    AllowMultiple = false,
                }).ConfigureAwait(false);
                var path = picked.FirstOrDefault()?.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    // False means the file could not be opened; the reason is already in the
                    // status bar, and the examiner stays where they were.
                    if (!mainVm.OpenFileInNewBuffer(path))
                    {
                        return;
                    }
                    var hexTool = mainVm.Workspace.FindByKind("hex");
                    if (hexTool is not null)
                    {
                        mainVm.Workspace.SelectedTool = hexTool;
                    }
                });
                _ = ct;
            }));

        registry.Register(new CommandDescriptor(
            Id: "hex.find",
            Title: "在文件中查找（列出全部匹配）…",
            Subtitle: "在打开的文件中搜索文本、十六进制字节或正则表达式，并列出所有匹配。",
            Category: "十六进制",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var owner = ResolveMainWindow();
                    if (owner is null || mainVm.Hex.Buffer is null)
                    {
                        return;
                    }
                    var dialog = new FindDialog { DataContext = new FindDialogViewModel(mainVm.Hex) };
                    dialog.ShowDialog(owner);
                });
                return Task.CompletedTask;
            }));

        registry.Register(new CommandDescriptor(
            Id: "hex.goto",
            Title: "跳转到偏移量…",
            Subtitle: "跳到打开文件中的某个位置，支持十进制或 0x 十六进制。",
            Category: "十六进制",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var owner = ResolveMainWindow();
                    if (owner is null || mainVm.Hex.Buffer is null)
                    {
                        return;
                    }
                    var dialog = new GotoDialog { DataContext = mainVm.Hex };
                    dialog.ShowDialog(owner);
                });
                return Task.CompletedTask;
            }));

        registry.Register(new CommandDescriptor(
            Id: "app.settings",
            Title: "设置",
            Subtitle: "主题、哈希样本集数据库、监管链时间戳服务、AI 模型。",
            Category: "应用",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() => mainVm.Workspace.SelectedTool = mainVm.Workspace.SettingsEntry);
                return Task.CompletedTask;
            }));

        registry.Register(new CommandDescriptor(
            Id: "case.open",
            Title: "打开案件…",
            Subtitle: "打开一个 .cinder 案件文件。",
            Category: "案件",
            Invoke: async ct =>
            {
                var owner = ResolveMainWindow();
                if (owner is null)
                {
                    return;
                }
                var picked = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "打开 Cinder 案件",
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("Cinder 案件") { Patterns = ["*.cinder"] }],
                }).ConfigureAwait(false);
                var path = picked.FirstOrDefault()?.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                await mainVm.OpenCaseFromPathAsync(path).ConfigureAwait(false);
            }));

        registry.Register(new CommandDescriptor(
            Id: "case.create",
            Title: "新建案件…",
            Subtitle: "开始一个新的案件文件（.cinder），带自己的监管链日志。",
            Category: "案件",
            Invoke: async ct =>
            {
                var owner = ResolveMainWindow();
                if (owner is null)
                {
                    return;
                }
                var picked = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "新建 Cinder 案件",
                    DefaultExtension = "cinder",
                    SuggestedFileName = "case.cinder",
                    FileTypeChoices = [new FilePickerFileType("Cinder 案件") { Patterns = ["*.cinder"] }],
                }).ConfigureAwait(false);
                var path = picked?.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                if (ExistingFileRefusal(path) is { } refusal)
                {
                    mainVm.AnnounceError(refusal);
                    return;
                }
                try
                {
                    var store = new Cinder.Core.Cases.CaseStore(path);
                    var custody = new Cinder.Core.Custody.CustodyLog(store);
                    var svc = new Cinder.Core.Cases.CaseService(store, custody);
                    var c = await svc.CreateAsync(System.IO.Path.GetFileNameWithoutExtension(path), Environment.UserName, null, ct).ConfigureAwait(false);
                    Dispatcher.UIThread.Post(() =>
                    {
                        mainVm.ActiveCaseName = c.Name;
                        mainVm.OpenCase(c.Id, c.Name, c.Examiner, path);
                        mainVm.Announce($"案件已创建：{c.Name}");
                    });
                }
                catch (Exception ex)
                {
                    mainVm.AnnounceError($"创建案件失败：{ex.Message}");
                }
            }));

        registry.Register(new CommandDescriptor(
            Id: "view.theme.toggle",
            Title: "切换主题（深色 / 浅色）",
            Subtitle: "在深色与浅色之间切换。选择会被记住。",
            Category: "视图",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ThemeService.Toggle() is not { } theme)
                    {
                        return;
                    }
                    // Remember the choice, or the next launch quietly undoes it.
                    try
                    {
                        var store = new SettingsStore();
                        if (!store.TryLoad(out var settings, out var error))
                        {
                            throw new SettingsUnreadableException(store.Path, error ?? "unknown error");
                        }
                        store.Save(settings with { Theme = theme });
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Warning(ex, "The theme choice was not saved");
                        mainVm.AnnounceError($"主题仅本次会话生效 —— 保存失败：{ex.Message}");
                    }
                });
                return Task.CompletedTask;
            }));

        registry.Register(new CommandDescriptor(
            Id: "app.about",
            Title: "关于 Cinder",
            Subtitle: "版本与许可证。",
            Category: "帮助",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() =>
                    mainVm.Announce($"Cinder {UpdateChecker.CurrentVersion()} · Apache-2.0 · github.com/AffluentMods/cinder"));
                return Task.CompletedTask;
            }));

        registry.Register(new CommandDescriptor(
            Id: "file.hash",
            Title: "计算文件哈希…",
            Subtitle: "对任意文件计算 MD5、SHA-1、SHA-256 与 BLAKE3；打开案件时会记录到监管链。",
            Category: "文件",
            Invoke: async _ =>
            {
                var owner = ResolveMainWindow();
                if (owner is null)
                {
                    return;
                }
                var picked = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择要哈希的文件",
                    AllowMultiple = false,
                }).ConfigureAwait(false);
                var path = picked.FirstOrDefault()?.TryGetLocalPath();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    // The dialog starts hashing as it opens and cancels the run if it is closed.
                    var dialog = new HashDialog { DataContext = new HashDialogViewModel(path: path) };
                    dialog.ShowDialog(owner);
                });
            }));

        registry.Register(new CommandDescriptor(
            Id: "app.update.check",
            Title: "检查更新",
            Subtitle: "现在向 GitHub 询问最新版本。Cinder 绝不会自行检查，也不会下载任何东西。",
            Category: "帮助",
            Invoke: async ct =>
            {
                // The only network request Cinder makes on its own account, and only because
                // the examiner just asked for it. See UpdateChecker.
                mainVm.Announce("正在检查更新…");
                UpdateCheckResult result;
                try
                {
                    result = await new UpdateChecker().CheckAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    mainVm.Announce("已取消检查更新。");
                    return;
                }
                if (result.Status == UpdateStatus.UpToDate)
                {
                    mainVm.Announce(result.Message);
                }
                else if (result.Status == UpdateStatus.UpdateAvailable)
                {
                    // Not an error, but it carries an address the examiner needs time to read.
                    mainVm.AnnounceNotice(result.Message);
                }
                else
                {
                    mainVm.AnnounceError(result.Message);
                }
            }));

        registry.Register(new CommandDescriptor(
            Id: "app.exit",
            Title: "退出 Cinder",
            Subtitle: null,
            Category: "应用",
            Invoke: _ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.Shutdown();
                    }
                });
                return Task.CompletedTask;
            }));
    }

    /// <summary>Shortcut shown in the palette for each built-in that has one.</summary>
    private static readonly Dictionary<string, string> Shortcuts = new(StringComparer.Ordinal)
    {
        ["file.open"] = "Ctrl O",
        ["hex.goto"] = "Ctrl G",
        ["app.settings"] = "Ctrl ,",
        ["case.open"] = "Ctrl Shift O",
        ["case.create"] = "Ctrl N",
    };

    /// <summary>
    /// Everything reachable from the rail and the shell is reachable from Ctrl+K too: one
    /// "go to" command per tool, the pane toggles, and the hex viewer's keyboard actions.
    /// </summary>
    public static void RegisterNavigation(CommandRegistry registry, MainWindowViewModel mainVm)
    {
        foreach (var (id, keys) in Shortcuts)
        {
            if (registry.Commands.FirstOrDefault(c => c.Id == id) is { } c)
            {
                registry.Replace(c with { Shortcut = keys });
            }
        }

        foreach (var section in mainVm.Workspace.Sections)
        {
            foreach (var tool in section.Tools)
            {
                var t = tool;
                if (t.Kind == "settings")
                {
                    // Already listed as the Settings command, which carries the Ctrl+, shortcut.
                    continue;
                }
                registry.Register(new CommandDescriptor(
                    Id: "tool." + t.Id,
                    Title: t.Title,
                    Subtitle: t.Subtitle,
                    Category: section.Title == "主页" ? "转到" : section.Title,
                    Invoke: _ =>
                    {
                        Dispatcher.UIThread.Post(() => mainVm.Workspace.SelectedTool = t);
                        return Task.CompletedTask;
                    }));
            }
        }

        void Action(string id, string title, string category, string? keys, Action run, string? subtitle = null) =>
            registry.Register(new CommandDescriptor(id, title, subtitle, category, _ =>
            {
                Dispatcher.UIThread.Post(run);
                return Task.CompletedTask;
            }, keys));

        Action("view.rail", "切换侧栏", "视图", "Ctrl B", () => mainVm.ToggleRailCommand.Execute(null));
        Action("view.inspector", "切换检查器", "视图", "Ctrl Shift B", () => mainVm.ToggleInspectorCommand.Execute(null),
            "十六进制查看器旁边的字节检查器。");
        Action("hex.findinline", "在十六进制查看器中查找", "十六进制", "Ctrl F", () => mainVm.FocusFindCommand.Execute(null));
        Action("hex.findnext", "查找下一个", "十六进制", "F3", () => mainVm.Hex.FindNextCommand.Execute(null));
        Action("hex.findprev", "查找上一个", "十六进制", "Shift F3", () => mainVm.Hex.FindPreviousCommand.Execute(null));
        Action("hex.bookmark", "为光标位置加书签", "十六进制", "Ctrl D", () => mainVm.Hex.AddBookmarkCommand.Execute(null));
        Action("hex.copyhex", "以十六进制复制所选", "十六进制", "Ctrl C", () => mainVm.Hex.CopySelectionAsHexCommand.Execute(null));
        Action("hex.back", "回到上一个偏移量", "十六进制", "Alt Left", () => mainVm.Hex.NavigateBackCommand.Execute(null));
        Action("hex.forward", "前进到下一个偏移量", "十六进制", "Alt Right", () => mainVm.Hex.NavigateForwardCommand.Execute(null));
        Action("hex.strings", "在字符串提取中打开当前文件", "十六进制", null, () => mainVm.OpenInStringsCommand.Execute(null));
        Action("hex.hash", "计算当前文件哈希 (SHA-256)", "十六进制", null, () => mainVm.Hex.StartHashCommand.Execute(null),
            "读取每个字节；2 GiB 以内的文件在打开时自动计算哈希。");
        Action("hex.utf16", "文本窗格按 UTF-16LE 显示", "十六进制", null, () => mainVm.Hex.TextIsUtf16 = true);
        Action("hex.ascii", "文本窗格按 ASCII 显示", "十六进制", null, () => mainVm.Hex.TextIsUtf16 = false);
        Action("hex.close", "关闭文件标签页", "十六进制", "Ctrl W", () => mainVm.CloseBufferCommand.Execute(mainVm.Hex));
        Action("case.close", "关闭案件", "案件", null, () => mainVm.CloseCaseCommand.Execute(mainVm.ActiveCase));
        Action("help.tool", "本工具的帮助", "帮助", "F1", () => mainVm.OpenHelpCommand.Execute(null));

        RegisterExternalToolCommands(registry, mainVm);
    }

    /// <summary>
    /// Commands that hand the file open in the Hex Viewer to an external program the locator
    /// found (Wireshark for captures, IDA / x64dbg / x32dbg for reversing, VMware for images).
    /// A tool that is not installed gets no command, so the palette stays honest; re-running
    /// Cinder after installing one is enough for it to appear.
    /// </summary>
    private static void RegisterExternalToolCommands(CommandRegistry registry, MainWindowViewModel mainVm)
    {
        var locator = new ExternalToolLocator();

        void Launch(string id, string title, string toolId, bool needsFile, bool prefixDashR)
        {
            if (locator.Locate(toolId) is not { Found: true } info || info.Path is null)
            {
                return;
            }
            var exe = info.Path;
            registry.Register(new CommandDescriptor(
                Id: id,
                Title: title,
                Subtitle: info.Path,
                Category: "外部工具",
                Invoke: _ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var file = mainVm.Hex.SourcePath;
                        if (needsFile && string.IsNullOrEmpty(file))
                        {
                            mainVm.AnnounceError("请先在十六进制查看器中打开一个文件，再交给外部工具。");
                            return;
                        }
                        try
                        {
                            // Wireshark reads a capture as -r <file>; IDA and the debuggers take
                            // the file bare; VMware just starts.
                            var start = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exe,
                                UseShellExecute = false,
                                WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? "",
                            };
                            if (prefixDashR)
                            {
                                start.Arguments = $"-r \"{file}\"";
                            }
                            else if (needsFile)
                            {
                                start.Arguments = $"\"{file}\"";
                            }
                            System.Diagnostics.Process.Start(start);
                            mainVm.Announce($"已交给外部工具：{info.Path}{(needsFile ? $"（{file}）" : "")}");
                        }
                        catch (Exception ex)
                        {
                            mainVm.AnnounceError($"无法启动 {info.Path}：{ex.Message}");
                        }
                    });
                    return Task.CompletedTask;
                }));
        }

        Launch("ext.vmware", "启动 VMware Workstation（仿真取证）", "vmware", needsFile: false, prefixDashR: false);
        Launch("ext.wireshark", "在 Wireshark 中打开当前文件", "wireshark", needsFile: true, prefixDashR: true);
        Launch("ext.ida", "在 IDA Pro 中打开当前文件", "ida", needsFile: true, prefixDashR: false);
        Launch("ext.x64dbg", "用 x64dbg 调试当前文件", "x64dbg", needsFile: true, prefixDashR: false);
        Launch("ext.x32dbg", "用 x32dbg 调试当前文件", "x32dbg", needsFile: true, prefixDashR: false);
    }

    /// <summary>
    /// Why a new case must not be created at <paramref name="path"/>, or null when it may.
    ///
    /// <para>A case file is a database, and "creating" one over an existing file does not
    /// replace it: the old case, its evidence records and its custody log stay inside, and the
    /// new case is added beside them. The save dialog's "replace?" prompt promises something
    /// that would not happen, so an existing file is refused outright. (A zero-length file is
    /// allowed: some platforms' save dialogs create one as a placeholder, and it holds nothing.)</para>
    /// </summary>
    internal static string? ExistingFileRefusal(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (Directory.Exists(path))
            {
                return $"未创建案件：{info.Name} 是一个文件夹。";
            }
            if (info.Exists && info.Length > 0)
            {
                return $"未创建案件：{info.Name} 已存在。Cinder 不会在现有文件上创建案件 —— " +
                       "请换一个名字；如果那正是你要的案件，请用“打开案件…”。";
            }
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return $"未创建案件：{ex.Message}";
        }
    }

    private static Window? ResolveMainWindow()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
