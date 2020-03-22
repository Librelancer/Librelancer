// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.GameData
{
    public class ResolvedModel
    {
        public string[] LibraryFiles;
        public string ModelFile;

        public IDrawable LoadFile(ResourceManager res)
        {
            if (LibraryFiles != null)
            {
                foreach (var f in LibraryFiles)
                    res.LoadResourceFile(f);
            }
            return res.GetDrawable(ModelFile);
        }
    }
}