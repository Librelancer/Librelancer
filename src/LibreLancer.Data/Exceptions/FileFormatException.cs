// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.Serialization;

namespace LibreLancer
{
    [Serializable]
    public class FileFormatException : FileException
    {
        private string actualFormat, expectedFormat;

        public FileFormatException() : base() { }

        public FileFormatException(string path) : base(path) { }

        public FileFormatException(string message, Exception innerException) : base(message, innerException) { }

        public FileFormatException(string path, string actualFormat, string expectedFormat)
            : base(path)
        {
            this.actualFormat = actualFormat;
            this.expectedFormat = expectedFormat;
        }

        protected FileFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public override string Message
        {
            get
            {
                return base.Message + "\r\nA " + expectedFormat + " file was expected but a " + actualFormat + " file was found";
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //TODO:
            base.GetObjectData(info, context);
        }
    }
}
