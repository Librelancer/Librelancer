// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class Scene : Container
    {
        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            CheckStyle(context);
            ClientRectangle = layout.Fill();
            var self = new Layout(ClientRectangle);
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].OnLayout(context, self, delta);
            }
        }
    }
}
