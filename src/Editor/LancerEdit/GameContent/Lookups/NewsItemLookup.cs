using System;
using System.Linq;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class NewsItemLookup(
    NewsCollection news,
    GameDataContext gd,
    Func<NewsItem, bool> allow)
    : ObjectLookup<NewsItem>(news.AllNews.Where(allow),
        x => x == null ? "(none)" : $"[{x.Icon}] {gd.Infocards.GetStringResource(x.Headline)}");
