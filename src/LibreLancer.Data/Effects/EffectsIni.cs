// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Data.Effects
{
	public class EffectsIni : IniFile
	{
        [Section("viseffect")]
		public List<VisEffect> VisEffects = new List<VisEffect>();
        [Section("beamspear")]
        public List<BeamSpear> BeamSpears = new List<BeamSpear>();
        [Section("beambolt")]
        public List<BeamBolt> BeamBolts = new List<BeamBolt>();
        [Section("effect")]
		public List<Effect> Effects = new List<Effect>();

        public void AddIni(string ini) => ParseAndFill(ini);

		public Effect FindEffect(string nickname)
		{
			var result = from Effect e in Effects where e.Nickname.Equals(nickname,StringComparison.OrdinalIgnoreCase) select e;
			if (result.Count() == 1)
				return result.First();
			return null;
		}

		public VisEffect FindVisEffect(string nickname)
		{
			var result = from VisEffect v in VisEffects where v.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select v;
			if (result.Count() >= 1)
				return result.First();
			return null;
		}
	}
}
