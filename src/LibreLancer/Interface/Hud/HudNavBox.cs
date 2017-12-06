/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class HudNavBox
	{
		//Box Position
		static readonly Vector2 BoxPos = new Vector2(0, 0.91f);
		static readonly Vector2 BoxScale = new Vector2(4.5f, 5.6f);
		static readonly string[] Boxes = new string[] {
			"",
			"hud_maneuverbox1.cmp",
			"hud_maneuverbox2.cmp",
			"hud_maneuverbox3.cmp",
			"hud_maneuverbox4.cmp",
			"hud_maneuverbox5.cmp"
		};
		//Icon Positions
		static readonly float[] IconPositions = new float[] {
			-0.218f, 0.925f,
			-0.063f, 0.925f,
			0.071f, 0.914f,
			0.228f, 0.925f
		};
		//TODO: These aren't accurate yet
		//Different scale + offset based on icon? (Does FL actually hardcode all this?)
		static readonly float[][] BaseIconPositions = new float[][] {
			new float[0],
			new float[] { //1 icon
				0, 0.925f
			},
			new float[] { //2 icons
				-0.218f, 0.925f,
				-0.063f, 0.925f,
			},
			new float[] { //3 icons
				-0.218f, 0.925f,
				-0.063f, 0.925f,
				0.071f, 0.914f,
			},
			new float[] { //4 icons
				-0.22f, 0.925f,
				-0.0775f, 0.925f,
				0.065f, 0.925f,
				0.21f, 0.915f
			},
			new float[] { //5 icons
				-0.25f, 0.93f,
				-0.11f, 0.925f,
				0.02f, 0.924f,
				0.15f, 0.915f,
				0.27f, 0.925f
			}
		};
		Vector2 IconScale = new Vector2(4.26f, 5.48f);
		//Hotspot Icons
		UIManager manager;
		List<Maneuver> maneuvers;
		List<GameData.BaseHotspot> hotspots;
		HudToggleButtonElement[] toggleButtons;
		HudModelElement navbuttons;
		public HudNavBox(List<Maneuver> maneuvers, UIManager manager)
		{
			this.manager = manager;
			this.maneuvers = maneuvers;
			navbuttons = new HudModelElement(manager, Boxes[maneuvers.Count], BoxPos.X, BoxPos.Y, BoxScale.X, BoxScale.Y);
			toggleButtons = new HudToggleButtonElement[maneuvers.Count];
			for (int i = 0; i < maneuvers.Count; i++)
			{
				toggleButtons[i] = new HudToggleButtonElement(
					manager,
					maneuvers[i].ActiveModel,
					maneuvers[i].InactiveModel,
					IconPositions[i * 2],
					IconPositions[i * 2 + 1],
					IconScale.X,
					IconScale.Y
				)
				{ Tag = "mnv" + i.ToString() };
			}
		}

		int idx = -1;
		public HudNavBox(Dictionary<string,string> icons, List<GameData.BaseHotspot> hotspots, UIManager manager)
		{
			this.manager = manager;
			this.hotspots = hotspots;
			navbuttons = new HudModelElement(manager, Boxes[hotspots.Count], BoxPos.X, BoxPos.Y, BoxScale.X, BoxScale.Y);
			toggleButtons = new HudToggleButtonElement[hotspots.Count];
			IconScale = new Vector2(2.08f, 2.96f);
			for (int i = 0; i < hotspots.Count; i++)
			{
				string hack = null;
				if (!icons.ContainsKey(hotspots[i].SetVirtualRoom ?? hotspots[i].Room))
					hack = "Cityscape"; //HACK: This probably means FL doesn't determine icons based on room name
				toggleButtons[i] = new HudBaseButtonElement(
					manager,
					icons[hack ?? hotspots[i].SetVirtualRoom ?? hotspots[i].Room],
					BaseIconPositions[hotspots.Count][i * 2],
					BaseIconPositions[hotspots.Count][i * 2 + 1],
					IconScale.X,
					IconScale.Y
				)
				{ Tag = "mnv" + i.ToString() };
			}
		}

		public void ProcessClick(string tag, Func<string,bool> callback)
		{
			var index = tag[3] - '0';
			if (hotspots != null)
			{
				callback(hotspots[index].Name);
				return;
			}
			if (!callback(maneuvers[index].Action)) return;
			InternalSetActive(index);
		}

		public void SetActive(string name)
		{
			if (hotspots != null)
			{
				for (int i = 0; i < hotspots.Count; i++)
				{
					if (hotspots[i].SetVirtualRoom == name)
					{
						InternalSetActive(i);
						return;
					}
				}
				for (int i = 0; i < hotspots.Count; i++)
				{
					if (hotspots[i].Room == name)
					{
						InternalSetActive(i);
						return;
					}
				}
			}
			else
			{
				for (int i = 0; i < maneuvers.Count; i++)
				{
					if (maneuvers[i].Action == name)
					{
						InternalSetActive(i);
						return;
					}
				}
			}
		}

		void InternalSetActive(int index)
		{
			for (int i = 0; i < toggleButtons.Length; i++)
			{
				toggleButtons[i].State = index == i ? ToggleState.Active : ToggleState.Inactive;
			}
		}

		public void Show()
		{
			manager.Elements.Add(navbuttons);
			foreach (var elem in toggleButtons)
				manager.Elements.Add(elem);
		}

		public void Hide()
		{
			manager.Elements.Remove(navbuttons);
			foreach (var elem in toggleButtons)
				manager.Elements.Remove(elem);
		}
	}
}
