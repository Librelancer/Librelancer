/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

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