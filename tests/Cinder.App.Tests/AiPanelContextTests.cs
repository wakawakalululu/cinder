using Cinder.AI;
using Cinder.App.Services;
using FluentAssertions;
using Xunit;

namespace Cinder.App.Tests;

/// <summary>
/// What the docked AI panel sends with a question: structured, sectioned, bounded. The builder
/// is pure — these tests feed records/anonymous rows directly, no shell needed.
/// </summary>
public sealed class AiContextBuilderTests
{
    [Fact]
    public void Nothing_attached_builds_an_empty_string()
    {
        AiContextBuilder.Build(AiContextRequest.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Evidence_and_file_info_land_in_one_section()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            EvidencePath = @"E:\evidence\usb.img",
            FileInfo = "1.2 GB · SHA-256 abc123",
        });
        text.Should().Contain("【当前检材】").And.Contain(@"E:\evidence\usb.img").And.Contain("SHA-256 abc123");
    }

    [Fact]
    public void Selected_row_gets_its_own_section_with_the_tool_title()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            ToolTitle = "注册表",
            SelectedRow = "Key: Run · Value: evil.exe",
        });
        text.Should().Contain("【当前工具 · 注册表 · 选中行】").And.Contain("Key: Run");
    }

    [Fact]
    public void Grid_rows_are_numbered_and_capped_with_a_tail_note()
    {
        var rows = Enumerable.Range(0, 45).Select(i => (object)new { Id = i, Name = $"row{i}" }).ToList();
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            ToolTitle = "事件日志",
            GridRows = rows,
            GridTotalRows = 45,
        });

        text.Should().Contain("前 40 行（共 45 行，仅附前 40 行）");
        text.Should().Contain("1. Id: 0");
        text.Should().Contain("40. Id: 39");
        text.Should().Contain("其余 5 行未附上");
        text.Should().NotContain("41. ");
    }

    [Fact]
    public void Bookmarks_render_title_tool_and_note()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            Bookmarks = [("发现 A", "恶意自启动项", "注册表"), ("发现 B", null, "文件系统")],
        });
        text.Should().Contain("1. 发现 A［来源：注册表］ — 恶意自启动项");
        text.Should().Contain("2. 发现 B［来源：文件系统］");
    }

    [Fact]
    public void Long_cells_are_truncated_and_newlines_flattened()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            SelectedRow = new string('x', 1000) + "\nsecond line",
        });
        text.Should().NotContain("second line");
        text.Should().Contain("…");
        // 默认选中行上限 800 字符：超长单行会被截断，而不是把提示词撑爆。
        text.Split('\n').Single(l => l.Contains("xxx")).Length.Should().BeLessThanOrEqualTo(810);
    }

    [Fact]
    public void A_reasonable_selected_row_passes_through_intact()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            ToolTitle = "注册表",
            SelectedRow = "Key: Run · Value: evil.exe\n（多行备注照常保留）",
        });
        text.Should().Contain("Value: evil.exe").And.Contain("（多行备注照常保留）");
    }

    [Fact]
    public void The_whole_context_is_capped()
    {
        var rows = Enumerable.Range(0, 40).Select(i => (object)new { Blob = new string('y', 400) }).ToList();
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            GridRows = rows,
            MaxTotalChars = 2000,
        });
        text.Length.Should().BeLessThanOrEqualTo(2100);
        text.Should().EndWith("…（上下文过长，已截断）");
    }

    [Fact]
    public void Empty_sections_leave_no_dangling_headings()
    {
        var text = AiContextBuilder.Build(AiContextRequest.Empty with
        {
            EvidencePath = @"E:\evidence\a.img",
            GridRows = Array.Empty<object>(),
        });
        text.Should().Contain("【当前检材】");
        text.Should().NotContain("【当前工具");
        text.Should().NotContain("【案件书签】");
    }

    [Fact]
    public void Real_parser_rows_render_as_column_value_pairs()
    {
        var text = AiContextBuilder.RenderRow(new { Timestamp = "2024-01-01 08:00:00", Name = "svchost.exe", Pid = 412 });
        text.Should().Contain("Timestamp: 2024-01-01 08:00:00").And.Contain("Name: svchost.exe").And.Contain("Pid: 412");
    }
}

/// <summary>The provider factory's refusals mirror what the settings page can produce.</summary>
public sealed class AiProviderFactoryTests
{
    [Fact]
    public void Disabled_provider_is_accepted_without_a_problem()
    {
        var ok = AiProviderFactory.TryBuild("disabled", null, null, null,
            new HttpClient(), out var provider, out var problem);
        ok.Should().BeTrue();
        provider.Should().BeOfType<DisabledProvider>();
        problem.Should().BeNull();
    }

    [Fact]
    public void A_broken_address_is_refused_with_a_reason()
    {
        var ok = AiProviderFactory.TryBuild("ollama", "   ", "llama3", null,
            new HttpClient(), out _, out var problem);
        ok.Should().BeFalse();
        problem.Should().Contain("地址无效");
    }

    [Fact]
    public void A_missing_model_is_refused_with_how_to_fix_it()
    {
        var ok = AiProviderFactory.TryBuild("openai-compatible", "http://localhost:8080/v1", " ", "sk-x",
            new HttpClient(), out _, out var problem);
        ok.Should().BeFalse();
        problem.Should().Contain("获取模型列表");
    }

    [Fact]
    public void A_valid_openai_compatible_config_builds_a_provider()
    {
        var ok = AiProviderFactory.TryBuild("openai-compatible", "192.168.5.130:3425/v1", "qwen2.5", "sk-x",
            new HttpClient(), out var provider, out var problem);
        ok.Should().BeTrue();
        problem.Should().BeNull();
        provider.Should().BeOfType<OpenAiCompatibleProvider>();
    }

    [Fact]
    public void Ollama_and_lm_studio_build_their_own_providers()
    {
        AiProviderFactory.TryBuild("ollama", "http://localhost:11434", "llama3", null,
            new HttpClient(), out var p1, out _).Should().BeTrue();
        p1.Should().BeOfType<OllamaProvider>();
        AiProviderFactory.TryBuild("lm-studio", "http://localhost:1234", "local", null,
            new HttpClient(), out var p2, out _).Should().BeTrue();
        p2.Should().BeOfType<LmStudioProvider>();
    }
}
