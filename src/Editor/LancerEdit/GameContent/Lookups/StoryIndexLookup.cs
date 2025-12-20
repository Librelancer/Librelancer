using System.Linq;
using LibreLancer;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class StoryIndexLookup : ObjectLookup<StoryIndex>
{
    public StoryIndexLookup(string id, GameDataManager dat, StoryIndex initial)
    {
        CreateDropdown(id,
            dat.Items.Story.Where(x => !x.Item.HideGui),
            x => x.Item.Nickname,
            initial);
    }
}
