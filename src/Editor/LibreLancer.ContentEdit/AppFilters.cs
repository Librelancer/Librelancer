using LibreLancer.Dialogs;

namespace LibreLancer.ContentEdit;

public class AppFilters
{
        public static readonly FileDialogFilters UtfFilters = new FileDialogFilters(
            new FileFilter("All Utf Files","utf","cmp","3db","dfm","vms","sph","mat","txm","ale","anm"),
            new FileFilter("Utf Files","utf"),
            new FileFilter("Anm Files","anm"),
            new FileFilter("Cmp Files","cmp"),
            new FileFilter("3db Files","3db"),
            new FileFilter("Dfm Files","dfm"),
            new FileFilter("Vms Files","vms"),
            new FileFilter("Sph Files","sph"),
            new FileFilter("Mat Files","mat"),
            new FileFilter("Txm Files","txm"),
            new FileFilter("Ale Files","ale")
        );

        public static readonly FileDialogFilters IniFilters = new FileDialogFilters(
            new FileFilter("Ini Files", "ini")
        );

        public static readonly  FileDialogFilters ImportModelFiltersNoBlender = new FileDialogFilters(
            new FileFilter("Model Files","dae","gltf","glb","obj"),
            new FileFilter("Collada Files", "dae"),
            new FileFilter("glTF 2.0 Files", "gltf"),
            new FileFilter("glTF 2.0 Binary Files", "glb"),
            new FileFilter("Wavefront Obj Files", "obj")
        );

        public static readonly  FileDialogFilters ImportModelFilters = new FileDialogFilters(
            new FileFilter("Model Files","dae","gltf","glb","obj", "blend"),
            new FileFilter("Collada Files", "dae"),
            new FileFilter("glTF 2.0 Files", "gltf"),
            new FileFilter("glTF 2.0 Binary Files", "glb"),
            new FileFilter("Wavefront Obj Files", "obj"),
            new FileFilter("Blender Files", "blend")
        );

        public static readonly FileDialogFilters BlenderFilter = new FileDialogFilters(
            new FileFilter("Blender Files", "blend")
        );

        public static readonly FileDialogFilters GlbFilter = new FileDialogFilters(
            new FileFilter("glTF 2.0 Binary Files", "glb")
        );

        public static readonly FileDialogFilters ColladaFilter = new FileDialogFilters(
            new FileFilter("Collada Files", "dae")
        );

        public static readonly FileDialogFilters FreelancerIniFilter = new FileDialogFilters(
            new FileFilter("Freelancer.ini","freelancer.ini")
        );

        public static readonly FileDialogFilters StateGraphFilter = new FileDialogFilters(
            new FileFilter("State Graph Db", "db")
        );

        public static readonly FileDialogFilters ImageFilter = new FileDialogFilters(
            new FileFilter("Images", "bmp", "png", "tga", "dds", "jpg", "jpeg")
        );

        public static readonly FileDialogFilters SurFilters = new FileDialogFilters(
            new FileFilter("Sur Files", "sur")
        );

        public static readonly FileDialogFilters ThnFilters = new FileDialogFilters(
            new FileFilter("Thorn Files", "thn", "lua"),
            new FileFilter("Thn Files", "thn"),
            new FileFilter("Lua Files", "lua")
        );
}
