#region License
/*
MIT License
Copyright Â© 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Numerics;

namespace LibreLancer;

public struct Ray : IEquatable<Ray>
{
    #region Public Fields

    public Vector3 Direction;

    public Vector3 Position;

    #endregion


    #region Public Constructors

    public Ray(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction;
    }

    #endregion


    #region Public Methods

    public override bool Equals(object? obj)
    {
        return (obj is Ray ray) && Equals(ray);
    }


    public bool Equals(Ray other)
    {
        return Position.Equals(other.Position) && Direction.Equals(other.Direction);
    }


    public override int GetHashCode()
    {
        return Position.GetHashCode() ^ Direction.GetHashCode();
    }

    // adapted from http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
    public float? Intersects(BoundingBox box)
    {
        const float epsilon = 1e-6f;

        float? tMin = null, tMax = null;

        if (Math.Abs(Direction.X) < epsilon)
        {
            if (Position.X < box.Min.X || Position.X > box.Max.X)
                return null;
        }
        else
        {
            tMin = (box.Min.X - Position.X) / Direction.X;
            tMax = (box.Max.X - Position.X) / Direction.X;

            if (tMin > tMax)
            {
                (tMin, tMax) = (tMax, tMin);
            }
        }

        if (Math.Abs(Direction.Y) < epsilon)
        {
            if (Position.Y < box.Min.Y || Position.Y > box.Max.Y)
                return null;
        }
        else
        {
            var tMinY = (box.Min.Y - Position.Y) / Direction.Y;
            var tMaxY = (box.Max.Y - Position.Y) / Direction.Y;

            if (tMinY > tMaxY)
            {
                (tMinY, tMaxY) = (tMaxY, tMinY);
            }

            if ((tMin > tMaxY) || (tMinY > tMax))
                return null;

            if (!tMin.HasValue || tMinY > tMin) tMin = tMinY;
            if (!tMax.HasValue || tMaxY < tMax) tMax = tMaxY;
        }

        if (Math.Abs(Direction.Z) < epsilon)
        {
            if (Position.Z < box.Min.Z || Position.Z > box.Max.Z)
                return null;
        }
        else
        {
            var tMinZ = (box.Min.Z - Position.Z) / Direction.Z;
            var tMaxZ = (box.Max.Z - Position.Z) / Direction.Z;

            if (tMinZ > tMaxZ)
            {
                (tMinZ, tMaxZ) = (tMaxZ, tMinZ);
            }

            if ((tMin > tMaxZ) || (tMinZ > tMax))
                return null;

            if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
            if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
        }

        return tMin switch
        {
            // having a positive tMin and a negative tMax means the ray is inside the box
            // we expect the intesection distance to be 0 in that case
            < 0 when tMax > 0 => 0,
            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            < 0 => null,
            _ => tMin
        };

    }


    public void Intersects(ref BoundingBox box, out float? result)
    {
        result = Intersects(box);
    }

    /*
    public float? Intersects(BoundingFrustum frustum)
    {
        if (frustum == null)
        {
            throw new ArgumentNullException("frustum");
        }

        return frustum.Intersects(this);
    }
    */

    public float? Intersects(BoundingSphere sphere)
    {
        Intersects(ref sphere, out var result);
        return result;
    }

    public float? Intersects(Plane plane)
    {
        Intersects(ref plane, out var result);
        return result;
    }

    public void Intersects(ref Plane plane, out float? result)
    {
        var den = Vector3.Dot(Direction, plane.Normal);
        if (Math.Abs(den) < 0.00001f)
        {
            result = null;
            return;
        }

        result = (-plane.D - Vector3.Dot(plane.Normal, Position)) / den;

        if (!(result < 0.0f))
        {
            return;
        }

        if (result < -0.00001f)
        {
            result = null;
            return;
        }

        result = 0.0f;
    }

    public void Intersects(ref BoundingSphere sphere, out float? result)
    {
        // Find the vector between where the ray starts the the sphere's centre
        Vector3 difference = sphere.Center - Position;

        var differenceLengthSquared = difference.LengthSquared();
        var sphereRadiusSquared = sphere.Radius * sphere.Radius;

        // If the distance between the ray start and the sphere's centre is less than
        // the radius of the sphere, it means we've intersected. N.B. checking the LengthSquared is faster.
        if (differenceLengthSquared < sphereRadiusSquared)
        {
            result = 0.0f;
            return;
        }

        var distanceAlongRay = Vector3.Dot(Direction, difference);

        // If the ray is pointing away from the sphere then we don't ever intersect
        if (distanceAlongRay < 0)
        {
            result = null;
            return;
        }

        // Next we kinda use Pythagoras to check if we are within the bounds of the sphere
        // if x = radius of sphere
        // if y = distance between ray position and sphere centre
        // if z = the distance we've travelled along the ray
        // if x^2 + z^2 - y^2 < 0, we do not intersect
        var dist = sphereRadiusSquared + distanceAlongRay * distanceAlongRay - differenceLengthSquared;

        result = (dist < 0) ? null : distanceAlongRay - (float?)Math.Sqrt(dist);
    }

    public static bool operator !=(Ray a, Ray b) => !a.Equals(b);
    public static bool operator ==(Ray a, Ray b) => a.Equals(b);
    public override string ToString() => $"{{Position:{Position.ToString()} Direction:{Direction.ToString()}}}";

    #endregion
}
