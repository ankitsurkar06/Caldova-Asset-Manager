using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaldovaAssetManager.Utils;

public static class HtmlHelperExtensions
{
    public static IHtmlContent Image(this IHtmlHelper helper, string src, string alt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src);
        var img = new TagBuilder("img") { TagRenderMode = TagRenderMode.SelfClosing };
        img.Attributes["src"] = $"{ConfigurationHelpers.GetString("ImagePath").TrimEnd('/')}/{src}";
        if (!string.IsNullOrWhiteSpace(alt)) img.Attributes["alt"] = alt;
        return img;
    }
}
