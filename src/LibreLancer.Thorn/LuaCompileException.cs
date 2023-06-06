using System;
using System.Runtime.Serialization;

namespace LibreLancer.Thorn
{
    [Serializable]
    public class LuaCompileException : Exception
    {
        public LuaCompileException(string message) : base(message)
        {
        }

        protected LuaCompileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
