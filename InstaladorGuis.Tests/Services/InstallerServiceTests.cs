using InstaladorGuis.Services;

namespace InstaladorGuis.Tests;

public class InstallerServiceTests
{
    [Theory]
    [InlineData("https://example.com/app.jnlp", true)]
    [InlineData("http://server:8080/app.jnlp", true)]
    [InlineData("https://internal.inditex.com/guis/v2/install.jnlp", true)]
    public void IsValidUrl_AcceptsValidUrls(string url, bool expected)
    {
        Assert.Equal(expected, InstallerService.IsValidUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com/app.jnlp")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    public void IsValidUrl_RejectsInvalidUrls(string? url)
    {
        Assert.False(InstallerService.IsValidUrl(url!));
    }

    [Fact]
    public void IsValidUrl_RejectsUrlsExceedingMaxLength()
    {
        var longUrl = "https://example.com/" + new string('a', 2049);
        Assert.False(InstallerService.IsValidUrl(longUrl));
    }

    [Fact]
    public void IsValidUrl_AcceptsUrlAtMaxLength()
    {
        var maxUrl = "https://example.com/" + new string('a', 2048 - "https://example.com/".Length);
        Assert.True(InstallerService.IsValidUrl(maxUrl));
    }

    [Theory]
    [InlineData("https://example.com/app.jnlp?q=1&r=2", "https://example.com/app.jnlp?q=1r=2")]
    [InlineData("https://example.com/\"app\"", "https://example.com/app")]
    [InlineData("https://example.com/`app`", "https://example.com/app")]
    [InlineData("https://example.com/app\\path", "https://example.com/apppath")]
    [InlineData("https://example.com/app;id", "https://example.com/appid")]
    [InlineData("https://example.com/app|x", "https://example.com/appx")]
    public void SanitizeUrl_RemovesDangerousCharacters(string input, string expected)
    {
        Assert.Equal(expected, InstallerService.SanitizeUrl(input));
    }

    [Fact]
    public void SanitizeUrl_ThrowsOnEmpty()
    {
        Assert.Throws<ArgumentException>(() => InstallerService.SanitizeUrl(""));
        Assert.Throws<ArgumentException>(() => InstallerService.SanitizeUrl("   "));
    }

    [Fact]
    public void SanitizeUrl_RemovesSingleQuotes()
    {
        var result = InstallerService.SanitizeUrl("https://example.com/'app'");
        Assert.DoesNotContain("'", result);
    }

    [Fact]
    public void SanitizeUrl_RemovesAmpersand()
    {
        var result = InstallerService.SanitizeUrl("https://example.com/app&cmd=evil");
        Assert.DoesNotContain("&", result);
    }
}
