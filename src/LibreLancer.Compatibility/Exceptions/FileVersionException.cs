// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.Serialization;

namespace LibreLancer
{
    [Serializable]
    public class FileVersionException : FileException
    {
        private string format;
        private int actualVersion, expectedVersion;

        public FileVersionException() : base() { }

        public FileVersionException(string path) : base(path) { }

        public FileVersionException(string message, Exception innerException) : base(message, innerException) { }

        public FileVersionException(string path, string format, int actualVersion, int expectedVersion)
            : base(path)
        {
            this.format = format;
            this.actualVersion = actualVersion;
            this.expectedVersion = expectedVersion;
        }

        protected FileVersionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public override string Message
        {
            get
            {
                return base.Message + "\r\nA " + format + " file of version " + expectedVersion + " was expected but a vesion " + actualVersion + " file was found";
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //TODO:
            base.GetObjectData(info, context);
        }
    }
}