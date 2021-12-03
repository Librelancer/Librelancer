// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Numerics;

namespace LibreLancer.AI
{
    public class AiAttackState : AiState
    {
        private GameObject target;
        public AiAttackState(GameObject target)
        {
            this.target = target;
        }
        
        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            if (obj.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                weapons.AimPoint = Vector3.Transform(Vector3.Zero, target.WorldTransform);
                weapons.FireAll();
            }
        }
    }
}