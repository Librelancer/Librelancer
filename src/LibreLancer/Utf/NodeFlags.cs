using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer.Utf
{
        [Flags()]
        enum NodeFlags : int
        {
            Intermediate = 0x00000010,
            Leaf = 0x00000080
        }
}
