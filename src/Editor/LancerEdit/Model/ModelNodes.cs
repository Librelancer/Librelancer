// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;

namespace LancerEdit
{
    //Class for keeping hardpoints node references
    public class ModelNodes
    {
        public List<ModelHpNode> Nodes = new List<ModelHpNode>();
        public LUtfNode Cons;
    }
}
