using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace LibreLancer.Net
{
    public class NetResponseHandler
    {
        private Dictionary<int, object> completionSources = new Dictionary<int, object>();

        public TaskCompletionSource<int> GetCompletionSource_int(int retSeq)
        {
            var src = new TaskCompletionSource<int>();
            completionSources.Add(retSeq, src);
            return src;
        }

        public bool HandlePacket(IPacket pkt)
        {
            switch (pkt)
            {
                case RespondIntPacket ri:
                    if (completionSources.TryGetValue(ri.Sequence, out object k)) {
                        completionSources.Remove(ri.Sequence);
                        if (k is TaskCompletionSource<int> i) {
                            i.SetResult(ri.Value);
                        }
                    }
                    break;
            }
            return false;
        }
    }
}