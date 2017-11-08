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
using System.Threading;

using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision.Shapes;
using LibreLancer.Jitter.Dynamics.Constraints;
using System.Collections.ObjectModel;
using LibreLancer.Jitter.DataStructures;
#endregion

namespace LibreLancer.Jitter.Collision
{
    /// <summary>
    /// Holds a list of bodies which are in contact with each other.
    /// </summary>
    public class CollisionIsland
    {

        internal IslandManager islandManager;

        internal HashSet<RigidBody> bodies = new HashSet<RigidBody>();
        internal HashSet<Arbiter> arbiter = new HashSet<Arbiter>();
        internal HashSet<Constraint> constraints = new HashSet<Constraint>();

        /// <summary>
        /// Gets a read only list of <see cref="RigidBody"/> which are in contact with each other.
        /// </summary>
        public ReadOnlyHashset<RigidBody> Bodies { get { return readOnlyBodies; } }

        /// <summary>
        /// Gets a read only list of <see cref="Arbiter"/> which are involved in this island.
        /// </summary>
        public ReadOnlyHashset<Arbiter> Arbiter { get { return readOnlyArbiter; } }

        /// <summary>
        /// Gets a read only list of <see cref="Constraint"/> which are involved in this island.
        /// </summary>
        public ReadOnlyHashset<Constraint> Constraints { get { return readOnlyConstraints; } }

        private ReadOnlyHashset<RigidBody> readOnlyBodies;
        private ReadOnlyHashset<Arbiter> readOnlyArbiter;
        private ReadOnlyHashset<Constraint> readOnlyConstraints;

        /// Constructor of CollisionIsland class.
        /// </summary>
        public CollisionIsland()
        {
            readOnlyBodies = new ReadOnlyHashset<RigidBody>(bodies);
            readOnlyArbiter = new ReadOnlyHashset<Arbiter>(arbiter);
            readOnlyConstraints = new ReadOnlyHashset<Constraint>(constraints);
        }

        /// <summary>
        /// Whether the island is active or not.
        /// </summary>
        /// <returns>Returns true if the island is active, otherwise false.</returns>
        /// <seealso cref="RigidBody.IsActive"/>
        public bool IsActive()
        {
            var enumerator = bodies.GetEnumerator();
            enumerator.MoveNext();

            if (enumerator.Current == null) return false;
            else return enumerator.Current.isActive;
        }

        /// <summary>
        /// Sets the status of every body in this island to active or inactive.
        /// </summary>
        /// <param name="active">If true the island gets activated, if false it
        /// gets deactivated. </param>
        /// <seealso cref="RigidBody.IsActive"/>
        public void SetStatus(bool active)
        {
            foreach (RigidBody body in bodies)
            {
                body.IsActive = active;
                if (active && !body.IsActive) body.inactiveTime = 0.0f;
            }

        }

        internal void ClearLists()
        {
            arbiter.Clear(); bodies.Clear(); constraints.Clear();
        }

    }
}
