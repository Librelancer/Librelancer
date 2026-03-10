// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LibreLancer.Utf.Ale
{
	public class AlchemyNode
    {
        public string NodeName = "";
		public string? ClassName;
		public uint CRC;
		public readonly List<AleParameter> Parameters = [];

        public override string? ToString()
		{
			return ClassName;
		}

		public bool TryGetParameter(AleProperty name, [MaybeNullWhen(false)] out AleParameter parameter)
		{
			parameter = null;
			foreach (var p in Parameters.Where(p => p.Name == name))
            {
                parameter = p;
                return true;
            }

			return false;
		}

        private bool TryGetObject<T>(AleProperty name, [MaybeNullWhen(false)] out T value)
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
            return TryGetObject(name, out bool v) ? v : def;
        }

        public string? GetString(AleProperty name)
        {
            return TryGetObject(name, out string? v) ? v : "";
        }

        public AlchemyFloatAnimation? GetFloatAnimation(AleProperty name, bool haveDef = true)
        {
            if (TryGetObject(name, out AlchemyFloatAnimation? v))
            {
                return v;
            }

            if (!haveDef)
            {
                return null;
            }

            var fa = new AlchemyFloatAnimation();
            fa.Items.Add(new AlchemyFloats() { Keyframes = [new(0, 0)]});
            return fa;

        }

        public AlchemyCurveAnimation? GetCurveAnimation(AleProperty name, bool haveDef = true)
        {
            if (TryGetObject(name, out AlchemyCurveAnimation? v))
            {
                return v;
            }

            if (!haveDef)
            {
                return null;
            }

            var fa = new AlchemyCurveAnimation();
            fa.Items.Add(new AlchemyCurve() { Value = 0});
            return fa;
        }

        public AlchemyColorAnimation? GetColorAnimation(AleProperty name, float def = 0)
        {
            if (TryGetObject(name, out AlchemyColorAnimation? v))
            {
                return v;
            }

            var fa = new AlchemyColorAnimation();
            fa.Items.Add(new AlchemyColors() { Keyframes = [new(0, Color3f.White)]});
            return fa;
        }

        public AlchemyTransform? GetTransform(AleProperty name)
        {
            return TryGetObject(name, out AlchemyTransform? v) ? v : new AlchemyTransform();
        }
	}
}

