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

namespace LibreLancer.Jitter.Collision.Shapes
{
    public class MinkowskiSumShape : Shape
    {
        Vector3 shifted;
        List<Shape> shapes = new List<Shape>();

        public MinkowskiSumShape(IEnumerable<Shape> shapes)
        {
            AddShapes(shapes);
        }

        public void AddShapes(IEnumerable<Shape> shapes)
        {
            foreach (Shape shape in shapes)
            {
                if (shape is Multishape) throw new Exception("Multishapes not supported by MinkowskiSumShape.");
                this.shapes.Add(shape);
            }

            this.UpdateShape();
        }

        public void AddShape(Shape shape)
        {
            if (shape is Multishape) throw new Exception("Multishapes not supported by MinkowskiSumShape.");
            shapes.Add(shape);

            this.UpdateShape();
        }

        public bool Remove(Shape shape)
        {
            if (shapes.Count == 1) throw new Exception("There must be at least one shape.");
            bool result = shapes.Remove(shape);
            UpdateShape();
            return result;
        }

        public Vector3 Shift()
        {
            return -1 * this.shifted;
        }

        public override void CalculateMassInertia()
        {
            this.mass = Shape.CalculateMassInertia(this, out shifted, out inertia);
        }

        public override void SupportMapping(ref Vector3 direction, out Vector3 result)
        {
            Vector3 temp1, temp2 = Vector3.Zero;

            for (int i = 0; i < shapes.Count; i++)
            {
                shapes[i].SupportMapping(ref direction, out temp1);
                Vector3.Add(ref temp1, ref temp2, out temp2);
            }

            Vector3.Subtract(ref temp2, ref shifted, out result);
        }

    }
}
