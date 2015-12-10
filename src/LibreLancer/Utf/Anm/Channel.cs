/* The contents of this file a
 * re subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;


namespace LibreLancer.Utf.Anm
{
    public class Channel
    {
        private ConstructCollection constructs;

        public string ParentName { get; private set; }
        public string ChildName { get; private set; }

        public int FrameCount { get; private set; }
        public float Interval { get; private set; }
        public int ChannelType { get; private set; }

        public Frame[] Frames { get; private set; }

        private AbstractConstruct construct;
        public AbstractConstruct Construct
        {
            get
            {
                if (construct == null) construct = constructs.Find(ChildName);
                return construct;
            }
        }

        private float globalTime = 0, frameTime = 0, timeDelta = 1;
        private int currentFrameIndex = 0;
        private Frame lastFrame, currentFrame;

        public Channel(IntermediateNode root, ConstructCollection constructs)
        {
            this.constructs = constructs;

            byte[] frameBytes = new byte[0];
            foreach (Node node in root)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "parent name":
                        if (ParentName == null) ParentName = (node as LeafNode).StringData;
                        else throw new Exception("Multiple parent name nodes in channel root");
                        break;
                    case "child name":
                        if (ChildName == null) ChildName = (node as LeafNode).StringData;
                        else throw new Exception("Multiple child name nodes in channel root");
                        break;
                    case "channel":
                        IntermediateNode channelNode = node as IntermediateNode;
                        foreach (LeafNode channelSubNode in channelNode)
                        {
                            switch (channelSubNode.Name.ToLowerInvariant())
                            {
                                case "header":
                                    using (BinaryReader reader = new BinaryReader(new MemoryStream(channelSubNode.ByteArrayData)))
                                    {
                                        FrameCount = reader.ReadInt32();
                                        Interval = reader.ReadSingle();
                                        ChannelType = reader.ReadInt32();
                                    }
                                    break;
                                case "frames":
                                    frameBytes = channelSubNode.ByteArrayData;
                                    break;
                                default: throw new Exception("Invalid node in " + channelNode.Name + ": " + channelSubNode.Name);
                            }
                        }
                        break;
                    default: throw new Exception("Invalid node in channel root: " + node.Name);
                }
            }

            Frames = new Frame[FrameCount];
            using (BinaryReader reader = new BinaryReader(new MemoryStream(frameBytes)))
            {
                for (int i = 0; i < FrameCount; i++)
                {
                    Frames[i] = new Frame(reader, Interval == -1, ChannelType);
                }
            }

            lastFrame = Frames[FrameCount - 1];
            currentFrame = Frames[0];
        }

        public void Update()
        {
            if (Interval == -1)
            {
                if (globalTime < currentFrame.Time.Value)
                {
                    globalTime += .1f;
                    frameTime += .1f;
                }
                else
                {
                    if (currentFrameIndex < FrameCount - 1)
                    {
                        currentFrameIndex++;
                        frameTime = 0;
                    }
                    else
                    {
                        currentFrameIndex = 0;
                        globalTime = 0;
                    }
                    lastFrame = currentFrame;
                    currentFrame = Frames[currentFrameIndex];
                    if (currentFrameIndex > 0) timeDelta = currentFrame.Time.Value - lastFrame.Time.Value;
                    else timeDelta = currentFrame.Time.Value;
                }

                if (Construct != null)
                {
                    float t = frameTime / timeDelta;
                    float dist = currentFrame.Distance * t + lastFrame.Distance * (1 - t);
                    
                    //System.IO.File.AppendAllLines("ani.txt", new string[] { currentFrameIndex + ": " + frameTime + " (" + t + ") :: " + lastFrame.Distance + " -> " + currentFrame.Distance + " (" + dist + ")" });
                    
                    Construct.Update(dist);
                }
            }
        }
    }
}
