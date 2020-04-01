using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        public static List<ModifiedMaterial> Setup(RigidModel model, ResourceManager res)
        {
            var mats = new List<ModifiedMaterial>();
            foreach (var p in model.AllParts)
            {
                if (p.Mesh == null) continue;
                foreach (var l in p.Mesh.Levels)
                {
                    if (l == null) continue;
                    foreach (var dc in l)
                    {
                        var mat = dc.GetMaterial(res)?.Render;
                        if (mat is BasicMaterial bm)
                        {
                            if (mats.Any(x => x.Mat == bm)) continue;
                            mats.Add(new ModifiedMaterial() {Mat = bm, Dc = bm.Dc, Dt = bm.DtSampler});
                        }
                    }
                }
            }
            return mats;
        }
    }
    
    [UiLoadable]
    public class DisplayModel : DisplayElement
    {
        public InterfaceModel Model { get; set; }
        public InterfaceColor Tint { get; set; }
        private RigidModel model;
        private bool loadable = true;
        private List<ModifiedMaterial> mats;
        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if (Model == null) return;
            if (!CanRender(context)) return;
            context.Mode3D();
            var rect = context.PointsToPixels(clientRectangle);
            var transform = Matrix4x4.CreateScale(Model.XScale, Model.YScale, 1) *
                            Matrix4x4.CreateTranslation(Model.X, Model.Y, 0);
            context.MatrixCam.CreateTransform((int)context.ViewportWidth, (int)context.ViewportHeight, rect);
            context.RenderState.Cull = false;
            model.UpdateTransform();
            model.Update(context.MatrixCam, context.GlobalTime, context.ResourceManager);
            if (Tint != null)
            {
                var color = Tint.GetColor(context.GlobalTime);
                for (int i = 0; i < mats.Count; i++)
                    mats[i].Mat.Dc = color;
            }
            model.DrawImmediate(context.RenderState, context.ResourceManager, transform, ref Lighting.Empty);
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
            if (model == null)
            {
                model = context.GetModel(Model.Path);
                if (model == null)
                {
                    loadable = false;
                    return false;
                }
                if (Tint != null)
                    mats = MaterialModification.Setup(model, context.ResourceManager);
            }
            return true;
        }
        
        
       
    }
}