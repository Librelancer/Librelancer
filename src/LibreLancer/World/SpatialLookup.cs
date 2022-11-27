// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.World
{
    public class SpatialLookup
    {
        public const float CELL_SIZE = 15000;

        private Dictionary<int, List<GameObject>> cells = new Dictionary<int, List<GameObject>>();
        private Dictionary<GameObject,CellRef> objToCell = new Dictionary<GameObject, CellRef>();

        public void AddObject(GameObject obj, Vector3 position)
        {
            int key = CellKey(Floored(position.X / CELL_SIZE), Floored(position.Z / CELL_SIZE));
            var cell = GetCell(key, true);
            AddToCell(key, cell, obj);
        }
        
        public void RemoveObject(GameObject obj)
        {
            var cell = objToCell[obj];
            RemoveFromCell(cell, obj);
        }

        struct CellRef
        {
            public int Key;
            public List<GameObject> Cell;

            public CellRef(int key, List<GameObject> cell)
            {
                Key = key;
                Cell = cell;
            }
        }

        void RemoveFromCell(CellRef r, GameObject obj)
        {
            r.Cell.Remove(obj);
            if (r.Cell.Count == 0) cells.Remove(r.Key);
        }

        void AddToCell(int key, List<GameObject> cell, GameObject obj)
        {
            var r = new CellRef(key, cell);
            cell.Add(obj);
            objToCell[obj] = r;
        }
        
        public void UpdatePosition(GameObject obj, Vector3 newPosition)
        {
            var newKey = CellKey(Floored(newPosition.X / CELL_SIZE), Floored(newPosition.Z / CELL_SIZE));
            var newCell = GetCell(newKey, true);
            var oldCell = objToCell[obj];
            if (oldCell.Cell == newCell) return;
            #if DEBUG
            /* Check our state is still valid */
            if (!oldCell.Cell.Contains(obj)) throw new Exception("SpatialLookup corrupted");
            #endif
            RemoveFromCell(oldCell, obj);
            AddToCell(newKey, newCell, obj);
        }

        public IEnumerable<GameObject> GetNearbyObjects(GameObject self, Vector3 position, float range = 0)
        {
            if (range > 0)
            {
                int minX = Floored((position.X - range) / CELL_SIZE);
                int maxX = Floored((position.X + range) / CELL_SIZE);
                int minZ = Floored((position.Z - range) / CELL_SIZE);
                int maxZ = Floored((position.Z + range) / CELL_SIZE);
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var cell = GetCell(CellKey(x, z), false);
                        if(cell != null)
                            foreach (var obj in cell) if(obj != self) yield return obj;
                    }
                }
            }
            else
            {
                var myCellX = Floored(position.X / CELL_SIZE);
                var myCellZ = Floored(position.Z / CELL_SIZE);
                var myCell = GetCell(CellKey(myCellX, myCellZ), false);
                if (myCell != null)
                    foreach (var obj in myCell) if (obj != self) yield return obj;
            }
        }
        
        public Vector3 GetCellCoordinate(Vector3 pos)
        {
            return new Vector3(MathF.Floor(pos.X / CELL_SIZE), 0,  MathF.Floor(pos.Y / CELL_SIZE));
        }

        List<GameObject> GetCell(int key, bool create)
        {
            if (!cells.TryGetValue(key, out var cell) && create)
            {
                cell = new List<GameObject>();
                cells[key] = cell;
            }
            return cell;
        }

        int Floored(float f) => (int) MathF.Floor(f);
        
        int CellKey(int cellX, int cellZ)
        {
            return (cellX * 73856093) ^ (cellZ * 19349663);
        }
    }
}