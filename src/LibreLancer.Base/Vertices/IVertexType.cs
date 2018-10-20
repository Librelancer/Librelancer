// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreLancer.Vertices
{
    public interface IVertexType
    {
		VertexDeclaration GetVertexDeclaration();
    }
}
