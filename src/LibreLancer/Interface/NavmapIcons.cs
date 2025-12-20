// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Schema.Interface;

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

        public IEnumerable<string> Libraries() => ini.LibraryFiles.SelectMany(x => x.Files);

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
                renderable = new UiRenderable();
                renderable.AddElement(new DisplayModel() {
                    Model = new InterfaceModel() {
                        Name = name, Path = $"{DIR}{name}.3db", XScale = 50, YScale = 50
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
