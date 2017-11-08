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

namespace LibreLancer.Jitter.Dynamics.Joints
{

    /// <summary>
    /// A joint is a collection of internally handled constraints.
    /// </summary>
    public abstract class Joint
    {
        /// <summary>
        /// The world class to which the internal constraints
        /// should be added.
        /// </summary>
        public World World { get; private set; }

        /// <summary>
        /// Creates a new instance of the Joint class.
        /// </summary>
        /// <param name="world">The world class to which the internal constraints
        /// should be added.</param>
        public Joint(World world) {this.World = world;}

        /// <summary>
        /// Adds the internal constraints of this joint to the world class.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Removes the internal constraints of this joint from the world class.
        /// </summary>
        public abstract void Deactivate();
    }
}
