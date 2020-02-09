using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Interface
{
    public class ModifiedMaterial
    {
        public BasicMaterial Mat;
        public Color4 Dc;
        public string Dt;
    }
    public class MaterialModification
    {
        public static List<ModifiedMaterial> Setup(IDrawable drawable)
        {
            var mats = new List<ModifiedMaterial>();
            if (drawable is ModelFile model)
            {
                ModifyMaterialsFor3db(model, mats);
            }
            else if (drawable is CmpFile cmp)
            {
                foreach (var child in cmp.Models.Values)
                    ModifyMaterialsFor3db(child, mats);
            }

            return mats;
        }
        static void ModifyMaterialsFor3db(ModelFile mfile, List<ModifiedMaterial> mats)
        {
            var l0 = mfile.Levels[0];
            var vms = l0.Mesh;
            //Save Mesh material state
            for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
            {
                var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
                if (mat == null) continue;
                if (mats.Any(x => x.Mat == mat)) continue;
                mats.Add(new ModifiedMaterial() {Mat = mat, Dc = mat.Dc, Dt = mat.DtSampler});
            }
        }
    }
    
    [UiLoadable]
    public class DisplayModel : DisplayElement
    {
        public InterfaceModel Model { get; set; }
        public InterfaceColor Tint { get; set; }
        private IDrawable drawable;
        private bool loadable = true;
        private List<ModifiedMaterial> mats;
        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if (Model == null) return;
            if (!CanRender(context)) return;
            context.Mode3D();
            var rect = context.PointsToPixels(clientRectangle);
            var transform = Matrix4.CreateScale(Model.XScale, Model.YScale, 1) *
                            Matrix4.CreateTranslation(Model.X, Model.Y, 0);
            context.MatrixCam.CreateTransform((int)context.ViewportWidth, (int)context.ViewportHeight, rect);
            context.RenderState.Cull = false;
            drawable.Update(context.MatrixCam, TimeSpan.Zero, context.GlobalTime);
            if (Tint != null)
            {
                var color = Tint.GetColor(context.GlobalTime);
                for (int i = 0; i < mats.Count; i++)
                    mats[i].Mat.Dc = color;
            }
            drawable.Draw(context.RenderState, transform, Lighting.Empty);
            if (Tint != null)
            {
                for (int i = 0; i < mats.Count; i++)
                {
                    mats[i].Mat.Dc = mats[i].Dc;
                }
            }           
            context.RenderState.Cull = true;
        }
        
        bool CanRender(UiContext context)
        {
            if (!loadable) return false;
            if (drawable == null)
            {
                drawable = context.GetDrawable(Model.Path);
                if (drawable == null)
                {
                    loadable = false;
                    return false;
                }
                if (Tint != null)
                    mats = MaterialModification.Setup(drawable);
            }
            return true;
        }
        
        
       
    }
}