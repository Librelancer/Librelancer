// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer
{
	public static class AsteroidFieldShared
	{ 
        public static Vector3 GetCloseCube(Vector3 cameraPos, int cubeSize)
        {
            var posX = (int) (cameraPos.X < 0 ? cameraPos.X - 0.1f : cameraPos.X + 0.1f);
            var posY = (int) (cameraPos.Y < 0 ? cameraPos.Y - 0.1f : cameraPos.Y + 0.1f);
            var posZ = (int) (cameraPos.Z < 0 ? cameraPos.Z - 0.1f : cameraPos.Z + 0.1f);
            int sz = (int) ((cubeSize + 1.0f) * 0.5f);
            var center = new Vector3(
                posX < 1 ? sz : -sz,
                posY < 1 ? sz : -sz,
                posZ < 1 ? sz : -sz
            );
            var cubePos = new Vector3(
                (posX / cubeSize) * cubeSize,
                (posY / cubeSize) * cubeSize,
                (posZ / cubeSize) * cubeSize
            );
            return cubePos + center;
        }
		//TODO: This function seems to work, but should probably be analyzed to see if the outputs are any good
		/// <summary>
		/// Function to determine whether or not a cube is present in a field based on fill_rate
		/// </summary>
		/// <returns><c>true</c> if the cube is present, <c>false</c> otherwise.</returns>
		/// <param name="cubePos">Cube position.</param>
		/// <param name="fill_rate">Fill rate.</param>
        public static bool CubeExists(Vector3 cubePos, float emptyFrequency, out int selectedRotation)
		{
			//Check for fill rate
			selectedRotation = 0;
            if (emptyFrequency >= 1)
				return false;
			//integer hash
            var hashValue = PositionHash(cubePos);
            if (hashValue > emptyFrequency)
            {
                var rot = emptyFrequency > float.Epsilon
                    ? (hashValue - emptyFrequency) / (1.0f - emptyFrequency)
                    : hashValue;
                selectedRotation = (int) (rot * 63);
                return true;
            }
            return false;
        }

        public static unsafe float PositionHash(Vector3 cubePos)
        {
            var u = (uint*)&cubePos;
            uint h = hash(u[0]);
            unchecked
            {
                h = (3 * h) + hash(u[1]);
                h = (7 * h) + hash(u[2]);
            }
            //get float
            return constructFloat(h);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		//return a float between [0,1] for a hash
		static unsafe float constructFloat(uint m)
		{
			const uint ieeeMantissa = 0x007FFFFFu;
			const uint ieeeOne = 0x3F800000u;
			m &= ieeeMantissa;
			m |= ieeeOne;
			float f = *(float*)&m;
			return f - 1.0f;
		}
		//simple hash function
		static uint hash(uint x)
		{
            unchecked
            {
                x += (x << 10);
                x ^= (x >> 6);
                x += (x << 3);
                x ^= (x >> 11);
                x += (x << 15);
                return x;
            }
		}
	}
}

