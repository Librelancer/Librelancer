// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using LibreLancer.Utf;

namespace LibreLancer.Utf.Mat
{
	public class Material
	{
		protected static Texture2D nullTexture;
		protected ILibFile textureLibrary;
		protected Shader effect = null;

		public bool Loaded = true;

		string type;
		public string Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
				isBasic = basicMaterials.Contains(type);
			}
		}

		/// <summary>
		/// Material Name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Diffuse Texture Flags
		/// </summary>
		public int DtFlags { get; set; }

		public string DtName { get; set; }


		/// <summary>
		/// Diffuse Colour
		/// </summary>
		public Color4 Dc { get { return _dc; } set { _dc = value; } }

		Color4 _dc = Color4.White;

		/// <summary>
		/// Emmisive Colour
		/// </summary>
		public Color4 Ec { get { return _ec; } set { _ec = value; } }

		Color4 _ec = new Color4(0, 0, 0, 0);

		/// <summary>
		/// B? Texture Flags
		/// </summary>
		public int BtFlags { get; private set; }

		private string btName;


		public int NtFlags { get; private set; }
		public string NtName;

		/// <summary>
		/// Opacity
		/// </summary>
		public float? Oc;

		/// <summary>
		/// Emissive Texture Flags
		/// </summary>
		public int EtFlags { get; private set; }

		public string EtName;

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
		/// Detail Map 1 Flags
		/// </summary>
		public int Dm1Flags { get; private set; }

		public string Dm1Name;

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

		public string DmName;

		static List<string> basicMaterials = new List<string> {
			"Dc", //DcDt buggy
			"DcDt", "DcDtTwo", "DcDtEc", "DcDtEt", "DcDtEcEt",
			"DcDtOcOt", "DcDtBtOcOt", "DcDtBtOcOtTwo", "DcDtEcOcOt",
			"DcDtOcOtTwo", "DcDtBt", "DcDtBtTwo", "BtDetailMapMaterial",
			"DcDtEcOcOtTwo", "DcDtEtTwo", "DcDtEcTwo"
		};
		RenderMaterial _rmat;
		public RenderMaterial Render
		{
			get
			{
				if (!Loaded) throw new Exception("Material unloaded"); //Should never happen
				if (_rmat == null)
					Initialize();
				return _rmat;
			}
		}
		bool isBasic = false;

		protected Material(IntermediateNode node, ILibFile library, string type)
		{

			this.textureLibrary = library;
			this.type = type;

			Name = node.Name;

			foreach (LeafNode n in node)
			{
				if (!parentNode(n))
					throw new Exception("Invalid node in node " + node.Name + ": " + n.Name);
			}
		}

		public Material(ResourceManager res)
		{
			textureLibrary = res;
			type = "DcDt";

			Name = "NullMaterial";
			DtFlags = 0;
			Dc = Color4.Magenta;
			DtName = null;
			isBasic = true;
		}

		public static Material FromNode(IntermediateNode node, ILibFile textureLibrary)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (textureLibrary == null)
				throw new ArgumentNullException("textureLibrary");

			LeafNode typeNode = node["Type"] as LeafNode;
			if (typeNode == null)
				throw new Exception("Invalid or missing type node in " + node.Name);


			string type = typeNode.StringData;
			type = MaterialMap.Instance.Get(type) ?? type;
			type = MaterialMap.Instance.Get(node.Name.ToLowerInvariant()) ?? type;

			if (type == "HighGlassMaterial" || 
			    type == "HUDAnimMaterial" || 
			    type == "HUDIconMaterial" ||
			    type == "PlanetWaterMaterial")
			{
				type = "DcDtOcOt"; //HACK: Should do env mapping
			}
			if (type == "ExclusionZoneMaterial")
			{
				type = "DcDt"; //HACK: This is handled in NebulaRenderer, not in Material.cs
			}
			var mat = new Material(node, textureLibrary, type);
			if (basicMaterials.Contains(type))
			{
				mat.isBasic = true;
			}
			else
				switch (type)
				{
					case "Nebula":
					case "NebulaTwo":
					case "AtmosphereMaterial":
					case "DetailMapMaterial":
					case "DetailMap2Dm1Msk2PassMaterial":
					case "IllumDetailMapMaterial":
					case "Masked2DetailMapMaterial":
					case "NomadMaterialNoBendy":
					case "NomadMaterial":
						break;
					default:
						throw new Exception("Invalid material type: " + type);
				}
			return mat;
		}

		protected virtual bool parentNode(LeafNode n)
		{
			switch (n.Name.ToLowerInvariant())
			{
				//standard flags (Dc*)
				case "dt_flags":
					DtFlags = n.Int32ArrayData [0];
					break;
				case "dt_name":
					DtName = n.StringData;
					break;
				case "dc":
					if (n.ColorData == null)
						Dc = new Color4 (n.SingleArrayData [0], n.SingleArrayData [1], n.SingleArrayData [2], 1);
					else
						Dc = n.ColorData.Value;
					break;
				case "ec":
					if (n.ColorData == null)
						Ec = new Color4 (n.SingleArrayData [0], n.SingleArrayData [1], n.SingleArrayData [2], 1);
					else
						Ec = n.ColorData.Value;
					break;
				case "bt_flags":
					BtFlags = n.Int32ArrayData[0];
					break;
				case "bt_name":
					btName = n.StringData;
					break;
				case "et_flags":
					EtFlags = n.Int32ArrayData [0];
					break;
				case "et_name":
					EtName = n.StringData;
					break;
				case "oc":
					Oc = n.SingleArrayData [0];
					break;
				case "type":
					break;
				//different material types
				case "ac":
					if (n.ColorData == null)
						Ac = new Color4 (n.SingleArrayData [0], n.SingleArrayData [1], n.SingleArrayData [2], 1);
					else
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
					Dm1Name = n.StringData;
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
					DmName = n.StringData;
					break;
				case "nt_name":
					NtName = n.StringData;
					break;
				case "nt_flags":
					NtFlags = n.Int32Data.Value;
					break;
				default:
                    FLLog.Warning("Material", Name + ": Unknown property " + n.Name.ToLowerInvariant());
                    break;
			}

			return true;
		}

		public virtual void Initialize()
		{
			if (isBasic)
			{
				var bm = new BasicMaterial(type);
				_rmat = bm;
				//set up material
				bm.Dc = Dc;
				bm.OcEnabled = Oc.HasValue;
				if (Oc.HasValue)
					bm.Oc = Oc.Value;
				bm.Ec = Ec;
				bm.DtSampler = DtName;
				bm.DtFlags = (SamplerFlags)DtFlags;
				bm.EtSampler = EtName;
				bm.EtFlags = (SamplerFlags)EtFlags;
				bm.Library = textureLibrary;
				if (type.Contains("Ot"))
					bm.AlphaEnabled = true;
				if (type.Contains("Two"))
					bm.DoubleSided = true;
				if (type.Contains("Et"))
					bm.EtEnabled = true;
			}
			else
			{
				switch (type)
				{
					case "Nebula":
					case "NebulaTwo":
						var nb = new NebulaMaterial();
						if (type == "NebulaTwo") nb.DoubleSided = true;
						_rmat = nb;
						nb.DtSampler = DtName;
						nb.DtFlags = (SamplerFlags)DtFlags;
						nb.Library = textureLibrary;
						break;
					case "AtmosphereMaterial":
						var am = new AtmosphereMaterial();
						_rmat = am;
						am.Dc = Dc;
						am.Ac = Ac;
						am.Alpha = Alpha;
						am.Fade = Fade;
						am.Scale = Scale;
						am.DtSampler = DtName;
						am.DtFlags = (SamplerFlags)DtFlags;
						am.Library = textureLibrary;
						break;
					case "Masked2DetailMapMaterial":
						var m2 = new Masked2DetailMapMaterial();
						_rmat = m2;
						m2.Dc = Dc;
						m2.Ac = Ac;
						m2.TileRate0 = TileRate0;
						m2.TileRate1 = TileRate1;
						m2.FlipU = FlipU;
						m2.FlipV = FlipV;
						m2.DtSampler = DtName;
						m2.DtFlags = (SamplerFlags)DtFlags;
						m2.Dm0Sampler = dm0Name;
						m2.Dm0Flags = (SamplerFlags)Dm0Flags;
						m2.Dm1Sampler = Dm1Name;
						m2.Dm1Flags = (SamplerFlags)Dm1Flags;
						m2.Library = textureLibrary;
						break;
					case "IllumDetailMapMaterial":
						var ilm = new IllumDetailMapMaterial();
						_rmat = ilm;
						ilm.Dc = Dc;
						ilm.Ac = Ac;
						ilm.TileRate0 = TileRate0;
						ilm.TileRate1 = TileRate1;
						ilm.FlipU = FlipU;
						ilm.FlipV = FlipV;

						ilm.DtSampler = DtName;
						ilm.DtFlags = (SamplerFlags)DtFlags;

						ilm.Dm0Sampler = dm0Name;
						ilm.Dm0Flags = (SamplerFlags)Dm0Flags;
						ilm.Dm1Sampler = Dm1Name;
						ilm.Dm1Flags = (SamplerFlags)Dm1Flags;
						ilm.Library = textureLibrary;
						break;
					case "DetailMap2Dm1Msk2PassMaterial":
						var dm2p = new DetailMap2Dm1Msk2PassMaterial();
						_rmat = dm2p;
						dm2p.Dc = Dc;
						dm2p.Ac = Ac;
						dm2p.FlipU = FlipU;
						dm2p.FlipV = FlipV;
						dm2p.TileRate = TileRate;

						dm2p.DtSampler = DtName;
						dm2p.DtFlags = (SamplerFlags)DtFlags;

						dm2p.Dm1Sampler = Dm1Name;
						dm2p.Dm1Flags = (SamplerFlags)Dm1Flags;
						dm2p.Library = textureLibrary;
						break;
					case "NomadMaterialNoBendy":
					case "NomadMaterial":
						var nmd = new NomadMaterial();
						_rmat = nmd;
						nmd.Dc = Dc;
						nmd.BtSampler = btName;
						nmd.BtFlags = (SamplerFlags)BtFlags;
						nmd.DtSampler = DtName;
						nmd.DtFlags = (SamplerFlags)DtFlags;
						nmd.NtFlags = (SamplerFlags)NtFlags;
						nmd.NtSampler = NtName;
						nmd.Oc = Oc ?? 1f;
						nmd.Library = textureLibrary;
						break;
					case "DetailMapMaterial":
						var dm = new DetailMapMaterial();
						_rmat = dm;
						dm.Dc = Dc;
						dm.Ac = Ac;
						dm.FlipU = FlipU;
						dm.FlipV = FlipV;
						dm.TileRate = TileRate;;
						dm.DmSampler = DmName;
						dm.DmFlags = (SamplerFlags)DmFlags;
						dm.DtSampler = DtName;
						dm.DtFlags = (SamplerFlags)DtFlags;
						dm.Library = textureLibrary;
						break;
					case "NormalDebugMaterial":
						_rmat = new NormalDebugMaterial();
						break;
					default:
						throw new NotImplementedException();
				}
			}
		}

		public void Resized()
		{
			//if (effect != null)
			//effect.SetParameter ("Projection", camera.Projection);
		}

		Matrix4 ViewProjection = Matrix4.Identity;

		public void Update(ICamera camera)
		{
			ViewProjection = camera.ViewProjection;
            if (Render != null)
                Render.Camera = camera;
		}

		public override string ToString()
		{
			return string.Format("[{0}: {1}]", type, Name);
		}
	}
}

