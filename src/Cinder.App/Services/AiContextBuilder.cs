using System.Text;
using Cinder.Core.Export;

namespace Cinder.App.Services;

/// <summary>What the docked AI panel may attach to a question, all optional and bounded.</summary>
public sealed record AiContextRequest(
    string? ToolTitle,
    string? EvidencePath,
    string? FileInfo,
    string? SelectedRow,
    IReadOnlyList<object>? GridRows,
    int GridTotalRows,
    IReadOnlyList<(string Title, string? Note, string? Tool)>? Bookmarks,
    int MaxGridRows = 40,
    int MaxBookmarks = 30,
    int MaxCellChars = 160,
    int MaxSelectedRowChars = 800,
    int MaxTotalChars = 12000)
{
    public static AiContextRequest Empty { get; } = new(null, null, null, null, null, 0, null);
}

/// <summary>
/// Renders what the AI panel sends along with a question: the evidence file's identity, the
/// row selected in the current grid, the first rows of the current grid, and the case's
/// bookmarks. Structured and bounded — it feeds the model parsed facts, never raw bytes, and
/// truncates before a prompt can grow without limit.
/// </summary>
public static class AiContextBuilder
{
    /// <summary>Builds the context block, or "" when nothing is attached. Never throws.</summary>
    public static string Build(AiContextRequest request)
    {
        try
        {
            var sb = new StringBuilder();
            void Section(string title, Action<StringBuilder> write)
            {
                var before = sb.Length;
                sb.Append("【").Append(title).Append("】\n");
                write(sb);
                if (sb.Length == before + title.Length + 3)
                {
                    sb.Length = before;   // nothing was written: drop the empty heading
                }
                else
                {
                    sb.Append('\n');
                }
            }

            if (!string.IsNullOrWhiteSpace(request.EvidencePath) || !string.IsNullOrWhiteSpace(request.FileInfo))
            {
                Section("当前检材", s =>
                {
                    if (!string.IsNullOrWhiteSpace(request.EvidencePath))
                    {
                        s.Append(request.EvidencePath).Append('\n');
                    }
                    if (!string.IsNullOrWhiteSpace(request.FileInfo))
                    {
                        s.Append(request.FileInfo).Append('\n');
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(request.SelectedRow))
            {
                Section($"当前工具 · {request.ToolTitle ?? "网格"} · 选中行",
                    s => s.AppendLine(Truncate(request.SelectedRow, request.MaxSelectedRowChars)));
            }

            if (request.GridRows is { Count: > 0 })
            {
                var take = Math.Min(request.GridRows.Count, request.MaxGridRows);
                Section(
                    $"当前工具 · {request.ToolTitle ?? "网格"} · 前 {take:N0} 行" +
                    (request.GridTotalRows > take ? $"（共 {request.GridTotalRows:N0} 行，仅附前 {take:N0} 行）" : ""),
                    s =>
                    {
                        for (var i = 0; i < take; i++)
                        {
                            s.Append(i + 1).Append(". ").AppendLine(Truncate(RenderRow(request.GridRows[i]), request.MaxCellChars));
                        }
                        if (request.GridRows.Count > take)
                        {
                            s.AppendLine($"…（其余 {(request.GridRows.Count - take):N0} 行未附上；需要更多请缩小筛选范围）");
                        }
                    });
            }

            if (request.Bookmarks is { Count: > 0 })
            {
                var take = Math.Min(request.Bookmarks.Count, request.MaxBookmarks);
                Section($"案件书签 · 前 {take:N0} 条", s =>
                {
                    for (var i = 0; i < take; i++)
                    {
                        var b = request.Bookmarks[i];
                        s.Append(i + 1).Append(". ").Append(b.Title);
                        if (!string.IsNullOrWhiteSpace(b.Tool))
                        {
                            s.Append("［来源：").Append(b.Tool).Append('］');
                        }
                        if (!string.IsNullOrWhiteSpace(b.Note))
                        {
                            s.Append(" — ").Append(Truncate(b.Note, request.MaxCellChars));
                        }
                        s.AppendLine();
                    }
                    if (request.Bookmarks.Count > take)
                    {
                        s.AppendLine($"…（其余 {(request.Bookmarks.Count - take):N0} 条未附上）");
                    }
                });
            }

            var text = sb.ToString().TrimEnd();
            if (text.Length > request.MaxTotalChars)
            {
                text = text[..request.MaxTotalChars] + "\n…（上下文过长，已截断）";
            }
            return text;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or InvalidOperationException)
        {
            return "";
        }
    }

    /// <summary>One grid row as "列: 值 · 列: 值", in the row type's own column order.</summary>
    public static string RenderRow(object row, int maxCellChars = 160)
    {
        try
        {
            var dict = TabularExporter.ToDictionary(row);
            var sb = new StringBuilder();
            foreach (var (key, value) in dict)
            {
                if (sb.Length > 0)
                {
                    sb.Append("  ·  ");
                }
                sb.Append(key).Append(": ").Append(Truncate(value ?? "", maxCellChars));
            }
            return sb.ToString();
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or InvalidOperationException)
        {
            return row.ToString() ?? "";
        }
    }

    /// <summary>Flattens newlines inside a cell so one row stays one line.</summary>
    private static string Truncate(string text, int maxChars)
    {
        var oneLine = text.Replace("\r", " ").Replace("\n", " ⏎ ");
        return oneLine.Length <= maxChars ? oneLine : oneLine[..maxChars] + "…";
    }
}
