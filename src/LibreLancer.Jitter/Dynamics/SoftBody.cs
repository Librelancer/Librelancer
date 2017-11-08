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
using System.Text;

using LibreLancer.Jitter.Dynamics.Constraints;
using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.Collision.Shapes;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision;
using System.Collections.ObjectModel;
#endregion

namespace LibreLancer.Jitter.Dynamics
{
    public partial class SoftBody : IBroadphaseEntity
    {
        [Flags]
        public enum SpringType
        {
            EdgeSpring = 0x02, ShearSpring = 0x04, BendSpring = 0x08
        }

        #region public class Spring : Constraint
        public class Spring : Constraint
        {
            public enum DistanceBehavior
            {
                LimitDistance,
                LimitMaximumDistance,
                LimitMinimumDistance,
            }

            public SpringType SpringType { get; set; }

            private float biasFactor = 0.1f;
            private float softness = 0.01f;
            private float distance;

            private DistanceBehavior behavior = DistanceBehavior.LimitDistance;

            /// <summary>
            /// Initializes a new instance of the DistanceConstraint class.
            /// </summary>
            /// <param name="body1">The first body.</param>
            /// <param name="body2">The second body.</param>
            /// <param name="anchor1">The anchor point of the first body in world space. 
            /// The distance is given by the initial distance between both anchor points.</param>
            /// <param name="anchor2">The anchor point of the second body in world space.
            /// The distance is given by the initial distance between both anchor points.</param>
            public Spring(RigidBody body1, RigidBody body2)
                : base(body1, body2)
            {
                distance = (body1.position - body2.position).Length;
            }

            public float AppliedImpulse { get { return accumulatedImpulse; } }

            /// <summary>
            /// 
            /// </summary>
            public float Distance { get { return distance; } set { distance = value; } }

            /// <summary>
            /// 
            /// </summary>
            public DistanceBehavior Behavior { get { return behavior; } set { behavior = value; } }

            /// <summary>
            /// Defines how big the applied impulses can get.
            /// </summary>
            public float Softness { get { return softness; } set { softness = value; } }

            /// <summary>
            /// Defines how big the applied impulses can get which correct errors.
            /// </summary>
            public float BiasFactor { get { return biasFactor; } set { biasFactor = value; } }

            float effectiveMass = 0.0f;
            float accumulatedImpulse = 0.0f;
            float bias;
            float softnessOverDt;

            Vector3[] jacobian = new Vector3[2];

            bool skipConstraint = false;

            float myCounter = 0.0f;

            /// <summary>
            /// Called once before iteration starts.
            /// </summary>
            /// <param name="timestep">The 5simulation timestep</param>
            public override void PrepareForIteration(float timestep)
            {
                Vector3 dp;
                Vector3.Subtract(ref body2.position, ref body1.position, out dp);

                float deltaLength = dp.Length - distance;

                if (behavior == DistanceBehavior.LimitMaximumDistance && deltaLength <= 0.0f)
                {
                    skipConstraint = true;
                }
                else if (behavior == DistanceBehavior.LimitMinimumDistance && deltaLength >= 0.0f)
                {
                    skipConstraint = true;
                }
                else
                {
                    skipConstraint = false;

                    Vector3 n = dp;
                    if (n.LengthSquared != 0.0f) n.Normalize();

                    jacobian[0] = -1.0f * n;
                    //jacobian[1] = -1.0f * (r1 % n);
                    jacobian[1] = 1.0f * n;
                    //jacobian[3] = (r2 % n);

                    effectiveMass = body1.inverseMass + body2.inverseMass;

                    softnessOverDt = softness / timestep;
                    effectiveMass += softnessOverDt;

                    effectiveMass = 1.0f / effectiveMass;

                    bias = deltaLength * biasFactor * (1.0f / timestep);

                    if (!body1.isStatic)
                    {
                        body1.linearVelocity += body1.inverseMass * accumulatedImpulse * jacobian[0];
                    }

                    if (!body2.isStatic)
                    {
                        body2.linearVelocity += body2.inverseMass * accumulatedImpulse * jacobian[1];
                    }
                }

            }

            /// <summary>
            /// Iteratively solve this constraint.
            /// </summary>
            public override void Iterate()
            {
                if (skipConstraint) return;

                float jv = Vector3.Dot(ref body1.linearVelocity, ref jacobian[0]);
                jv += Vector3.Dot(ref body2.linearVelocity, ref jacobian[1]);

                float softnessScalar = accumulatedImpulse * softnessOverDt;

                float lambda = -effectiveMass * (jv + bias + softnessScalar);

                if (behavior == DistanceBehavior.LimitMinimumDistance)
                {
                    float previousAccumulatedImpulse = accumulatedImpulse;
                    accumulatedImpulse = JMath.Max(accumulatedImpulse + lambda, 0);
                    lambda = accumulatedImpulse - previousAccumulatedImpulse;
                }
                else if (behavior == DistanceBehavior.LimitMaximumDistance)
                {
                    float previousAccumulatedImpulse = accumulatedImpulse;
                    accumulatedImpulse = JMath.Min(accumulatedImpulse + lambda, 0);
                    lambda = accumulatedImpulse - previousAccumulatedImpulse;
                }
                else
                {
                    accumulatedImpulse += lambda;
                }

                Vector3 temp;

                if (!body1.isStatic)
                {
                    Vector3.Multiply(ref jacobian[0], lambda * body1.inverseMass, out temp);
                    Vector3.Add(ref temp, ref body1.linearVelocity, out body1.linearVelocity);
                }

                if (!body2.isStatic)
                {
                    Vector3.Multiply(ref jacobian[1], lambda * body2.inverseMass, out temp);
                    Vector3.Add(ref temp, ref body2.linearVelocity, out body2.linearVelocity);
                }
            }

            public override void DebugDraw(IDebugDrawer drawer)
            {
                drawer.DrawLine(body1.position, body2.position);
            }

        }
        #endregion

        #region public class MassPoint : RigidBody
        public class MassPoint : RigidBody
        {
            public SoftBody SoftBody { get; private set; }

            public MassPoint(Shape shape, SoftBody owner, Material material)
                : base(shape, material, true)
            {
                this.SoftBody = owner;
            }

        }
        #endregion

        #region public class Triangle : ISupportMappable
        public class Triangle : ISupportMappable
        {
            private SoftBody owner;

            public SoftBody Owner { get { return owner; } }

            internal JBBox boundingBox;
            internal int dynamicTreeID;
            internal TriangleVertexIndices indices;


            public JBBox BoundingBox { get { return boundingBox; } }
            public int DynamicTreeID { get { return dynamicTreeID; } }

            public TriangleVertexIndices Indices { get { return indices; } }

            public MassPoint VertexBody1 { get { return owner.points[indices.I0]; } }
            public MassPoint VertexBody2 { get { return owner.points[indices.I1]; } }
            public MassPoint VertexBody3 { get { return owner.points[indices.I2]; } }

            public Triangle(SoftBody owner)
            {
                this.owner = owner;
            }

            public void GetNormal(out Vector3 normal)
            {
                Vector3 sum;
                Vector3.Subtract(ref owner.points[indices.I1].position, ref owner.points[indices.I0].position, out sum);
                Vector3.Subtract(ref owner.points[indices.I2].position, ref owner.points[indices.I0].position, out normal);
                Vector3.Cross(ref sum, ref normal, out normal);
            }

            public void UpdateBoundingBox()
            {
                boundingBox = JBBox.SmallBox;
                boundingBox.AddPoint(ref owner.points[indices.I0].position);
                boundingBox.AddPoint(ref owner.points[indices.I1].position);
                boundingBox.AddPoint(ref owner.points[indices.I2].position);

                boundingBox.Min -= new Vector3(owner.triangleExpansion);
                boundingBox.Max += new Vector3(owner.triangleExpansion);
            }

            public float CalculateArea()
            {
				return Vector3.Cross((owner.points[indices.I1].position - owner.points[indices.I0].position),
				                      (owner.points[indices.I2].position - owner.points[indices.I0].position)).Length;
            }

            public void SupportMapping(ref Vector3 direction, out Vector3 result)
            {

                float min = Vector3.Dot(ref owner.points[indices.I0].position, ref direction);
                float dot = Vector3.Dot(ref owner.points[indices.I1].position, ref direction);

                Vector3 minVertex = owner.points[indices.I0].position;

                if (dot > min)
                {
                    min = dot;
                    minVertex = owner.points[indices.I1].position;
                }
                dot = Vector3.Dot(ref owner.points[indices.I2].position, ref direction);
                if (dot > min)
                {
                    min = dot;
                    minVertex = owner.points[indices.I2].position;
                }


                Vector3 exp;
                Vector3.Normalize(ref direction, out exp);
                exp *= owner.triangleExpansion;
                result = minVertex + exp;


            }

            public void SupportCenter(out Vector3 center)
            {
                center = owner.points[indices.I0].position;
                Vector3.Add(ref center, ref owner.points[indices.I1].position, out center);
                Vector3.Add(ref center, ref owner.points[indices.I2].position, out center);
                Vector3.Multiply(ref center, 1.0f / 3.0f, out center);
            }
        }
        #endregion

        private SphereShape sphere = new SphereShape(0.1f);

        protected List<Spring> springs = new List<Spring>();
        protected List<MassPoint> points = new List<MassPoint>();
        protected List<Triangle> triangles = new List<Triangle>();

        public ReadOnlyCollection<Spring> EdgeSprings { get; private set; }
        public ReadOnlyCollection<MassPoint> VertexBodies { get; private set; }
        public ReadOnlyCollection<Triangle> Triangles { private set; get; }

        protected float triangleExpansion = 0.1f;

        private bool selfCollision = false;

        public bool SelfCollision { get { return selfCollision; } set { selfCollision = value; } }

        public float TriangleExpansion { get { return triangleExpansion; } 
            set { triangleExpansion = value; } }

        public float VertexExpansion { get { return sphere.Radius; } set { sphere.Radius = value; } }

        private float volume = 1.0f;
        private float mass = 1.0f;

        internal DynamicTree<Triangle> dynamicTree = new DynamicTree<Triangle>();
        public DynamicTree<Triangle> DynamicTree { get { return dynamicTree; } }

        private Material material = new Material();
        public Material Material { get { return material; } }

        JBBox box = new JBBox();

        bool active = true;


        /// <summary>
        /// Does create an empty body. Derive from SoftBody and fill 
        /// EdgeSprings,VertexBodies and Triangles by yourself.
        /// </summary>
        public SoftBody()
        {
        }

        /// <summary>
        /// Creates a 2D-Cloth. Connects Nearest Neighbours (4x, called EdgeSprings) and adds additional
        /// shear/bend constraints (4xShear+4xBend).
        /// </summary>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        /// <param name="scale"></param>
        public SoftBody(int sizeX,int sizeY, float scale)
        {
            List<TriangleVertexIndices> indices = new List<TriangleVertexIndices>();
            List<Vector3> vertices = new List<Vector3>();

            for (int i = 0; i < sizeY; i++)
            {
                for (int e = 0; e < sizeX; e++)
                {
                    vertices.Add(new Vector3(i, 0, e) *scale);
                }
            }
            
            for (int i = 0; i < sizeX-1; i++)
            {
                for (int e = 0; e < sizeY-1; e++)
                {
                    TriangleVertexIndices index = new TriangleVertexIndices();
                    {

                        index.I0 = (e + 0) * sizeX + i + 0;
                        index.I1 = (e + 0) * sizeX + i + 1;
                        index.I2 = (e + 1) * sizeX + i + 1;

                        indices.Add(index);

                        index.I0 = (e + 0) * sizeX + i + 0;
                        index.I1 = (e + 1) * sizeX + i + 1;
                        index.I2 = (e + 1) * sizeX + i + 0;

                        indices.Add(index);    
                    }
                }
            }

            EdgeSprings = new ReadOnlyCollection<Spring>(springs);
            VertexBodies = new ReadOnlyCollection<MassPoint>(points);
            Triangles = new ReadOnlyCollection<Triangle>(triangles);

            AddPointsAndSprings(indices, vertices);

            for (int i = 0; i < sizeX - 1; i++)
            {
                for (int e = 0; e < sizeY - 1; e++)
                {
                    Spring spring = new Spring(points[(e + 0) * sizeX + i + 1], points[(e + 1) * sizeX + i + 0]);
                    spring.Softness = 0.01f; spring.BiasFactor = 0.1f;
                    springs.Add(spring);
                }
            }

            foreach (Spring spring in springs)
            {
                Vector3 delta = spring.body1.position - spring.body2.position;

                if (delta.Z != 0.0f && delta.X != 0.0f) spring.SpringType = SpringType.ShearSpring;
                else spring.SpringType = SpringType.EdgeSpring;
            }


            for (int i = 0; i < sizeX - 2; i++)
            {
                for (int e = 0; e < sizeY - 2; e++)
                {
                    Spring spring1 = new Spring(points[(e + 0) * sizeX + i + 0], points[(e + 0) * sizeX + i + 2]);
                    spring1.Softness = 0.01f; spring1.BiasFactor = 0.1f;

                    Spring spring2 = new Spring(points[(e + 0) * sizeX + i + 0], points[(e + 2) * sizeX + i + 0]);
                    spring2.Softness = 0.01f; spring2.BiasFactor = 0.1f;

                    spring1.SpringType = SpringType.BendSpring;
                    spring2.SpringType = SpringType.BendSpring;

                    springs.Add(spring1);
                    springs.Add(spring2);
                }
            }
        }

        public SoftBody(List<TriangleVertexIndices> indices, List<Vector3> vertices)
        {
            EdgeSprings = new ReadOnlyCollection<Spring>(springs);
            VertexBodies = new ReadOnlyCollection<MassPoint>(points);

            AddPointsAndSprings(indices, vertices);
            Triangles = new ReadOnlyCollection<Triangle>(triangles);
        }


        private float pressure = 0.0f;
        public float Pressure { get { return pressure; } set { pressure = value; } }

        private struct Edge
        {
            public int Index1;
            public int Index2;

            public Edge(int index1, int index2)
            {
                Index1 = index1;
                Index2 = index2;
            }

            public override int GetHashCode()
            {
                return Index1.GetHashCode() + Index2.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                Edge e = (Edge)obj;
                return (e.Index1 == Index1 && e.Index2 == Index2 || e.Index1 == Index2 && e.Index2 == Index1);
            }
        }

        #region AddPressureForces
        private void AddPressureForces(float timeStep)
        {
            if (pressure == 0.0f || volume == 0.0f) return;

            float invVolume = 1.0f / volume;

            foreach (Triangle t in triangles)
            {
                Vector3 v1 = points[t.indices.I0].position;
                Vector3 v2 = points[t.indices.I1].position;
                Vector3 v3 = points[t.indices.I2].position;

				Vector3 cross = Vector3.Cross((v3 - v1), (v2 - v1));
                Vector3 center = (v1 + v2 + v3) * (1.0f / 3.0f);

                points[t.indices.I0].AddForce(invVolume * cross * pressure);
                points[t.indices.I1].AddForce(invVolume * cross * pressure);
                points[t.indices.I2].AddForce(invVolume * cross * pressure);
            }
        }
        #endregion

        public void Translate(Vector3 position)
        {
            foreach (MassPoint point in points) point.Position += position;

            Update(float.Epsilon);
        }

        public void AddForce(Vector3 force)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Rotate(Matrix3 orientation, Vector3 center)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i].position = Vector3.Transform(points[i].position - center, orientation);
            }
        }

        public Vector3 CalculateCenter()
        {
            // TODO
            throw new NotImplementedException();
        }

        private HashSet<Edge> GetEdges(List<TriangleVertexIndices> indices)
        {
            HashSet<Edge> edges = new HashSet<Edge>();

            for (int i = 0; i < indices.Count; i++)
            {
                Edge edge;

                edge = new Edge(indices[i].I0, indices[i].I1);
                if (!edges.Contains(edge)) edges.Add(edge);

                edge = new Edge(indices[i].I1, indices[i].I2);
                if (!edges.Contains(edge)) edges.Add(edge);

                edge = new Edge(indices[i].I2, indices[i].I0);
                if (!edges.Contains(edge)) edges.Add(edge);
            }

            return edges;
        }

        List<int> queryList = new List<int>();

        public virtual void DoSelfCollision(CollisionDetectedHandler collision)
        {
            if (!selfCollision) return;

            Vector3 point, normal;
            float penetration;

            for (int i = 0; i < points.Count; i++)
            {
                queryList.Clear();
                this.dynamicTree.Query(queryList, ref points[i].boundingBox);

                for (int e = 0; e < queryList.Count; e++)
                {
                    Triangle t = this.dynamicTree.GetUserData(queryList[e]);

                    if (!(t.VertexBody1 == points[i] || t.VertexBody2 == points[i] || t.VertexBody3 == points[i]))
                    {
                        if (XenoCollide.Detect(points[i].Shape, t, ref points[i].orientation,
                            ref Matrix3.Identity, ref points[i].position, ref Vector3.Zero,
                            out point, out normal, out penetration))
                        {
                            int nearest = CollisionSystem.FindNearestTrianglePoint(this, queryList[e], ref point);

                            collision(points[i], points[nearest], point, point, normal, penetration);
                     
                        }
                    }
                }
            }
        }
                    
                

        private void AddPointsAndSprings(List<TriangleVertexIndices> indices, List<Vector3> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                MassPoint point = new MassPoint(sphere, this,material);
                point.Position = vertices[i];

                point.Mass = 0.1f;

                points.Add(point);
            }

            for (int i = 0; i < indices.Count; i++)
            {
                TriangleVertexIndices index = indices[i];
                
                Triangle t = new Triangle(this);

                t.indices = index;
                triangles.Add(t);

                t.boundingBox = JBBox.SmallBox;
                t.boundingBox.AddPoint(points[t.indices.I0].position);
                t.boundingBox.AddPoint(points[t.indices.I1].position);
                t.boundingBox.AddPoint(points[t.indices.I2].position);

                t.dynamicTreeID = dynamicTree.AddProxy(ref t.boundingBox, t);
            }

            HashSet<Edge> edges = GetEdges(indices);

            int count = 0;

            foreach (Edge edge in edges)
            {
                Spring spring = new Spring(points[edge.Index1], points[edge.Index2]);
                spring.Softness = 0.01f; spring.BiasFactor = 0.1f;
                spring.SpringType = SpringType.EdgeSpring;

                springs.Add(spring);
                count++;
            }

        }

        public void SetSpringValues(float bias, float softness)
        {
            SetSpringValues(SpringType.EdgeSpring | SpringType.ShearSpring | SpringType.BendSpring,
                bias, softness);
        }

        public void SetSpringValues(SpringType type, float bias, float softness)
        {
            for (int i = 0; i < springs.Count; i++)
            {
                if ((springs[i].SpringType & type) != 0)
                {
                    springs[i].Softness = softness; springs[i].BiasFactor = bias;
                }
            }
        }

        public virtual void Update(float timestep)
        {
            active = false;

            foreach (MassPoint point in points)
            {
                if (point.isActive && !point.isStatic) { active = true; break; }
            }

            if(!active) return;

            box = JBBox.SmallBox;
            volume = 0.0f;
            mass = 0.0f;

            foreach (MassPoint point in points)
            {
                mass += point.Mass;
                box.AddPoint(point.position);
            }

            box.Min -= new Vector3(TriangleExpansion);
            box.Max += new Vector3(TriangleExpansion);

            foreach (Triangle t in triangles)
            {
                // Update bounding box and move proxy in dynamic tree.
                Vector3 prevCenter = t.boundingBox.Center;
                t.UpdateBoundingBox();

                Vector3 linVel = t.VertexBody1.linearVelocity + 
                    t.VertexBody2.linearVelocity + 
                    t.VertexBody3.linearVelocity;

                linVel *= 1.0f / 3.0f;

                dynamicTree.MoveProxy(t.dynamicTreeID, ref t.boundingBox, linVel * timestep);

                Vector3 v1 = points[t.indices.I0].position;
                Vector3 v2 = points[t.indices.I1].position;
                Vector3 v3 = points[t.indices.I2].position;

                volume -= ((v2.Y - v1.Y) * (v3.Z - v1.Z) -
                    (v2.Z - v1.Z) * (v3.Y - v1.Y)) * (v1.X + v2.X + v3.X);
            }

            volume /= 6.0f;

            AddPressureForces(timestep);
        }

        public float Mass
        {
            get
            {
                return mass;
            }
            set
            {
                for (int i = 0; i < points.Count; i++)
                {
                    points[i].Mass = value / points.Count;
                }
            }
        }

        public float Volume { get { return volume; } }

        public JBBox BoundingBox
        {
            get { return box; }
        }

        public int BroadphaseTag { get; set; }

        public object Tag { get; set; }

        public bool IsStaticOrInactive
        {
            get { return !active; }
        }
    }


}


    
