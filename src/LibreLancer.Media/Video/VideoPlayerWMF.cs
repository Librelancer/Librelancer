// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LibreLancer.Graphics;

namespace LibreLancer.Media
{
    class VideoPlayerWMF : VideoPlayerInternal
    {
        MediaSession session;
        PresentationClock clock;
        Texture2D _texture;
        Topology topology;
        MFSamples videoSampler;
        MediaType mt;
        static Texture2D dot;
        bool _textureWrittenTo;
        public override void Dispose()
        {
            FLLog.Info("Video", "Closing Windows Media Foundation backend");
            if (session != null)
            {
                session.Stop();
                session.ClearTopologies();
                //Sample grabber thread works asynchronously (as task), so we need give him a time, to understand, that session is closed
                //minimal time to wait: 33 ms (1000 ms / 30 fps), but I decide to use a little more
                System.Threading.Thread.Sleep(100);
                session.Close();
                session.Dispose();
                session = null;
            }
            if (topology != null)
            {
                topology.Dispose();
                topology = null;
            }
            if (videoSampler != null)
            {
                videoSampler.Dispose();
                videoSampler = null;
            }
            if (clock != null)
            {
                clock.Dispose();
                clock = null;
            }
            if (_texture != null)
            {
                _texture.Dispose();
                _texture = null;
            }
            if (cb != null)
            {
                cb.Dispose();
                cb = null;
            }
        }

        public override void Draw()
        {
            if(Playing)
            {

                if (videoSampler.Changed)
                {
                    _texture.SetData(videoSampler.TextureData);
                    _textureWrittenTo = true;
                    videoSampler.Changed = false;
                }
            }
        }

        public override Texture2D GetTexture()
        {
            if (_texture != null && _textureWrittenTo) {
                return _texture;
            }
            return dot;
        }
        static bool _started = false;

        private RenderContext rcontext;
        public override bool Init(RenderContext rcontext)
        {
            this.rcontext = rcontext;
            FLLog.Info("Video", "Opening Windows Media Foundation backend");
            try
            {
               if(dot == null)
                {
                    dot = new Texture2D(rcontext, 1, 1);
                    dot.SetData(new uint[] { 0x000000FF });
                }
                if (!_started)
                {
                    MediaManager.Startup();
                    _started = true;
                }
                MediaFactory.CreateTopology(out topology);
                MediaFactory.CreateMediaSession(null, out session);
                return true;
            }
            catch (Exception ex)
            {
                FLLog.Info("Video", "Media Foundation: " + ex.Message);
                return false;
            }
        }
        MFCallback cb;

        delegate void SinkActivateMethod(MediaType iMFMediaTypeRef,
            SampleGrabberSinkCallback iMFSampleGrabberSinkCallbackRef, out Activate iActivateOut);
        private static SinkActivateMethod MFCreateSampleGrabberSinkActivate;
        static void GetMethods()
        {
            if (MFCreateSampleGrabberSinkActivate != null)
            {
                var mi = typeof(MediaFactory).GetMethod("MFCreateSampleGrabberSinkActivate",
                    BindingFlags.Static | BindingFlags.NonPublic);
                MFCreateSampleGrabberSinkActivate = (SinkActivateMethod)mi.CreateDelegate(typeof(SinkActivateMethod));
            }
        }
        public override void PlayFile(string filename)
        {
            //Load the file
            MediaSource mediaSource;
            {
                var resolver = new SourceResolver();
                ObjectType otype;
                var source = new ComObject(resolver.CreateObjectFromURL(filename, SourceResolverFlags.MediaSource, null, out otype));
                try
                {
                    // Sometimes throws HRESULT: [0x80004002], Module: [General], ApiCode: [E_NOINTERFACE/No such interface supported], Message: No such interface supported. Bug?
                    mediaSource = source.QueryInterface<MediaSource>();
                }
                catch (SharpDXException)
                {
                    mediaSource = null;
                    FLLog.Error("VideoPlayerWMF", "QueryInterface failed on Media Foundation");
                }
                resolver.Dispose();
                source.Dispose();
            }
            if (mediaSource is null)
            {
                return;
            }

            PresentationDescriptor presDesc;
            mediaSource.CreatePresentationDescriptor(out presDesc);

            for(int i = 0; i < presDesc.StreamDescriptorCount; i++)
            {
                SharpDX.Mathematics.Interop.RawBool selected;
                StreamDescriptor desc;
                presDesc.GetStreamDescriptorByIndex(i, out selected, out desc);
                if(selected)
                {
                    TopologyNode sourceNode;
                    MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out sourceNode);

                    sourceNode.Set(TopologyNodeAttributeKeys.Source, mediaSource);
                    sourceNode.Set(TopologyNodeAttributeKeys.PresentationDescriptor, presDesc);
                    sourceNode.Set(TopologyNodeAttributeKeys.StreamDescriptor, desc);

                    TopologyNode outputNode;
                    MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outputNode);

                    var majorType = desc.MediaTypeHandler.MajorType;
                    if (majorType == MediaTypeGuids.Video)
                    {
                        Activate activate;

                        videoSampler = new MFSamples();
                        //retrieve size of video
                        long sz = desc.MediaTypeHandler.CurrentMediaType.Get<long>(new Guid("{1652c33d-d6b2-4012-b834-72030849a37d}"));
                        int height = (int)(sz & uint.MaxValue), width = (int)(sz >> 32);
                        _texture = new Texture2D(rcontext, width, height, false, SurfaceFormat.Bgra8);
                        mt = new MediaType();

                        mt.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);

                        // Specify that we want the data to come in as RGB32.
                        mt.Set(MediaTypeAttributeKeys.Subtype, new Guid("00000016-0000-0010-8000-00AA00389B71"));
                        GetMethods();
                        MFCreateSampleGrabberSinkActivate(mt, videoSampler, out activate);
                        outputNode.Object = activate;
                    }

                    if (majorType == MediaTypeGuids.Audio)
                    {
                        Activate activate;
                        MediaFactory.CreateAudioRendererActivate(out activate);

                        outputNode.Object = activate;
                    }

                    topology.AddNode(sourceNode);
                    topology.AddNode(outputNode);
                    sourceNode.ConnectOutput(0, outputNode, 0);

                    sourceNode.Dispose();
                    outputNode.Dispose();
                }
                desc.Dispose();
            }

            presDesc.Dispose();
            mediaSource.Dispose();
            //Play the file
            cb = new MFCallback(this, session);
            session.BeginGetEvent(cb, null);
            session.SetTopology(SessionSetTopologyFlags.Immediate, topology);
            // Get the clock
            clock = session.Clock.QueryInterface<PresentationClock>();

            // Start playing.
            Playing = true;
        }
        class MFSamples : CallbackBase, SampleGrabberSinkCallback
        {
            internal byte[] TextureData { get; private set; }
            public bool Changed = false;
            public void OnProcessSample(Guid guidMajorMediaType, int dwSampleFlags, long llSampleTime, long llSampleDuration, IntPtr sampleBufferRef, int dwSampleSize)
            {
                if (TextureData == null || TextureData.Length != dwSampleSize)
                    TextureData = new byte[dwSampleSize];

                Marshal.Copy(sampleBufferRef, TextureData, 0, dwSampleSize);
                //SET ALPHA to 0xFF
                for (int i = 3; i < TextureData.Length; i += 4)
                    TextureData[i] = 0xFF;
                Changed = true;
            }

            public void OnSetPresentationClock(PresentationClock presentationClockRef)
            {
            }

            public void OnShutdown()
            {
            }

            public void OnClockPause(long systemTime)
            {
            }

            public void OnClockRestart(long systemTime)
            {
            }

            public void OnClockSetRate(long systemTime, float flRate)
            {
            }

            public void OnClockStart(long systemTime, long llClockStartOffset)
            {
            }

            public void OnClockStop(long hnsSystemTime)
            {
            }
        }
        class MFCallback : ComObject, IAsyncCallback
        {
            MediaSession _session;
            VideoPlayerWMF _player;
            public MFCallback(VideoPlayerWMF player, MediaSession _session)
            {
                this._session = _session;
                _player = player;
                Disposed += (sender, args) => disposed = true;
            }
            bool disposed = false;

            public IDisposable Shadow { get; set; }
            public void Invoke(AsyncResult asyncResultRef)
            {
                if (disposed)
                    return;
                try
                {
                    var ev = _session.EndGetEvent(asyncResultRef);
                    if (disposed)
                        return;
                    if (ev.TypeInfo == MediaEventTypes.SessionTopologySet)
                    {
                        _player.Begin();
                    }
                    if (ev.TypeInfo == MediaEventTypes.SessionEnded)
                        _player.Playing = false;
                    _session.BeginGetEvent(this, null);
                }
                catch (Exception)
                {
                }

            }
            public AsyncCallbackFlags Flags { get; private set; }
            public WorkQueueId WorkQueueId { get; private set; }
        }
        internal void Begin()
        {
            var varStart = new Variant();
            session.Start(null, varStart);
        }
    }
}
