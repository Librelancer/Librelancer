// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Schema;
using LibreLancer.Render;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;


namespace LibreLancer.Utf.Mat
{
	public class Material
	{

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
		public Color4? Ec { get; set; }

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

		public string Dm0Name;


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

        public int MtFlags;
        public string MtName;

        public int RtFlags;
        public string RtName;

        public int NmFlags;
        public string NmName;

        public float? MFactor;
        public float? RFactor;

		static List<string> basicMaterials = new List<string> {
			"Dc", //DcDt buggy
			"DcDt", "DcDtTwo", "DcDtEc", "DcDtEt", "DcDtEcEt", "DcDtBtEc", "DcDtBtEcEt",
			"DcDtOcOt", "DcDtBtOcOt", "DcDtBtOcOtTwo", "DcDtEcOcOt",
			"DcDtOcOtTwo", "DcDtBt", "DcDtBtTwo", "BtDetailMapMaterial",
			"DcDtEcOcOtTwo", "DcDtEtTwo", "DcDtEcTwo"
		};
		RenderMaterial _rmat;
		public RenderMaterial Render
		{
			get
            {
                if (_rmat == null) throw new InvalidOperationException("Material not initialized");
				return _rmat;
			}
		}
		bool isBasic = false;

		protected Material(IntermediateNode node, string type)
		{
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
			type = "DcDt";

			Name = "NullMaterial";
			DtFlags = 0;
			Dc = Color4.Magenta;
			DtName = null;
			isBasic = true;
		}

        public Material(RenderMaterial render)
        {
            type = "CUSTOM";
            Name = "CUSTOM";
            _rmat = render;
        }

		public static Material FromNode(IntermediateNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			LeafNode typeNode = node["Type"] as LeafNode;
			if (typeNode == null)
				throw new Exception("Invalid or missing type node in " + node.Name);


			string type = typeNode.StringData;
			type = MaterialMap.Instance.Get(type) ?? type;
			type = MaterialMap.Instance.Get(node.Name.ToLowerInvariant()) ?? type;

			if (type == "HighGlassMaterial" ||
                type == "GlassMaterial" ||
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
			var mat = new Material(node, type);
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
					Dm0Name = n.StringData;
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
                case "mt_name":
                    MtName = n.StringData;
                    break;
                case "mt_flags":
                    MtFlags = n.Int32Data.Value;
                    break;
                case "rt_name":
                    RtName = n.StringData;
                    break;
                case "rt_flags":
                    RtFlags = n.Int32Data.Value;
                    break;
                case "nm_name":
                    NmName = n.StringData;
                    break;
                case "nm_flags":
                    NmFlags = n.Int32Data.Value;
                    break;
                case "m_factor":
                    MFactor = n.SingleData.Value;
                    break;
                case "r_factor":
                    RFactor = n.SingleData.Value;
                    break;
				default:
                    FLLog.Warning("Material", Name + ": Unknown property " + n.Name.ToLowerInvariant());
                    break;
			}

			return true;
		}

		public void Initialize(ResourceManager res)
		{
			if (isBasic)
			{
				var bm = new BasicMaterial(type, res);
				_rmat = bm;
				//set up material
				bm.Dc = Dc;
				bm.OcEnabled = Oc.HasValue;
				if (Oc.HasValue)
					bm.Oc = Oc.Value;
				bm.Ec = Ec ?? Color4.Black;
				bm.DtSampler = DtName;
				bm.DtFlags = (SamplerFlags)DtFlags;
				bm.EtSampler = EtName;
				bm.EtFlags = (SamplerFlags)EtFlags;
                bm.NmSampler = NmName;
                bm.NmFlags = (SamplerFlags)NtFlags;
                bm.MtSampler = MtName;
                bm.MtFlags = (SamplerFlags)MtFlags;
                bm.RtSampler = RtName;
                bm.RtFlags = (SamplerFlags)RtFlags;
                bm.Roughness = RFactor;
                bm.Metallic = MFactor;
				bm.Library = res;
				if (type.Contains("Ot"))
					bm.AlphaEnabled = true;
				if (type.Contains("Two"))
					bm.DoubleSided = true;
				if (type.Contains("Et"))
					bm.EtEnabled = true;
                if(Name.StartsWith("alpha_mask", StringComparison.OrdinalIgnoreCase) ||
                   type.Equals("BtDetailMapMaterial", StringComparison.OrdinalIgnoreCase))
                    bm.AlphaTest = true;
			}
			else
			{
				switch (type)
				{
					case "Nebula":
					case "NebulaTwo":
						var nb = new NebulaMaterial(res);
						if (type == "NebulaTwo") nb.DoubleSided = true;
						_rmat = nb;
						nb.DtSampler = DtName;
						nb.DtFlags = (SamplerFlags)DtFlags;
						nb.Library = res;
						break;
					case "AtmosphereMaterial":
						var am = new AtmosphereMaterial(res);
						_rmat = am;
						am.Dc = Dc;
						am.Ac = Ac;
						am.Alpha = Alpha;
						am.Fade = Fade;
						am.Scale = Scale;
						am.DtSampler = DtName;
						am.DtFlags = (SamplerFlags)DtFlags;
						am.Library = res;
						break;
					case "Masked2DetailMapMaterial":
						var m2 = new Masked2DetailMapMaterial(res);
						_rmat = m2;
						m2.Dc = Dc;
						m2.Ac = Ac;
						m2.TileRate0 = TileRate0;
						m2.TileRate1 = TileRate1;
						m2.FlipU = FlipU;
						m2.FlipV = FlipV;
						m2.DtSampler = DtName;
						m2.DtFlags = (SamplerFlags)DtFlags;
						m2.Dm0Sampler = Dm0Name;
						m2.Dm0Flags = (SamplerFlags)Dm0Flags;
						m2.Dm1Sampler = Dm1Name;
						m2.Dm1Flags = (SamplerFlags)Dm1Flags;
						m2.Library = res;
						break;
					case "IllumDetailMapMaterial":
						var ilm = new IllumDetailMapMaterial(res);
						_rmat = ilm;
						ilm.Dc = Dc;
						ilm.Ac = Ac;
						ilm.TileRate0 = TileRate0;
						ilm.TileRate1 = TileRate1;
						ilm.FlipU = FlipU;
						ilm.FlipV = FlipV;

						ilm.DtSampler = DtName;
						ilm.DtFlags = (SamplerFlags)DtFlags;

						ilm.Dm0Sampler = Dm0Name;
						ilm.Dm0Flags = (SamplerFlags)Dm0Flags;
						ilm.Dm1Sampler = Dm1Name;
						ilm.Dm1Flags = (SamplerFlags)Dm1Flags;
						ilm.Library = res;
						break;
					case "DetailMap2Dm1Msk2PassMaterial":
						var dm2p = new DetailMap2Dm1Msk2PassMaterial(res);
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
						dm2p.Library = res;
						break;
					case "NomadMaterialNoBendy":
					case "NomadMaterial":
						var nmd = new NomadMaterial(res);
						_rmat = nmd;
						nmd.Dc = Dc;
						nmd.BtSampler = btName;
						nmd.BtFlags = (SamplerFlags)BtFlags;
						nmd.DtSampler = DtName;
						nmd.DtFlags = (SamplerFlags)DtFlags;
						nmd.NtFlags = (SamplerFlags)NtFlags;
						nmd.NtSampler = NtName;
						nmd.Oc = Oc ?? 1f;
						nmd.Library = res;
						break;
					case "DetailMapMaterial":
						var dm = new DetailMapMaterial(res);
						_rmat = dm;
						dm.Dc = Dc;
						dm.Ac = Ac;
						dm.FlipU = FlipU;
						dm.FlipV = FlipV;
						dm.TileRate = TileRate;
						dm.DmSampler = DmName;
						dm.DmFlags = (SamplerFlags)DmFlags;
						dm.DtSampler = DtName;
						dm.DtFlags = (SamplerFlags)DtFlags;
						dm.Library = res;
						break;
                    default:
						throw new NotImplementedException();
				}
			}
		}

        public override string ToString()
		{
			return string.Format("[{0}: {1}]", type, Name);
		}
	}
}

