using System;
using System.Linq;
using LibreLancer.GameData;

namespace LancerEdit.GameContent.Lookups;

public class NewsItemLookup : ObjectLookup<NewsItem>
{
    public NewsItemLookup(string id, NewsCollection news, GameDataContext gd, Func<NewsItem, bool> allow)
    {
        CreateDropdown(
            id,
            news.AllNews.Where(allow),
            x => $"[{x.Icon}] {gd.Infocards.GetStringResource(x.Headline)}",
            null);
    }
}
