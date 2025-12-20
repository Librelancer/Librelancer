using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.Data.GameData;

public class NewsCollection
{
    private Dictionary<Base, List<NewsItem>> newsByBase = new();
    private Dictionary<NewsItem, List<Base>> basesByNews = new();
    private List<NewsItem> allNews = new();

    public void AddNewsItem(NewsItem item, int index = -1)
    {
        if (index != -1)
        {
            allNews.Insert(index, item);
        }
        else
        {
            allNews.Add(item);
        }
    }

    public int DeleteNewsItem(NewsItem item)
    {
        if (basesByNews.TryGetValue(item, out var baseList))
        {
            foreach (var b in baseList)
            {
                if (newsByBase.TryGetValue(b, out var newsList))
                {
                    newsList.Remove(item);
                }
            }
            basesByNews.Remove(item);
        }
        var pos = allNews.IndexOf(item);
        if(pos != -1)
            allNews.RemoveAt(pos);
        return pos;
    }

    public void AddToBase(NewsItem item, Base b)
    {
        if (!newsByBase.TryGetValue(b, out List<NewsItem> newsList))
        {
            newsByBase[b] = newsList = new();
        }
        newsList.Add(item);

        if (!basesByNews.TryGetValue(item, out var baseList))
        {
            basesByNews[item] = baseList = new();
        }
        baseList.Add(b);
    }

    public void RemoveFromBase(NewsItem item, Base b)
    {
        if (!newsByBase.TryGetValue(b, out var newsList))
            return;
        newsList.Remove(item);
        if (!basesByNews.TryGetValue(item, out var baseList))
            return;
        baseList.Remove(b);
    }

    public Base[] GetBases(NewsItem item)
    {
        if (basesByNews.TryGetValue(item, out var baseList))
            return baseList.ToArray();
        return [];
    }

    public NewsItem[] GetNewsForBase(Base loc)
    {
        if (newsByBase.TryGetValue(loc, out var list))
            return list.ToArray();
        return [];
    }

    public bool BaseHasNews(Base loc, NewsItem item)
    {
        if (!newsByBase.TryGetValue(loc, out var list))
            return false;
        return list.Contains(item);
    }

    public IEnumerable<NewsItem> QueryNews(Base loc, int missionNumber)
    {
        if (!newsByBase.TryGetValue(loc, out var newsList))
            return [];
        return newsList.Where(x => x.From.Index <=  missionNumber && x.To.Index >= missionNumber);
    }

    public IEnumerable<NewsItem> AllNews => allNews;

    public IEnumerable<(NewsItem, Base[])> AsCopy() =>
        basesByNews.Select(x => (x.Key, x.Value.ToArray()));

    public NewsCollection Clone()
    {
        var nc = new NewsCollection();
        foreach (var x in AsCopy())
        {
            var article = x.Item1.Clone();
            nc.AddNewsItem(article);
            foreach (var b in x.Item2)
            {
                nc.AddToBase(article, b);
            }
        }
        return nc;
    }
}
