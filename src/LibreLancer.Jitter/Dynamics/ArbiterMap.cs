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
using System.Collections;
using LibreLancer.Jitter.Collision;
#endregion

namespace LibreLancer.Jitter.Dynamics
{
    /// <summary>
    /// For easy access, Arbiters are stored in a Hashtable(ArbiterMap). 
    /// To find the Arbiter fortwo RigidBodies, build an ArbiterKey for the two bodies
    /// and use it as the lookup key for the ArbiterMap.
    /// </summary>
    public struct ArbiterKey
    {
        // internal values for faster access within the engine
        internal RigidBody body1, body2;

        /// <summary>
        /// Initializes a new instance of the ArbiterKey class.
        /// </summary>
        /// <param name="body1"></param>
        /// <param name="body2"></param>
        public ArbiterKey(RigidBody body1, RigidBody body2)
        {
            this.body1 = body1;
            this.body2 = body2;
        }

        /// <summary>
        /// Don't call this, while the key is used in the arbitermap.
        /// It changes the hashcode of this object.
        /// </summary>
        /// <param name="body1">The first body.</param>
        /// <param name="body2">The second body.</param>
        internal void SetBodies(RigidBody body1, RigidBody body2)
        {
            this.body1 = body1;
            this.body2 = body2;
        }

        #region public override bool Equals(object obj)
        /// <summary>
        /// Checks if two objects are equal.
        /// </summary>
        /// <param name="obj">The object to check against.</param>
        /// <returns>Returns true if they are equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            ArbiterKey other = (ArbiterKey)obj;
            return (other.body1.Equals(body1) && other.body2.Equals(body2) ||
                other.body1.Equals(body2) && other.body2.Equals(body1));
        }
        #endregion

        #region public override int GetHashCode()
        /// <summary>
        /// Returns the hashcode of the ArbiterKey.
        /// The hashcode is the same if an ArbiterKey contains the same bodies.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return body1.GetHashCode() + body2.GetHashCode();
        }
        #endregion


    }

    internal class ArbiterKeyComparer : IEqualityComparer<ArbiterKey>
    {
        public bool Equals(ArbiterKey x, ArbiterKey y)
        {
            return (x.body1.Equals(y.body1) && x.body2.Equals(y.body2) ||
                x.body1.Equals(y.body2) && x.body2.Equals(y.body1));
        }

        public int GetHashCode(ArbiterKey obj)
        {
            return obj.body1.GetHashCode() + obj.body2.GetHashCode();
        }
    }

    /// <summary>
    /// The ArbiterMap is a dictionary which stores all arbiters.
    /// </summary>
    public class ArbiterMap : IEnumerable
    {
        private Dictionary<ArbiterKey, Arbiter> dictionary =
            new Dictionary<ArbiterKey, Arbiter>(2048, arbiterKeyComparer);

        private ArbiterKey lookUpKey;
        private static ArbiterKeyComparer arbiterKeyComparer = new ArbiterKeyComparer();

        /// <summary>
        /// Initializes a new instance of the ArbiterMap class.
        /// </summary>
        public ArbiterMap()
        {
            lookUpKey = new ArbiterKey(null,null);
        }

        /// <summary>
        /// Gets an arbiter by it's bodies. Not threadsafe.
        /// </summary>
        /// <param name="body1">The first body.</param>
        /// <param name="body2">The second body.</param>
        /// <param name="arbiter">The arbiter which was found.</param>
        /// <returns>Returns true if the arbiter could be found, otherwise false.</returns>
        public bool LookUpArbiter(RigidBody body1, RigidBody body2,out Arbiter arbiter)
        {
            lookUpKey.SetBodies(body1, body2);
            return dictionary.TryGetValue(lookUpKey, out arbiter);
        }

        public Dictionary<ArbiterKey, Arbiter>.ValueCollection Arbiters
        {
            get { return dictionary.Values; }
        }

        internal void Add(ArbiterKey key, Arbiter arbiter)
        {
            dictionary.Add(key, arbiter);
        }

        internal void Clear()
        {
            dictionary.Clear();
        }

        internal void Remove(Arbiter arbiter)
        {
            lookUpKey.SetBodies(arbiter.body1, arbiter.body2);
            dictionary.Remove(lookUpKey);
        }

        /// <summary>
        /// Checks if an arbiter is within the arbiter map.
        /// </summary>
        /// <param name="body1">The first body.</param>
        /// <param name="body2">The second body.</param>
        /// <returns>Returns true if the arbiter could be found, otherwise false.</returns>
        public bool ContainsArbiter(RigidBody body1, RigidBody body2)
        {
            lookUpKey.SetBodies(body1, body2);
            return dictionary.ContainsKey(lookUpKey);
        }

        public IEnumerator GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }
    }

}
