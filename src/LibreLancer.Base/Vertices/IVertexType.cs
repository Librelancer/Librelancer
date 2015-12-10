using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer.Vertices
{
    public interface IVertexType
    {
		void SetVertexPointers(int offset);
        int VertexSize();
    }
}
