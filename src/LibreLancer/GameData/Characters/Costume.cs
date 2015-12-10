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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;

using LibreLancer.Ini;
using LibreLancer.Utf;

namespace LibreLancer.GameData.Characters
{
	public class Costume
	{
		public string Nickname { get; private set; }
		FreelancerData GameData;
		private string headName = null;
		private Bodypart head = null;
		public Bodypart Head
		{
			get
			{
				if (head == null && headName != null) head = GameData.Bodyparts.FindBodypart(headName);
				return head;
			}
		}

		private string bodyName = null;
		private Bodypart body = null;
		public Bodypart Body
		{
			get
			{
				if (body == null && bodyName != null) body = GameData.Bodyparts.FindBodypart(bodyName);
				return body;
			}
		}

		private string rightHandName = null;
		private Bodypart rightHand = null;
		public Bodypart RightHand
		{
			get
			{
				if (rightHand == null && rightHandName != null) rightHand = GameData.Bodyparts.FindBodypart(rightHandName);
				return rightHand;
			}
		}

		private string leftHandName = null;
		private Bodypart leftHand = null;
		public Bodypart LeftHand
		{
			get
			{
				if (leftHand == null && leftHandName != null) leftHand = GameData.Bodyparts.FindBodypart(leftHandName);
				return leftHand;
			}
		}

		private string accessoryName = null;
		private Accessory accessory = null;
		public Accessory Accessory
		{
			get
			{
				if (accessory == null && accessoryName != null) accessory = GameData.Bodyparts.FindAccessory(accessoryName);
				return accessory;
			}
		}

		public Costume(Section s, FreelancerData gdata)
		{
			GameData = gdata;
			foreach (Entry e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					//if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					Nickname = e[0].ToString();
					break;
				case "head":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					//if (headName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					headName = e[0].ToString();
					break;
				case "body":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					//if (bodyName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					bodyName = e[0].ToString();
					break;
				case "righthand":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					//if (rightHandName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					rightHandName = e[0].ToString();
					break;
				case "lefthand":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					//if (leftHandName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					leftHandName = e[0].ToString();
					break;
				case "accessory":
					FLLog.Error ("Costume","Accessories not implemented");
					break;
				default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
				}
			}
		}

		public Costume(string headName, string bodyName, string accessoryName, FreelancerData gdata)
		{
			GameData = gdata;
			this.headName = headName == string.Empty ? null : headName;
			this.bodyName = bodyName == string.Empty ? null : bodyName;
			this.accessoryName = accessoryName == string.Empty ? null : accessoryName;
		}
	}
}
