using System;
namespace LibreLancer
{
    public class FOVUtil
    {
        //JFLP's Algorithm
        //Modified to output radians
        public static float CalcFovx(float deg, float ratio)
        {
            return (float)(Math.Atan(ratio / 2
               / (4.0f / 3 / 2
              / Math.Tan(deg * Math.PI / 180))));
        }
    }
}
