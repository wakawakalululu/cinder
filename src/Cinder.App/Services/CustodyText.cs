using System.Text;
using System.Text.Json;
using Avalonia.Data.Converters;

namespace Cinder.App.Services;

/// <summary>
/// Turns a custody entry's stored action id and JSON details into words for the table.
/// Display only: the entry itself, its hash and every export keep the stored text exactly.
/// </summary>
public static class CustodyText
{
    /// <summary>"case.opened" becomes "Case opened"; an id this build does not know is shown as stored.</summary>
    public static string Action(string? action) => action switch
    {
        "case.created" => "新建案件",
        "case.opened" => "打开案件",
        "evidence.hashed" => "检材哈希",
        "evidence.imaged" => "检材采集镜像",
        "evidence.mounted" => "挂载检材",
        "evidence.unmounted" => "卸载检材",
        "parser.ran" => "工具运行",
        "report.exported" => "报告导出",
        "data.exported" => "数据导出",
        "annotation" => "备注",
        null => "",
        _ => action,
    };

    /// <summary>
    /// A flat JSON object becomes "Name: value  ·  Name: value". Empty values are left out.
    /// Anything that is not a JSON object is returned unchanged.
    /// </summary>
    public static string Details(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "";
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return json;
            }
            var sb = new StringBuilder();
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.True => "是",
                    JsonValueKind.False => "否",
                    JsonValueKind.Number => property.Value.GetRawText(),
                    _ => property.Value.GetRawText(),
                };
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append("  ·  ");
                }
                sb.Append(Label(property.Name)).Append(": ").Append(value);
            }
            return sb.Length == 0 ? json : sb.ToString();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>"BytesWritten" becomes "Bytes written"; "MD5" and "Sha256" keep their shape.</summary>
    private static string Label(string name)
    {
        // [zh-cn] 已知字段名的固定中文标签；未知字段回退到驼峰拆词的英文原文。
        var zh = name switch
        {
            "Tool" => "工具",
            "Evidence" => "检材",
            "Rows" => "行数",
            "Truncated" => "已截断",
            "BytesWritten" => "写入字节",
            "Padded" => "有填充",
            "Note" => "备注",
            "Path" => "路径",
            "Source" => "来源",
            "Destination" => "目标",
            "Format" => "格式",
            "Template" => "模板",
            "Output" => "输出",
            "Examiner" => "取证员",
            "Verdict" => "结论",
            "Mount" => "挂载",
            _ => null,
        };
        if (zh is not null)
        {
            return zh;
        }
        if (name.Length < 2 || name.All(c => char.IsUpper(c) || char.IsDigit(c)))
        {
            return name;
        }
        var sb = new StringBuilder(name.Length + 4);
        sb.Append(char.ToUpperInvariant(name[0]));
        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && char.IsLower(name[i - 1]))
            {
                sb.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static IValueConverter ActionConverter { get; } =
        new FuncValueConverter<string?, string>(Action);

    public static IValueConverter DetailsConverter { get; } =
        new FuncValueConverter<string?, string>(Details);
}
