// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.GameData
{
    public class ResolvedModel
    {
        public string[] LibraryFiles;
        public string ModelFile;
        public string SourcePath;

        public ModelResource LoadFile(ResourceManager res, MeshLoadMode loadMode = MeshLoadMode.GPU)
        {
            if (ModelFile == null) return null;
            if (LibraryFiles != null)
            {
                foreach (var f in LibraryFiles)
                    res.LoadResourceFile(f, loadMode);
            }
            return res.GetDrawable(ModelFile, loadMode);
        }

        public override string ToString() => $"{ModelFile} ({LibraryFiles?.Length ?? 0} resources)";
    }
}
