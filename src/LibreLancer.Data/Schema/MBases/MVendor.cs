using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.MBases;

[ParsedSection]
public partial class MVendor
{
    [Entry("num_offers")]
    public Vector2 NumOffers;
}
