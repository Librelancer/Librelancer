// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyNode
    {
        public string NodeName = "";
		public string ClassName;
		public uint CRC;
		public List<AleParameter> Parameters = new List<AleParameter>();
		public AlchemyNode ()
		{
		}
		public override string ToString ()
		{
			return ClassName;
		}
		public bool TryGetParameter(AleProperty name, out AleParameter parameter)
		{
			parameter = null;
			foreach (var p in Parameters) {
				if (p.Name == name) {
					parameter = p;
					return true;
				}
			}
			return false;
		}

        bool TryGetObject<T>(AleProperty name, out T value)
        {
            if (!TryGetParameter(name, out var p))
            {
                value = default;
                return false;
            }
            value = (T)p.Value;
            return true;
        }

        public bool GetBoolean(AleProperty name, bool def = false)
        {
            if (TryGetObject(name, out bool v))
                return v;
            return def;
        }

        public string GetString(AleProperty name)
        {
            if (TryGetObject(name, out string v))
                return v;
            return "";
        }

        public AlchemyFloatAnimation GetFloatAnimation(AleProperty name, bool haveDef = true)
        {
            if (TryGetObject(name, out AlchemyFloatAnimation v))
            {
                return v;
            }
            else if (haveDef)
            {
                var fa = new AlchemyFloatAnimation();
                fa.Items.Add(new AlchemyFloats() { Keyframes = [new(0, 0)]});
                return fa;
            }
            return null;
        }

        public AlchemyCurveAnimation GetCurveAnimation(AleProperty name, bool haveDef = true)
        {
            if (TryGetObject(name, out AlchemyCurveAnimation v))
            {
                return v;
            }
            else if (haveDef)
            {
                var fa = new AlchemyCurveAnimation();
                fa.Items.Add(new AlchemyCurve() { Value = 0});
                return fa;
            }
            return null;
        }

        public AlchemyColorAnimation GetColorAnimation(AleProperty name, float def = 0)
        {
            if (TryGetObject(name, out AlchemyColorAnimation v))
                return v;
            var fa = new AlchemyColorAnimation();
            fa.Items.Add(new AlchemyColors() { Keyframes = [new(0, Color3f.White)]});
            return fa;
        }

        public AlchemyTransform GetTransform(AleProperty name)
        {
            if (TryGetObject(name, out AlchemyTransform v))
                return v;
            return new AlchemyTransform();
        }
	}
}

