// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Data.Interface;

namespace LibreLancer.Interface
{
    public interface INavmapIcons
    {
        UiRenderable GetSystemObject(string name);
        UiRenderable GetBackground();
        IEnumerable<string> Libraries();
    }

    public class IniNavmapIcons : INavmapIcons
    {
        private NavmapIni ini;
        Dictionary<string, UiRenderable> renderables = new Dictionary<string, UiRenderable>();
        public IniNavmapIcons(NavmapIni ini)
        {
            this.ini = ini;
            if (ini.Icons == null)
                throw new Exception("Navmap Ini must have [Icons] section with nav_depot entry");
            if (!ini.Icons.Map.TryGetValue("nav_depot", out var _))
                throw new Exception("Navmap Ini must have nav_depot in [Icons]");
        }

        public IEnumerable<string> Libraries() => ini.LibraryFiles ?? (IEnumerable<string>) Array.Empty<string>();
        
        public UiRenderable GetSystemObject(string name)
        {
            var type = ini.Type?.Type ?? NavIconType.Model;
            if (string.IsNullOrEmpty(name)) return GetSystemObject("nav_depot");
            if (!renderables.TryGetValue(name, out var renderable))
            {
                if (!ini.Icons.Map.TryGetValue(name, out var model))
                {
                    return GetSystemObject("nav_depot");
                }
                renderable = new UiRenderable();
                if (type == NavIconType.Model)
                {
                    renderable.AddElement(new DisplayModel()
                    {
                        Model = new InterfaceModel()
                        {
                            Name = name, Path = model, XScale = 50, YScale = 50
                        }
                    });
                } 
                else if (type == NavIconType.Texture)
                {
                    renderable.AddElement(new DisplayImage()
                    {
                        Image = new InterfaceImage()
                        {
                            Name = model, TexName = model
                        }
                    });
                }
                

                renderables.Add(name, renderable);
            }
            return renderable;
        }
        
        private UiRenderable background;
        public UiRenderable GetBackground()
        {
            if (background == null) {
                background = new UiRenderable();
                background.AddElement(new DisplayImage()
                {
                    Image = new InterfaceImage()
                    {
                        TexName = ini.Background?.Texture ?? "NAV_zoomedliberty.tga"
                    }
                });
            }
            return background;
        }
    }
    public class NavmapIcons : INavmapIcons
    {
        //TODO: Turn this into directory lookup + .3db like vanilla
        private const string DIR = "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/SPACEOBJECTS/";
        static Dictionary<string,string> models = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "nav_depot", $"{DIR}nav_depot.3db" },
            { "nav_dockingrings", $"{DIR}nav_dockingrings.3db" },
            { "nav_jumpgate", $"{DIR}nav_jumpgate.3db" },
            { "nav_jumphole", $"{DIR}nav_jumphole.3db" },
            { "nav_largestation", $"{DIR}nav_largestation.3db" },
            { "nav_lootabledepot", $"{DIR}nav_lootabledepot.3db" },
            { "nav_offscreenarrow", $"{DIR}nav_offscreenarrow.3db" },
            { "nav_outpost", $"{DIR}nav_outpost.3db" },
            { "nav_playership", $"{DIR}nav_playership.3db" },
            { "nav_smallstation", $"{DIR}nav_smallstation.3db" },
            { "nav_star", $"{DIR}nav_star.3db" },
            { "nav_surprisex", $"{DIR}nav_surprisex.3db" },
            { "nav_tradelanering", $"{DIR}nav_tradelanering.3db" },
            { "nav_waypointcircle", $"{DIR}nav_waypointcircle.3db" },
            { "nav_waypointdiamond", $"{DIR}nav_waypointdiamond.3db" },
            { "nav_weaponplatform", $"{DIR}nav_weaponplatform.3db" },
            { "nnm_lg_depot", $"{DIR}nnm_lg_depot.3db" },
            { "nnm_sm_depot", $"{DIR}nnm_sm_depot.3db" },
            { "nnm_sm_info_position", $"{DIR}nnm_sm_info_position.3db" },
            { "nnm_sm_medium_forest_moon", $"{DIR}nnm_sm_medium_forest_moon.3db" },
            { "nnm_sm_medium_rocky_moon", $"{DIR}nnm_sm_medium_rocky_moon.3db" },
            { "nnm_sm_mining", $"{DIR}nnm_sm_mining.3db" },
            { "nnm_sm_small_desert_moon", $"{DIR}nnm_sm_small_desert_moon.3db" },
            { "nnm_sm_small_ice_moon", $"{DIR}nnm_sm_small_ice_moon.3db" },
            { "nnm_sm_sun", $"{DIR}nnm_sm_sun.3db" }
        };

        Dictionary<string, UiRenderable> renderables = new Dictionary<string, UiRenderable>();

        public IEnumerable<string> Libraries()
        {
            yield return $"{DIR}spaceobjects.mat";
            yield return "INTERFACE/interface.generic.vms";
            yield return "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/navmaptextures.txm";
            yield return "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/zoomedmap_liberty.3db";
        }
        public UiRenderable GetSystemObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return GetSystemObject("nav_depot");
            if (!renderables.TryGetValue(name, out var renderable))
            {
                if (!models.TryGetValue(name, out var model))
                {
                    return GetSystemObject("nav_depot");
                }
                renderable = new UiRenderable();
                renderable.AddElement(new DisplayModel() {
                    Model = new InterfaceModel() {
                        Name = name, Path = model, XScale = 50, YScale = 50
                    }
                });
                renderables.Add(name, renderable);
            }
            return renderable;
        }

        private UiRenderable background;
        public UiRenderable GetBackground()
        {
            if (background == null) {
                background = new UiRenderable();
                background.AddElement(new DisplayImage()
                {
                    Image = new InterfaceImage()
                    {
                        TexName = "NAV_zoomedliberty.tga"
                    }
                });
            }
            return background;
        }
    }
}
