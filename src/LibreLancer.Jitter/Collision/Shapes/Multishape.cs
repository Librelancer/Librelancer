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
using System.Diagnostics;

using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision.Shapes;
#endregion

namespace LibreLancer.Jitter.Collision.Shapes
{


    /// <summary>
    /// Represents a variable form of a shape.
    /// </summary>
    public abstract class Multishape : Shape
    {

        /// <summary>
        /// Sets the current shape. First <see cref="Prepare"/> has to be called.
        /// After SetCurrentShape the shape immitates another shape.
        /// </summary>
        /// <param name="index"></param>
        public abstract void SetCurrentShape(int index);

        /// <summary>
        /// Passes a axis aligned bounding box to the shape where collision
        /// could occour.
        /// </summary>
        /// <param name="box">The bounding box where collision could occur.</param>
        /// <returns>The upper index with which <see cref="SetCurrentShape"/> can be 
        /// called.</returns>
        public abstract int Prepare(ref JBBox box);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rayOrigin"></param>
        /// <param name="rayDelta"></param>
        /// <returns></returns>
        public abstract int Prepare(ref Vector3 rayOrigin, ref Vector3 rayDelta);

        protected abstract Multishape CreateWorkingClone();

        internal bool isClone = false;

        public bool IsClone { get{ return isClone;} }

        Stack<Multishape> workingCloneStack = new Stack<Multishape>();
        public Multishape RequestWorkingClone()
        {
            Debug.Assert(this.workingCloneStack.Count<10, "Unusual size of the workingCloneStack. Forgot to call ReturnWorkingClone?");
            Debug.Assert(!this.isClone, "Can't clone clones! Something wrong here!");

            Multishape multiShape;

            lock (workingCloneStack)
            {
                if (workingCloneStack.Count == 0)
                {
                    multiShape = this.CreateWorkingClone();
                    multiShape.workingCloneStack = this.workingCloneStack;
                    workingCloneStack.Push(multiShape);
                }
                multiShape = workingCloneStack.Pop();
                multiShape.isClone = true;
            }

            return multiShape;
        }

        public override void UpdateShape()
        {
            lock(workingCloneStack) workingCloneStack.Clear();
            base.UpdateShape();
        }

        public void ReturnWorkingClone()
        {
            Debug.Assert(this.isClone, "Only clones can be returned!");
            lock (workingCloneStack) { workingCloneStack.Push(this); }
        }

        /// <summary>
        /// Gets the axis aligned bounding box of the orientated shape. This includes
        /// the whole shape.
        /// </summary>
        /// <param name="orientation">The orientation of the shape.</param>
        /// <param name="box">The axis aligned bounding box of the shape.</param>
        public override void GetBoundingBox(ref Matrix3 orientation, out JBBox box)
        {
            JBBox helpBox = JBBox.LargeBox;
            int length = this.Prepare(ref helpBox);

            box = JBBox.SmallBox;

            for (int i = 0; i < length; i++)
            {
                this.SetCurrentShape(i);
                base.GetBoundingBox(ref orientation, out helpBox);
                JBBox.CreateMerged(ref box, ref helpBox, out box);
            }
        }

        public override void MakeHull(ref List<Vector3> triangleList, int generationThreshold)
        {
            //throw new NotImplementedException();
        }


        /// <summary>
        /// Calculates the inertia of a box with the sides of the multishape.
        /// </summary>
        public override void CalculateMassInertia()
        {
            geomCen = Vector3.Zero;

            // TODO: calc this right
            inertia = Matrix3.Identity;

            Vector3 size; Vector3.Subtract(ref boundingBox.Max, ref boundingBox.Min, out size);

            mass = size.X * size.Y * size.Z;

            inertia.M11 = (1.0f / 12.0f) * mass * (size.Y * size.Y + size.Z * size.Z);
            inertia.M22 = (1.0f / 12.0f) * mass * (size.X * size.X + size.Z * size.Z);
            inertia.M33 = (1.0f / 12.0f) * mass * (size.X * size.X + size.Y * size.Y);
        }

    }
}
