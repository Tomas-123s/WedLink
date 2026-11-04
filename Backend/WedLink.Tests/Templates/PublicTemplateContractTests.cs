using WedLink.API.Contracts.Templates;
using WedLink.Business.Templates;

namespace WedLink.Tests.Templates;

public sealed class PublicTemplateContractTests
{
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://images.example.test/design.png")]
    [InlineData("https://user:secret@images.example.test/design.png")]
    [InlineData("https://images.example.test/design.png?key=private")]
    [InlineData("/images/design.png?token=private")]
    public void PublicTemplateResponseOmitsUnsafePreviewReferences(string reference)
    {
        var response = PublicTemplateResponse.FromData(new PublicTemplateData(1, "Design", null,
            reference, "1.0", []));
        Assert.Null(response.PreviewImageUrl);
    }

    [Fact]
    public void PublicTemplateResponseAllowsSafeHttpsAndLocalReferences()
    {
        var remote = PublicTemplateResponse.FromData(new PublicTemplateData(1, "Design", null,
            "https://cdn.example.test/design.png", "1.0", []));
        var local = PublicTemplateResponse.FromData(new PublicTemplateData(2, "Design", null,
            "/images/design.png", "1.0", []));

        Assert.Equal("https://cdn.example.test/design.png", remote.PreviewImageUrl);
        Assert.Equal("/images/design.png", local.PreviewImageUrl);
    }
}
