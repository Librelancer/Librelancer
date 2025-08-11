// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyNode
	{
		public string Name;
		public uint CRC;
		public List<AleParameter> Parameters = new List<AleParameter>();
		public AlchemyNode ()
		{
		}
		public override string ToString ()
		{
			return Name;
		}
		public bool TryGetParameter(string name, out AleParameter parameter)
		{
			parameter = null;
			var nm = name.ToUpperInvariant ();
			foreach (var p in Parameters) {
				if (p.Name.ToUpperInvariant () == nm) {
					parameter = p;
					return true;
				}
			}
			return false;
		}

        bool TryGetObject<T>(string name, out T value)
        {
            if (!TryGetParameter(name, out var p))
            {
                value = default;
                return false;
            }
            value = (T)p.Value;
            return true;
        }

        public bool GetBoolean(string name, bool def = false)
        {
            if (TryGetObject(name, out bool v))
                return v;
            return def;
        }

        public string GetString(string name)
        {
            if (TryGetObject(name, out string v))
                return v;
            return "";
        }

        public AlchemyFloatAnimation GetFloatAnimation(string name, bool haveDef = true)
        {
            if (TryGetObject(name, out AlchemyFloatAnimation v))
            {
                return v;
            }
            else if (haveDef)
            {
                var fa = new AlchemyFloatAnimation();
                fa.Items.Add(new AlchemyFloats() { Data = [(0, 0)]});
                return fa;
            }
            return null;
        }

        public AlchemyCurveAnimation GetCurveAnimation(string name, bool haveDef = true)
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

        public AlchemyColorAnimation GetColorAnimation(string name, float def = 0)
        {
            if (TryGetObject(name, out AlchemyColorAnimation v))
                return v;
            var fa = new AlchemyColorAnimation();
            fa.Items.Add(new AlchemyColors() { Data = [new(0, Color3f.White)]});
            return fa;
        }

        public AlchemyTransform GetTransform(string name)
        {
            if (TryGetObject(name, out AlchemyTransform v))
                return v;
            return new AlchemyTransform();
        }
	}
}

