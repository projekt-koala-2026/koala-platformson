using System.Reflection;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Services;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace koala.src.Modules.Account.Tests.UnitTests;

public class EmailServiceHtmlTests
{
    [Theory]
    [InlineData("Tytuł", "Treść", "Tytuł", "Treść")]
    [InlineData("  Tytuł  ", "  Treść  ", "Tytuł", "Treść")]
    [InlineData("A&B <C>", "x < y & z > q", "A&amp;B &lt;C&gt;", "x &lt; y &amp; z &gt; q")]
    public void FormatPlainTextAsHtmlBody_MatchesSampleHtmlAndEscapesContent(string title, string body, string expectedTitle, string expectedBody)
    {
        var html = EmailService.FormatPlainTextAsHtmlBody(body, title);

        Assert.Contains($"<h1 style=\"margin:0 0 16px; font-size:22px; line-height:1.3; color:#0f172a;\">\n                        {expectedTitle}\n                    </h1>", html);
        Assert.Contains($"<p style=\"margin:0 0 0; font-size:15px; line-height:1.65; color:#334155;\">\n                        {expectedBody}\n                    </p>", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cid:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pierwszy\ndrugi", "pierwszy<br>drugi")]
    [InlineData("jeden\n\ndwa", "jeden")]
    [InlineData("A\r\nB", "A<br>B")]
    [InlineData("A\rB", "A<br>B")]
    public void FormatPlainTextAsHtmlBody_ConvertsLineBreaksToHtml(string body, string expected)
    {
        var html = EmailService.FormatPlainTextAsHtmlBody(body);

        Assert.Contains(expected, html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorBuildsImageFreeEmailShellWithoutAssetFiles()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["EmailSettings:SmtpPort"] = "1025" }).Build();
        var options = new DbContextOptionsBuilder<AccountDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AccountDbContext(options);
        var service = new EmailService(configuration, Mock.Of<ICoreModule>(), Mock.Of<Hangfire.IBackgroundJobClient>(), db);
        var type = typeof(EmailService);
        var header = (string)type.GetField("_htmlHeader", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
        var footer = (string)type.GetField("_htmlFooter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

        Assert.Contains("<!DOCTYPE html>", header);
        Assert.Contains("KOALA", header);
        Assert.Contains("</html>", footer);
        Assert.DoesNotContain("<img", header + footer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cid:", header + footer, StringComparison.OrdinalIgnoreCase);
    }
}
