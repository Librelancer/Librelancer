using System.Collections.Generic;
using System.Numerics;

namespace LancerEdit.GameContent
{
    public class PatrolRouteBuilder
    {
        public bool IsActive { get; private set; } = false;
        public List<Vector3> Points { get; } = new();

        public void Start()
        {
            IsActive = true;
            Points.Clear();
        }

        public void AddPoint(Vector3 point)
        {
            if (IsActive)
                Points.Add(point);
        }

        public void Cancel()
        {
            IsActive = false;
            Points.Clear();
        }

        public List<Vector3> Finish()
        {
            IsActive = false;
            var result = new List<Vector3>(Points);
            Points.Clear();
            return result;
        }
    }
} 