/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
* 
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution. 
*/

#region Using Statements
using System;
using System.Collections.Generic;

using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision.Shapes;
#endregion

namespace LibreLancer.Jitter.LinearMath
{

    /// <summary>
    /// Contains some math operations used within Jitter.
    /// </summary>
    public sealed class JMath
    {

        /// <summary>
        /// PI.
        /// </summary>
        public const float Pi = 3.1415926535f;

        public const float PiOver2 = 1.570796326794f;

        /// <summary>
        /// A small value often used to decide if numeric 
        /// results are zero.
        /// </summary>
        public const float Epsilon = 1.192092896e-012f;

        /// <summary>
        /// Gets the square root.
        /// </summary>
        /// <param name="number">The number to get the square root from.</param>
        /// <returns></returns>
        #region public static float Sqrt(float number)
        public static float Sqrt(float number)
        {
            return (float)Math.Sqrt(number);
        }
        #endregion

        /// <summary>
        /// Gets the maximum number of two values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <returns>Returns the largest value.</returns>
        #region public static float Max(float val1, float val2)
        public static float Max(float val1, float val2)
        {
            return (val1 > val2) ? val1 : val2;
        }
        #endregion

        /// <summary>
        /// Gets the minimum number of two values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <returns>Returns the smallest value.</returns>
        #region public static float Min(float val1, float val2)
        public static float Min(float val1, float val2)
        {
            return (val1 < val2) ? val1 : val2;
        }
        #endregion

        /// <summary>
        /// Gets the maximum number of three values.
        /// </summary>
        /// <param name="val1">The first value.</param>
        /// <param name="val2">The second value.</param>
        /// <param name="val3">The third value.</param>
        /// <returns>Returns the largest value.</returns>
        #region public static float Max(float val1, float val2,float val3)
        public static float Max(float val1, float val2,float val3)
        {
            float max12 = (val1 > val2) ? val1 : val2;
            return (max12 > val3) ? max12 : val3;
        }
        #endregion

        /// <summary>
        /// Returns a number which is within [min,max]
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        #region public static float Clamp(float value, float min, float max)
        public static float Clamp(float value, float min, float max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }
        #endregion
        
        /// <summary>
        /// Changes every sign of the matrix entry to '+'
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="result">The absolute matrix.</param>
        #region public static void Absolute(ref JMatrix matrix,out JMatrix result)
        public static void Absolute(ref Matrix3 matrix,out Matrix3 result)
        {
			result = Matrix3.Identity;
            result.M11 = Math.Abs(matrix.M11);
            result.M12 = Math.Abs(matrix.M12);
            result.M13 = Math.Abs(matrix.M13);
            result.M21 = Math.Abs(matrix.M21);
            result.M22 = Math.Abs(matrix.M22);
            result.M23 = Math.Abs(matrix.M23);
            result.M31 = Math.Abs(matrix.M31);
            result.M32 = Math.Abs(matrix.M32);
            result.M33 = Math.Abs(matrix.M33);
        }
        #endregion
    }
}
