// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public partial class UtfTab
    {
        void EditString(LUtfNode node)
        {
            ConfirmIf(node.Data.Length > 250, "Data is >250 bytes, string will be truncated. Continue?", () =>
            {
                var config = new NameInputConfig()
                    { AllowInitial = true, IsId = false, Title = "String Editor", ValueName = "String", AllowEmpty = true };
                popups.OpenPopup(new NameInputPopup(config, node.StringData,
                    newStr => node.StringData = newStr));
            });
        }

        void RenameNode(LUtfNode node)
        {
            var config = new NameInputConfig()
                { IsId = false, Title = "Rename Node", ValueName = "Name", AllowInitial = true };
            popups.OpenPopup(new NameInputPopup(config, node.Name, name =>
            {
                node.Name = name;
                node.ResolvedName = null;
            }));
        }

        void AddNode(LUtfNode addParent, LUtfNode addNode, int addOffset)
        {
            var config = new NameInputConfig() { IsId = false, Title = "Add Node", ValueName = "Name" };
            popups.OpenPopup(new NameInputPopup(config, "", name =>
            {
                var node = new LUtfNode() { Name = name, Parent = addParent ?? addNode };
                if (addParent != null)
                    addParent.Children.Insert(addParent.Children.IndexOf(addNode) + addOffset, node);
                else
                {
                    addNode.Data = null;
                    if (addNode.Children == null) addNode.Children = new List<LUtfNode>();
                    addNode.Children.Add(node);
                }
                selectedNode = node;
            }));
        }

        void ConfirmIf(bool condition, string text, Action action)
        {
            if (condition) Confirm(text, action);
            else action();
        }

        void Confirm(string text, Action action)
        {
            popups.MessageBox("Confirm?", text, false, MessageBoxButtons.YesNo,
                x =>
                {
                    if (x == MessageBoxResponse.Yes)
                    {
                        action();
                    }
                });
        }

        void ErrorPopup(string error)
        {
            popups.MessageBox("Error", error);
        }
    }
}
