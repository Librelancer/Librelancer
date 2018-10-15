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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer
{
    public struct LightBitfield
    {
        public static int Capacity = 128;
        long a;
        long b;
        public bool this[int idx]
        {
            get
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                    return (b & (1L << idx - 63)) != 0;
                else
                    return (a & (1L << idx)) != 0;
            }
            set
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                {
                    if (value)
                        b |= (1L << (idx - 63));
                    else
                        b &= ~(1L << (idx - 63));
                }
                else
                {
                    if (value)
                        a |= (1L << idx);
                    else
                        a &= ~(1L << idx);
                }
            }
        }
    }
}
