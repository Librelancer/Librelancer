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
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using LibreLancer.Utf;

namespace LibreLancer.Utf.Mat
{
	public class Material
	{
		protected static Texture2D nullTexture;
		protected ILibFile textureLibrary;
		protected Shader effect = null;

		public bool IsDisposed { get { return false; } }

		public string type { get; private set; }

		/// <summary>
		/// Material Name
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Diffuse Texture Flags
		/// </summary>
		public int DtFlags { get; private set; }

		public string DtName { get; private set; }

		/// <summary>
		/// Diffuse Texture
		/// </summary>
		public TextureData Dt { get; set; }

		/// <summary>
		/// Diffuse Colour
		/// </summary>
		public Color4 Dc { get { return _dc; } set { _dc = value; } }

		Color4 _dc = Color4.White;

		/// <summary>
		/// Emmisive Colour
		/// </summary>
		public Color4 Ec { get { return _ec; } set { _ec = value; } }

		Color4 _ec = new Color4 (0, 0, 0, 0);

		/// <summary>
		/// B? Texture Flags
		/// </summary>
		public int BtFlags { get; private set; }

		private string btName;

		/// <summary>
		/// B? Texture
		/// </summary>
		public TextureData Bt { get; private set; }

		/// <summary>
		/// Opacity
		/// </summary>
		public float Oc = -1;

		/// <summary>
		/// Emissive Texture Flags
		/// </summary>
		public int EtFlags { get; private set; }

		private string etName;

		/// <summary>
		/// Emissive Texture
		/// </summary>
		public TextureData Et { get; set; }

		public Color4 Ac { get; set; }

		/// <summary>
		/// Flip U
		/// </summary>
		public int FlipU { get; set; }

		/// <summary>
		/// Flip V
		/// </summary>
		public int FlipV { get; set; }

		/// <summary>
		/// Alpha
		/// </summary>
		public float Alpha { get; private set; }

		/// <summary>
		/// max opacity (0-1), 0 (f)
		/// </summary>
		public float Fade { get; private set; }

		/// <summary>
		/// scale amount, 0 (f)
		/// </summary>
		public float Scale { get; private set; }

		/// <summary>
		/// Detail Map 0 Flags
		/// </summary>
		public int Dm0Flags { get; private set; }

		private string dm0Name;

		/// <summary>
		/// Detail Map 0
		/// </summary>
		public TextureData Dm0 { get; set; }

		/// <summary>
		/// Detail Map 1 Flags
		/// </summary>
		public int Dm1Flags { get; private set; }

		private string dm1Name;

		/// <summary>
		/// Detail Map 1
		/// </summary>
		public TextureData Dm1 { get; set; }

		/// <summary>
		/// Tile Rate 0 tile amount (1=no tiling, >1 creates multiple tiles), 0 (f)
		/// </summary>
		public float TileRate0 { get; set; }

		/// <summary>
		/// Tile Rate 1 tile amount (1=no tiling, >1 creates multiple tiles), 0 (f)
		/// </summary>
		public float TileRate1 { get; set; }

		/// <summary>
		/// Tile Rate tile amount (1=no tiling, >1 creates multiple tiles), 0 (f)
		/// </summary>
		public float TileRate { get; set; }

		/// <summary>
		/// Detail Map Flags
		/// </summary>
		public int DmFlags { get; private set; }

		private string dmName;

		/// <summary>
		/// Detail Map
		/// </summary>
		public TextureData Dm { get; private set; }

		static List<string> basicMaterials = new List<string> {
			"DcDt", "DcDtTwo", "DcDtEc", "DcDtEt", "DcDtEcEt",
			"DcDtOcOt", "DcDtBtOcOt", "DcDtBtOcOtTwo", "DcDtEcOcOt",
			"DcDtOcOtTwo", "DcDtBt"
		};
		RenderMaterial _rmat;
		public RenderMaterial Render {
			get {
				if (_rmat == null)
					Initialize ();
				return _rmat;
			}
		}
		bool isBasic = false;

		protected Material (IntermediateNode node, ILibFile library, string type)
		{
           
			this.textureLibrary = library;
			this.type = type;

			Name = node.Name;

			foreach (LeafNode n in node) {
				if (!parentNode (n))
					throw new Exception ("Invalid node in node " + node.Name + ": " + n.Name);
			}
		}

		public Material ()
		{
			textureLibrary = new TxmFile ();
			type = "DcDt";

			Name = "NullMaterial";

			DtFlags = -1;
			DtName = null;
		}

		public static Material FromNode (IntermediateNode node, ILibFile textureLibrary)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			if (textureLibrary == null)
				throw new ArgumentNullException ("textureLibrary");

			LeafNode typeNode = node ["Type"] as LeafNode;
			if (typeNode == null)
				throw new Exception ("Invalid or missing type node in " + node.Name);


			string type = typeNode.StringData;
			type = MaterialMap.Instance.Get (type);
			var mat = new Material (node, textureLibrary, type);
			if (basicMaterials.Contains (type)) {
				mat.isBasic = true;
			} else
				switch (type) {
				case "Nebula":
				case "AtmosphereMaterial":
				case "DetailMapMaterial":
				case "DetailMap2Dm1Msk2PassMaterial":
				case "IllumDetailMapMaterial":
				case "Masked2DetailMapMaterial":
					break;
				default:
					throw new Exception ("Invalid material type: " + type);
				}
			FLLog.Debug ("Material", "Created " + type);
			return mat;
		}

		protected virtual bool parentNode (LeafNode n)
		{
			switch (n.Name.ToLowerInvariant ()) {
			//standard flags (Dc*)
			case "dt_flags":
				DtFlags = n.Int32Data.Value;
				break;
			case "dt_name":
				DtName = n.StringData;
				break;
			case "dc":
				Dc = n.ColorData.Value;
				break;
			case "ec":
				Ec = n.ColorData.Value;
				break;
			case "bt_flags":
				BtFlags = n.Int32Data.Value;
				break;
			case "bt_name":
				btName = n.StringData;
				break;
			case "et_flags":
				EtFlags = n.Int32Data.Value;
				break;
			case "et_name":
				etName = n.StringData;
				break;
			case "oc":
				Oc = n.SingleData.Value;
				break;
			case "type":
				break;
			//different material types
			case "ac":
				Ac = n.ColorData.Value;
				break;
			case "flip u":
				FlipU = n.Int32Data.Value;
				break;
			case "flip v":
				FlipV = n.Int32Data.Value;
				break;
			case "alpha":
				Alpha = n.SingleData.Value;
				break;
			case "fade":
				Fade = n.SingleData.Value;
				break;
			case "scale":
				Scale = n.SingleData.Value;
				break;
			case "dm0_flags":
				Dm0Flags = n.Int32Data.Value;
				break;
			case "dm0_name":
				dm0Name = n.StringData;
				break;
			case "dm1_flags":
				Dm1Flags = n.Int32Data.Value;
				break;
			case "dm1_name":
				dm1Name = n.StringData;
				break;
			case "tilerate0":
				TileRate0 = n.SingleData.Value;
				break;
			case "tilerate1":
				TileRate1 = n.SingleData.Value;
				break;
			case "tilerate":
				TileRate = n.SingleData.Value;
				break;
			case "dm_flags":
				DmFlags = n.Int32Data.Value;
				break;
			case "dm_name":
				dmName = n.StringData;
				break;
			default:
				throw new NotImplementedException ();
				;
			}

			return true;
		}

		public virtual void Initialize ()
		{
			if (DtName != null)
				Dt = textureLibrary.FindTexture (DtName);
			if (etName != null)
				Et = textureLibrary.FindTexture (etName);
			if (btName != null)
				Bt = textureLibrary.FindTexture (btName);
			if (dm0Name != null)
				Dm0 = textureLibrary.FindTexture (dm0Name);
			if (dm1Name != null)
				Dm1 = textureLibrary.FindTexture (dm1Name);
			if (dmName != null)
				Dm = textureLibrary.FindTexture (dmName);
			if (isBasic) {
				var bm = new BasicMaterial (type);
				_rmat = bm;
				//set up material
				bm.Dc = Dc;
				bm.Oc = Oc;
				bm.Ec = Ec;
				if (Dt != null) {
					if (Dt.Texture == null)
						throw new Exception ();
					bm.DtSampler = Dt.Texture;
				}
				if (Et != null)
					bm.EtSampler = Et.Texture;
				if (type.Contains ("Ot"))
					bm.AlphaEnabled = true;
			} else {
				switch (type) {
				case "Nebula":
					var nb = new NebulaMaterial ();
					_rmat = nb;
					if (Dt != null)
						nb.DtSampler = Dt.Texture;
					break;
				case "AtmosphereMaterial":
					var am = new AtmosphereMaterial ();
					_rmat = am;
					am.Dc = Dc;
					am.Ac = Ac;
					am.Alpha = Alpha;
					am.Fade = Fade;
					am.Scale = Scale;
					if (Dt != null)
						am.DtSampler = Dt.Texture;
					break;
				case "Masked2DetailMapMaterial":
					var m2 = new Masked2DetailMapMaterial ();
					_rmat = m2;
					m2.Dc = Dc;
					m2.Ac = Ac;
					m2.TileRate0 = TileRate0;
					m2.TileRate1 = TileRate1;
					m2.FlipU = FlipU;
					m2.FlipV = FlipV;
					if (Dt != null)
						m2.DtSampler = Dt.Texture;
					if (Dm0 != null)
						m2.Dm0Sampler = Dm0.Texture;
					if (Dm1 != null)
						m2.Dm1Sampler = Dm1.Texture;
					break;
				case "DetailMap2Dm1Msk2PassMaterial":
					var dm2p = new DetailMap2Dm1Msk2PassMaterial ();
					_rmat = dm2p;
					dm2p.Dc = Dc;
					dm2p.Ac = Ac;
					dm2p.FlipU = FlipU;
					dm2p.FlipV = FlipV;
					dm2p.TileRate = TileRate;
					if (Dt != null)
						dm2p.DtSampler = Dt.Texture;
					if (Dm1 != null)
						dm2p.Dm1Sampler = Dm1.Texture;
					break;
				case "DetailMapMaterial":
					var dm = new DetailMapMaterial ();
					_rmat = dm;
					dm.Dc = Dc;
					dm.Ac = Ac;
					dm.FlipU = FlipU;
					dm.FlipV = FlipV;
					dm.TileRate = TileRate;
					if (Dm != null)
						dm.DmSampler = Dm.Texture;
					if (Dt != null)
						dm.DtSampler = Dt.Texture;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public void Resized ()
		{
			//if (effect != null)
			//effect.SetParameter ("Projection", camera.Projection);
		}

		Matrix4 ViewProjection = Matrix4.Identity;

		public void Update (Camera camera)
		{
			ViewProjection = camera.ViewProjection;
			if (Render != null)
				Render.ViewProjection = ViewProjection;
		}
	}
}

