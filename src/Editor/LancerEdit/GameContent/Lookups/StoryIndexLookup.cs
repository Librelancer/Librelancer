using System.Linq;
using LibreLancer;
using LibreLancer.GameData;

namespace LancerEdit.GameContent.Lookups;

public class StoryIndexLookup : ObjectLookup<StoryIndex>
{
    public StoryIndexLookup(string id, GameDataManager dat, StoryIndex initial)
    {
        CreateDropdown(id,
            dat.Story.Where(x => !x.Item.HideGui),
            x => x.Item.Nickname,
            initial);
    }
}
