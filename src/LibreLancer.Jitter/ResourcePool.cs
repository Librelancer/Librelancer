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

namespace LibreLancer.Jitter
{

    /// <summary>
    /// A thread safe resource pool.
    /// </summary>
    /// <typeparam name="T">The type of the object to cache. The type T must
    /// have a parameterless constructor.</typeparam>
    #region public class ResourcePool<T>
    public class ResourcePool<T>
    {
        private Stack<T> stack = new Stack<T>();

        /// <summary>
        /// Creates a new instance of the ResourcePool class.
        /// </summary>
        public ResourcePool()
        {
        }

        /// <summary>
        /// Removes all cached resources.
        /// So they can get garbage collected.
        /// </summary>
        public void ResetResourcePool()
        {
            lock (stack) { stack.Clear(); }
        }

        public int Count { get { return stack.Count; } }

        /// <summary>
        /// Gives a resource back to the pool.
        /// </summary>
        /// <param name="obj">The resource to give back</param>
        public void GiveBack(T obj)
        {
            lock (stack) { stack.Push(obj); }
        }

        /// <summary>
        /// Get a free resource.
        /// </summary>
        /// <returns>The free resource.</returns>
        public T GetNew()
        {
            T freeObj;

            lock (stack)
            {
                if (stack.Count == 0)
                {
                    freeObj = Activator.CreateInstance<T>();
                    stack.Push(freeObj);
                }

                freeObj = stack.Pop();
            }

            return freeObj;
        }
    }
    #endregion


    /// <summary>
    /// A thread safe resource pool.
    /// </summary>
    /// <typeparam name="T">The type of the array of objects to cache. The type T must
    /// have a parameterless constructor.  Do not specify [].</typeparam>
    #region public class ArrayResourcePool<T>
    public class ArrayResourcePool<T>
    {
        private Stack<T[]> stack = new Stack<T[]>();

        /// <summary>
        /// The length of each array object to be created in the pool.
        /// </summary>
        private int arrayLength;

        /// <summary>
        /// Creates a new instance of the ResourcePool class.
        /// </summary>
        public ArrayResourcePool(int arrayLength)
        {
            this.arrayLength = arrayLength;
        }

        /// <summary>
        /// Removes all cached resources.
        /// So they can get garbage collected.
        /// </summary>
        public void ResetResourcePool()
        {
            lock (stack) { stack.Clear(); }
        }

        public int Count { get { return stack.Count; } }

        /// <summary>
        /// Gives a resource back to the pool.
        /// </summary>
        /// <param name="obj">The resource to give back</param>
        public void GiveBack(T[] obj)
        {
            lock (stack) { stack.Push(obj); }
        }

        /// <summary>
        /// Get a free resource.
        /// </summary>
        /// <returns>The free resource.</returns>
        public T[] GetNew()
        {
            T[] freeObj;

            lock (stack)
            {
                if (stack.Count == 0)
                {
                    freeObj = new T[arrayLength];
                    //freeObj = Activator.CreateInstance<T>();
                    stack.Push(freeObj);
                }

                freeObj = stack.Pop();
            }

            return freeObj;
        }
    }
    #endregion
}
