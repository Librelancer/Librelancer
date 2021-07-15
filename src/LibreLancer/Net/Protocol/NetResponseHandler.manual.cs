using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace LibreLancer.Net
{
    public partial class NetResponseHandler
    {
        private Dictionary<int, object> completionSources = new Dictionary<int, object>();
    }
}