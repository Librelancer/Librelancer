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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;

namespace LibreLancer
{
    public enum PrimitiveTypes
    {
        TriangleList,
        TriangleStrip,
        LineList,
		LineStrip,
		Points
    }

    public static class PrimitiveTypeExtensions
    {
        public static int GLType(this PrimitiveTypes type)
        {
            switch (type)
            {
				case PrimitiveTypes.LineList:
					return GL.GL_LINES;
				case PrimitiveTypes.TriangleList:
					return GL.GL_TRIANGLES;
				case PrimitiveTypes.TriangleStrip:
					return GL.GL_TRIANGLE_STRIP;
				case PrimitiveTypes.LineStrip:
					return GL.GL_LINE_STRIP;
				case PrimitiveTypes.Points:
					return GL.GL_POINTS;
            }
            throw new ArgumentException();
        }

        public static int GetArrayLength(this PrimitiveTypes primitiveType, int primitiveCount)
        {
            switch (primitiveType)
            {
                case PrimitiveTypes.LineList:
                    return primitiveCount * 2;
                case PrimitiveTypes.TriangleList:
                    return primitiveCount * 3;
                case PrimitiveTypes.TriangleStrip:
                    return 3 + (primitiveCount - 1);
				case PrimitiveTypes.Points:
				case PrimitiveTypes.LineStrip:
					return primitiveCount;
            }
            throw new ArgumentException();
        }
    }
}
