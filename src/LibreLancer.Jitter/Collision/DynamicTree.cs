/*
* Jitter Physics
* Copyright (c) 2011 Thorben Linneweber
* made 3d
* Added DynamicTree vs DynamicTree collision query
* 
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibreLancer.Jitter.LinearMath;


namespace LibreLancer.Jitter.Collision
{

    /// <summary>
    /// A node in the dynamic tree. The client does not interact with this directly.
    /// </summary>
    public struct DynamicTreeNode<T>
    {
        /// <summary>
        /// This is the fattened AABB.
        /// </summary>
        public JBBox AABB;

        public float MinorRandomExtension;

        public int Child1;
        public int Child2;

        public int LeafCount;
        public int ParentOrNext;
        public T UserData;

        public bool IsLeaf()
        {
            return Child1 == DynamicTree<T>.NullNode;
        }
    }

    /// <summary>
    /// A dynamic tree arranges data in a binary tree to accelerate
    /// queries such as volume queries and ray casts. Leafs are proxies
    /// with an AABB. In the tree we expand the proxy AABB by Settings.b2_fatAABBFactor
    /// so that the proxy AABB is bigger than the client object. This allows the client
    /// object to move by small amounts without triggering a tree update.
    ///
    /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
    /// </summary>
    public class DynamicTree<T>
    {
        internal const int NullNode = -1;
        private int _freeList;
        private int _insertionCount;
        private int _nodeCapacity;
        private int _nodeCount;
        private DynamicTreeNode<T>[] _nodes;

        private const float SettingsAABBMultiplier = 2.0f;

        // Added by 'noone' to prevent highly symmetric cases to
        // update the whole tree at once.
        private float settingsRndExtension = 0.1f;

        private int _root;

        public int Root { get { return _root; } }
        public DynamicTreeNode<T>[] Nodes { get { return _nodes; } }

        public DynamicTree()
            : this(0.1f)
        {
        }

        /// <summary>
        /// Constructing the tree initializes the node pool.
        /// </summary>
        public DynamicTree(float rndExtension)
        {
            settingsRndExtension = rndExtension;
            _root = NullNode;

            _nodeCapacity = 16;
            _nodes = new DynamicTreeNode<T>[_nodeCapacity];

            // Build a linked list for the free list.
            for (int i = 0; i < _nodeCapacity - 1; ++i)
            {
                _nodes[i].ParentOrNext = i + 1;
            }
            _nodes[_nodeCapacity - 1].ParentOrNext = NullNode;
        }

        Random rnd = new Random();

        /// <summary>
        /// Create a proxy in the tree as a leaf node. We return the index
        /// of the node instead of a pointer so that we can grow
        /// the node pool.        
        /// /// </summary>
        /// <param name="aabb">The aabb.</param>
        /// <param name="userData">The user data.</param>
        /// <returns>Index of the created proxy</returns>
        public int AddProxy(ref JBBox aabb, T userData)
        {
            int proxyId = AllocateNode();

            _nodes[proxyId].MinorRandomExtension = (float)rnd.NextDouble() * settingsRndExtension;

            // Fatten the aabb.
            Vector3 r = new Vector3(_nodes[proxyId].MinorRandomExtension);
            _nodes[proxyId].AABB.Min = aabb.Min - r;
            _nodes[proxyId].AABB.Max = aabb.Max + r;
            _nodes[proxyId].UserData = userData;
            _nodes[proxyId].LeafCount = 1;

            InsertLeaf(proxyId);

            return proxyId;
        }

        /// <summary>
        /// Destroy a proxy. This asserts if the id is invalid.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
            Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            Debug.Assert(_nodes[proxyId].IsLeaf());

            RemoveLeaf(proxyId);
            FreeNode(proxyId);
        }

        /// <summary>
        /// Move a proxy with a swepted AABB. If the proxy has moved outside of its fattened AABB,
        /// then the proxy is removed from the tree and re-inserted. Otherwise
        /// the function returns immediately.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The aabb.</param>
        /// <param name="displacement">The displacement.</param>
        /// <returns>true if the proxy was re-inserted.</returns>
        public bool MoveProxy(int proxyId, ref JBBox aabb, Vector3 displacement)
        {
            Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);

            Debug.Assert(_nodes[proxyId].IsLeaf());

            if (_nodes[proxyId].AABB.Contains(ref aabb) != JBBox.ContainmentType.Disjoint)
            {
                return false;
            }

            RemoveLeaf(proxyId);

            // Extend AABB.
            JBBox b = aabb;
            Vector3 r = new Vector3(_nodes[proxyId].MinorRandomExtension);
            b.Min = b.Min - r;
            b.Max = b.Max + r;

            // Predict AABB displacement.
            Vector3 d = SettingsAABBMultiplier * displacement;
            //JVector randomExpansion = new JVector((float)rnd.Next(0, 10) * 0.1f, (float)rnd.Next(0, 10) * 0.1f, (float)rnd.Next(0, 10) * 0.1f);

            //d += randomExpansion;

            if (d.X < 0.0f)
            {
                b.Min.X += d.X;
            }
            else
            {
                b.Max.X += d.X;
            }

            if (d.Y < 0.0f)
            {
                b.Min.Y += d.Y;
            }
            else
            {
                b.Max.Y += d.Y;
            }

            if (d.Z < 0.0f)
            {
                b.Min.Z += d.Z;
            }
            else
            {
                b.Max.Z += d.Z;
            }

            _nodes[proxyId].AABB = b;

            InsertLeaf(proxyId);
            return true;
        }

        /// <summary>
        /// Get proxy user data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>the proxy user data or 0 if the id is invalid.</returns>
        public T GetUserData(int proxyId)
        {
            Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].UserData;
        }

        /// <summary>
        /// Get the fat AABB for a proxy.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="fatAABB">The fat AABB.</param>
        public void GetFatAABB(int proxyId, out JBBox fatAABB)
        {
            Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            fatAABB = _nodes[proxyId].AABB;
        }

        /// <summary>
        /// Compute the height of the binary tree in O(N) time. Should not be
        /// called often.
        /// </summary>
        /// <returns></returns>
        public int ComputeHeight()
        {
            return ComputeHeight(_root);
        }

        public void Query(Vector3 origin, Vector3 direction, List<int> collisions)
        {
            Stack<int> stack = stackPool.GetNew();

            stack.Push(_root);

            while (stack.Count > 0)
            {
                int nodeId = stack.Pop();
                DynamicTreeNode<T> node = _nodes[nodeId];

                if (node.AABB.RayIntersect(ref origin, ref direction))
                {
                    if (node.IsLeaf()) collisions.Add(nodeId);
                    else
                    {
                        if (_nodes[node.Child1].AABB.RayIntersect(ref origin, ref direction)) stack.Push(node.Child1);
                        if (_nodes[node.Child2].AABB.RayIntersect(ref origin, ref direction)) stack.Push(node.Child2);
                    }
                }
            }

            stackPool.GiveBack(stack);
        }

        public void Query(List<int> other, List<int> my, DynamicTree<T> tree)
        {
            Stack<int> stack1 = stackPool.GetNew();
            Stack<int> stack2 = stackPool.GetNew();

            stack1.Push(_root);
            stack2.Push(tree._root);

            while (stack1.Count > 0)
            {
                int nodeId1 = stack1.Pop();
                int nodeId2 = stack2.Pop();

                if (nodeId1 == NullNode) continue;
                if (nodeId2 == NullNode) continue;

                if (tree._nodes[nodeId2].AABB.Contains(ref _nodes[nodeId1].AABB) != JBBox.ContainmentType.Disjoint)
                {
                    if (_nodes[nodeId1].IsLeaf() && tree._nodes[nodeId2].IsLeaf())
                    {
                        my.Add(nodeId1);
                        other.Add(nodeId2);
                    }
                    else if (tree._nodes[nodeId2].IsLeaf())
                    {
                        stack1.Push(_nodes[nodeId1].Child1);
                        stack2.Push(nodeId2);

                        stack1.Push(_nodes[nodeId1].Child2);
                        stack2.Push(nodeId2);
                    }
                    else if (_nodes[nodeId1].IsLeaf())
                    {
                        stack1.Push(nodeId1);
                        stack2.Push(tree._nodes[nodeId2].Child1);

                        stack1.Push(nodeId1);
                        stack2.Push(tree._nodes[nodeId2].Child2);
                    }
                    else
                    {
                        stack1.Push(_nodes[nodeId1].Child1);
                        stack2.Push(tree._nodes[nodeId2].Child1);

                        stack1.Push(_nodes[nodeId1].Child1);
                        stack2.Push(tree._nodes[nodeId2].Child2);

                        stack1.Push(_nodes[nodeId1].Child2);
                        stack2.Push(tree._nodes[nodeId2].Child1);

                        stack1.Push(_nodes[nodeId1].Child2);
                        stack2.Push(tree._nodes[nodeId2].Child2);
                    }

                }

            }

            stackPool.GiveBack(stack1);
            stackPool.GiveBack(stack2);
        }


        private ResourcePool<Stack<int>> stackPool = new ResourcePool<Stack<int>>();

        /// <summary>
        /// Query an AABB for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied AABB.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="aabb">The aabb.</param>
        public void Query(List<int> my, ref JBBox aabb)
        {
            //Stack<int> _stack = new Stack<int>(256);
            Stack<int> _stack = stackPool.GetNew();

            _stack.Push(_root);

            while (_stack.Count > 0)
            {
                int nodeId = _stack.Pop();
                if (nodeId == NullNode)
                {
                    continue;
                }

                DynamicTreeNode<T> node = _nodes[nodeId];

                //if (JBBox.TestOverlap(ref node.AABB, ref aabb))
                if(aabb.Contains(ref node.AABB) != JBBox.ContainmentType.Disjoint)
                {
                    if (node.IsLeaf())
                    {
                        my.Add(nodeId);
                        //bool proceed = callback(nodeId);
                        //if (proceed == false)
                        //{
                        //    return;
                        //}
                    }
                    else
                    {
                        _stack.Push(node.Child1);
                        _stack.Push(node.Child2);
                    }
                }
            }

            stackPool.GiveBack(_stack);
        }

        private int CountLeaves(int nodeId)
        {
            if (nodeId == NullNode)
            {
                return 0;
            }

            Debug.Assert(0 <= nodeId && nodeId < _nodeCapacity);
            DynamicTreeNode<T> node = _nodes[nodeId];

            if (node.IsLeaf())
            {
                Debug.Assert(node.LeafCount == 1);
                return 1;
            }

            int count1 = CountLeaves(node.Child1);
            int count2 = CountLeaves(node.Child2);
            int count = count1 + count2;
            Debug.Assert(count == node.LeafCount);
            return count;
        }

        private void Validate()
        {
            CountLeaves(_root);
        }

        private int AllocateNode()
        {
            // Expand the node pool as needed.
            if (_freeList == NullNode)
            {
                Debug.Assert(_nodeCount == _nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                DynamicTreeNode<T>[] oldNodes = _nodes;
                _nodeCapacity *= 2;
                _nodes = new DynamicTreeNode<T>[_nodeCapacity];
                Array.Copy(oldNodes, _nodes, _nodeCount);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (int i = _nodeCount; i < _nodeCapacity - 1; ++i)
                {
                    _nodes[i].ParentOrNext = i + 1;
                }
                _nodes[_nodeCapacity - 1].ParentOrNext = NullNode;
                _freeList = _nodeCount;
            }

            // Peel a node off the free list.
            int nodeId = _freeList;
            _freeList = _nodes[nodeId].ParentOrNext;
            _nodes[nodeId].ParentOrNext = NullNode;
            _nodes[nodeId].Child1 = NullNode;
            _nodes[nodeId].Child2 = NullNode;
            _nodes[nodeId].LeafCount = 0;
            ++_nodeCount;
            return nodeId;
        }

        private void FreeNode(int nodeId)
        {
            Debug.Assert(0 <= nodeId && nodeId < _nodeCapacity);
            Debug.Assert(0 < _nodeCount);
            _nodes[nodeId].ParentOrNext = _freeList;
            _freeList = nodeId;
            --_nodeCount;
        }

        private void InsertLeaf(int leaf)
        {
            ++_insertionCount;

            if (_root == NullNode)
            {
                _root = leaf;
                _nodes[_root].ParentOrNext = NullNode;
                return;
            }

            // Find the best sibling for this node
            JBBox leafAABB = _nodes[leaf].AABB;
            int sibling = _root;
            while (_nodes[sibling].IsLeaf() == false)
            {
                int child1 = _nodes[sibling].Child1;
                int child2 = _nodes[sibling].Child2;

                // Expand the node's AABB.
                //_nodes[sibling].AABB.Combine(ref leafAABB);
                JBBox.CreateMerged(ref _nodes[sibling].AABB, ref leafAABB, out _nodes[sibling].AABB);

                _nodes[sibling].LeafCount += 1;

                float siblingArea = _nodes[sibling].AABB.Perimeter;
                JBBox parentAABB = new JBBox();
                //parentAABB.Combine(ref _nodes[sibling].AABB, ref leafAABB);
                JBBox.CreateMerged(ref _nodes[sibling].AABB, ref leafAABB, out _nodes[sibling].AABB);

                float parentArea = parentAABB.Perimeter;
                float cost1 = 2.0f * parentArea;

                float inheritanceCost = 2.0f * (parentArea - siblingArea);

                float cost2;
                if (_nodes[child1].IsLeaf())
                {
                    JBBox aabb = new JBBox();
                    //aabb.Combine(ref leafAABB, ref _nodes[child1].AABB);
                    JBBox.CreateMerged(ref leafAABB, ref _nodes[child1].AABB, out aabb);
                    cost2 = aabb.Perimeter + inheritanceCost;
                }
                else
                {
                    JBBox aabb = new JBBox();
                    //aabb.Combine(ref leafAABB, ref _nodes[child1].AABB);
                    JBBox.CreateMerged(ref leafAABB, ref _nodes[child1].AABB, out aabb);

                    float oldArea = _nodes[child1].AABB.Perimeter;
                    float newArea = aabb.Perimeter;
                    cost2 = (newArea - oldArea) + inheritanceCost;
                }

                float cost3;
                if (_nodes[child2].IsLeaf())
                {
                    JBBox aabb = new JBBox();
                    //aabb.Combine(ref leafAABB, ref _nodes[child2].AABB);
                    JBBox.CreateMerged(ref leafAABB, ref _nodes[child2].AABB, out aabb);
                    cost3 = aabb.Perimeter + inheritanceCost;
                }
                else
                {
                    JBBox aabb = new JBBox();
                    //aabb.Combine(ref leafAABB, ref _nodes[child2].AABB);
                    JBBox.CreateMerged(ref leafAABB, ref _nodes[child2].AABB, out aabb);
                    float oldArea = _nodes[child2].AABB.Perimeter;
                    float newArea = aabb.Perimeter;
                    cost3 = newArea - oldArea + inheritanceCost;
                }

                // Descend according to the minimum cost.
                if (cost1 < cost2 && cost1 < cost3)
                {
                    break;
                }

                // Expand the node's AABB to account for the new leaf.
                //_nodes[sibling].AABB.Combine(ref leafAABB);
                JBBox.CreateMerged(ref leafAABB, ref _nodes[sibling].AABB, out _nodes[sibling].AABB);

                // Descend
                if (cost2 < cost3)
                {
                    sibling = child1;
                }
                else
                {
                    sibling = child2;
                }
            }

            // Create a new parent for the siblings.
            int oldParent = _nodes[sibling].ParentOrNext;
            int newParent = AllocateNode();
            _nodes[newParent].ParentOrNext = oldParent;
            _nodes[newParent].UserData = default(T);
            //_nodes[newParent].AABB.Combine(ref leafAABB, ref _nodes[sibling].AABB);
            JBBox.CreateMerged(ref leafAABB, ref _nodes[sibling].AABB, out _nodes[newParent].AABB);
            _nodes[newParent].LeafCount = _nodes[sibling].LeafCount + 1;

            if (oldParent != NullNode)
            {
                // The sibling was not the root.
                if (_nodes[oldParent].Child1 == sibling)
                {
                    _nodes[oldParent].Child1 = newParent;
                }
                else
                {
                    _nodes[oldParent].Child2 = newParent;
                }

                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].ParentOrNext = newParent;
                _nodes[leaf].ParentOrNext = newParent;
            }
            else
            {
                // The sibling was the root.
                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].ParentOrNext = newParent;
                _nodes[leaf].ParentOrNext = newParent;
                _root = newParent;
            }
        }

        private void RemoveLeaf(int leaf)
        {
            if (leaf == _root)
            {
                _root = NullNode;
                return;
            }

            int parent = _nodes[leaf].ParentOrNext;
            int grandParent = _nodes[parent].ParentOrNext;
            int sibling;
            if (_nodes[parent].Child1 == leaf)
            {
                sibling = _nodes[parent].Child2;
            }
            else
            {
                sibling = _nodes[parent].Child1;
            }

            if (grandParent != NullNode)
            {
                // Destroy parent and connect sibling to grandParent.
                if (_nodes[grandParent].Child1 == parent)
                {
                    _nodes[grandParent].Child1 = sibling;
                }
                else
                {
                    _nodes[grandParent].Child2 = sibling;
                }
                _nodes[sibling].ParentOrNext = grandParent;
                FreeNode(parent);

                // Adjust ancestor bounds.
                parent = grandParent;
                while (parent != NullNode)
                {
                    //_nodes[parent].AABB.Combine(ref _nodes[_nodes[parent].Child1].AABB,
                    //                            ref _nodes[_nodes[parent].Child2].AABB);

                    JBBox.CreateMerged(ref _nodes[_nodes[parent].Child1].AABB,
                        ref _nodes[_nodes[parent].Child2].AABB,out _nodes[parent].AABB);

                    Debug.Assert(_nodes[parent].LeafCount > 0);
                    _nodes[parent].LeafCount -= 1;

                    parent = _nodes[parent].ParentOrNext;
                }
            }
            else
            {
                _root = sibling;
                _nodes[sibling].ParentOrNext = NullNode;
                FreeNode(parent);
            }
        }

        private int ComputeHeight(int nodeId)
        {
            if (nodeId == NullNode)
            {
                return 0;
            }

            Debug.Assert(0 <= nodeId && nodeId < _nodeCapacity);
            DynamicTreeNode<T> node = _nodes[nodeId];
            int height1 = ComputeHeight(node.Child1);
            int height2 = ComputeHeight(node.Child2);
            return 1 + Math.Max(height1, height2);
        }
    }
}