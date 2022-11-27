// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Net.Protocol
{
    public partial class NetResponseHandler
    {
        private Dictionary<int, object> completionSources = new Dictionary<int, object>();
    }
}