using Cinder.App.Services;
using FluentAssertions;
using Xunit;

namespace Cinder.App.Tests;

/// <summary>
/// The model-list fetcher's pure parts: reading the two response shapes found in the wild and
/// turning a free-typed address into the right URLs to try. The HTTP call itself is thin glue
/// over these and is exercised by hand against a real endpoint.
/// </summary>
public sealed class AiModelCatalogTests
{
    // ============================================================ parsing ========

    [Fact]
    public void Openai_shape_data_with_ids_is_parsed()
    {
        var models = AiModelCatalog.ParseModelList(
            """{"object":"list","data":[{"id":"gpt-4o-mini","object":"model"},{"id":"qwen2.5:7b"}]}""");
        models.Should().Equal("gpt-4o-mini", "qwen2.5:7b");
    }

    [Fact]
    public void Ollama_shape_models_with_names_is_parsed()
    {
        var models = AiModelCatalog.ParseModelList(
            """{"models":[{"name":"llama3:latest"},{"name":"qwen2.5:14b","model":"qwen2.5:14b"}]}""");
        models.Should().Equal("llama3:latest", "qwen2.5:14b");
    }

    [Fact]
    public void A_bare_array_of_strings_is_accepted()
    {
        AiModelCatalog.ParseModelList("""["a","b"]""").Should().Equal("a", "b");
    }

    [Fact]
    public void Duplicates_are_removed_and_results_are_sorted_case_insensitively()
    {
        var models = AiModelCatalog.ParseModelList(
            """{"data":[{"id":"Zeta"},{"id":"alpha"},{"id":"Zeta"},{"id":"Beta"}]}""");
        models.Should().Equal("alpha", "Beta", "Zeta");
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("""{"error":{"message":"nope"}}""")]
    [InlineData("[]")]   // an empty array is not a model list worth showing
    public void Shapes_that_hold_no_models_yield_an_empty_list(string json)
    {
        AiModelCatalog.ParseModelList(json).Should().BeEmpty();
    }

    // ============================================================ URLs ========

    [Fact]
    public void Ollama_tries_its_native_tags_endpoint_first()
    {
        var urls = AiModelCatalog.BuildCandidateUrls("ollama", "http://localhost:11434");
        urls.Should().Equal("http://localhost:11434/api/tags", "http://localhost:11434/v1/models");
    }

    [Fact]
    public void Ollama_behind_an_openai_style_v1_base_tries_models_then_the_native_tags()
    {
        var urls = AiModelCatalog.BuildCandidateUrls("ollama", "http://localhost:11434/v1");
        urls.Should().Equal("http://localhost:11434/v1/models", "http://localhost:11434/api/tags");
    }

    [Fact]
    public void Openai_compatible_adds_v1_when_the_address_does_not_carry_it()
    {
        var urls = AiModelCatalog.BuildCandidateUrls("openai-compatible", "http://localhost:1234");
        urls.Should().Equal("http://localhost:1234/v1/models", "http://localhost:1234/models");
    }

    [Fact]
    public void An_address_that_already_ends_in_v1_is_not_doubled()
    {
        var urls = AiModelCatalog.BuildCandidateUrls("openai", "https://api.openai.com/v1/");
        urls.Should().Equal("https://api.openai.com/v1/models");
    }

    [Fact]
    public void A_useless_address_yields_no_urls()
    {
        AiModelCatalog.BuildCandidateUrls("ollama", "   ").Should().BeEmpty();
        AiModelCatalog.BuildCandidateUrls("ollama", null).Should().BeEmpty();
    }

    // ============================================================ address normalisation ========

    [Fact]
    public void A_scheme_less_host_gets_http()
    {
        var ok = AiModelCatalog.TryNormalizeBase("192.168.1.5:11434", out var uri, out var problem);
        ok.Should().BeTrue();
        problem.Should().BeNull();
        uri!.Scheme.Should().Be("http");
        uri.Host.Should().Be("192.168.1.5");
        uri.Port.Should().Be(11434);
    }

    [Fact]
    public void Trailing_slashes_do_not_break_the_base()
    {
        var ok = AiModelCatalog.TryNormalizeBase("http://localhost:11434/", out var uri, out _);
        ok.Should().BeTrue();
        uri!.AbsoluteUri.Should().Be("http://localhost:11434/");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_address_is_refused_with_a_reason(string? endpoint)
    {
        var ok = AiModelCatalog.TryNormalizeBase(endpoint, out _, out var problem);
        ok.Should().BeFalse();
        problem.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void A_non_http_scheme_is_refused()
    {
        var ok = AiModelCatalog.TryNormalizeBase("ftp://example.com", out _, out var problem);
        ok.Should().BeFalse();
        problem.Should().Contain("http");
    }

    [Fact]
    public void Default_endpoints_cover_the_local_providers()
    {
        AiModelCatalog.DefaultEndpoint("ollama").Should().Be("http://localhost:11434");
        AiModelCatalog.DefaultEndpoint("lm-studio").Should().Be("http://localhost:1234");
        AiModelCatalog.DefaultEndpoint("openai").Should().Contain("api.openai.com");
    }
}
