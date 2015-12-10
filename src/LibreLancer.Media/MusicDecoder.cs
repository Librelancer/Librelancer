using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.IO;
using FFmpeg.AutoGen;
using OpenTK.Audio.OpenAL;
using System.Threading;
namespace LibreLancer.Media
{
	unsafe class MusicDecoder : IDisposable
	{
		static bool inited = false;
		static void Init()
		{
			if (!inited)
				ffmpeg.av_register_all ();
			inited = true;
		}
		public int Frequency {
			get {
				return codec_context->sample_rate;
			}
		}
		public ALFormat Format {
			get {
				return codec_context->channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
			}
		}
		//Things
		SwrContext* swr_ctx;
		AVFormatContext *fctx;
		AVCodecContext *codec_context;
		AVCodec *codec;
		AVFrame *decodedFrame;
		int audioStream = -1;
		ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
		bool finished = false;
		const int MAX_BUFFERS = 5;
		Thread decoderThread;
		bool running = true;
		public MusicDecoder (string filename)
		{
			Init ();
			fctx = OpenInput (filename);
			if (ffmpeg.avformat_find_stream_info (fctx, (AVDictionary**)0) < 0)
				throw new Exception ("Couldn't find streams");
			audioStream = -1;
			for (int i = 0; i < fctx->nb_streams; i++) {
				if (fctx->streams [i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO) {
					audioStream = i;
					break;
				}
			}
			if (audioStream == -1)
				throw new Exception ("No Audio Stream");
			codec_context = fctx->streams [audioStream]->codec;
			codec = ffmpeg.avcodec_find_decoder (codec_context->codec_id);
			if (ffmpeg.avcodec_open2 (codec_context, codec, (AVDictionary**)0) < 0)
				throw new Exception ("Couldn't open decoder");
			swr_ctx = ffmpeg.swr_alloc ();
			if (swr_ctx == (SwrContext*)0)
				throw new Exception ("Failed to alloc resample context");
			var chlayout = codec_context->channels == 2 ? ffmpeg.AV_CH_LAYOUT_STEREO : ffmpeg.AV_CH_LAYOUT_MONO;
			ffmpeg.av_opt_set_int ((void*)swr_ctx, "in_channel_layout", chlayout, 0);
			ffmpeg.av_opt_set_int ((void*)swr_ctx, "in_sample_rate", codec_context->sample_rate, 0);
			ffmpeg.av_opt_set_sample_fmt ((void*)swr_ctx, "in_sample_fmt", codec_context->sample_fmt, 0);

			ffmpeg.av_opt_set_int ((void*)swr_ctx, "out_channel_layout", chlayout, 0);
			ffmpeg.av_opt_set_int ((void*)swr_ctx, "out_sample_rate", codec_context->sample_rate, 0);
			ffmpeg.av_opt_set_sample_fmt ((void*)swr_ctx, "out_sample_fmt", AVSampleFormat.AV_SAMPLE_FMT_S16, 0);
			if (ffmpeg.swr_init (swr_ctx) < 0)
				throw new Exception ("Could not init resample context");

			decodedFrame = ffmpeg.avcodec_alloc_frame ();
			decoderThread = new Thread (DecodeThread);
			decoderThread.Start ();
		}
		public bool GetBuffer(ref byte[] output)
		{
			if (finished && buffers.Count == 0)
				return false;
			else {
				while (buffers.Count < 1)
					Thread.Sleep (1);
				buffers.TryDequeue (out output);
				return true;
			}
		}
		void DecodeThread()
		{
			AVPacket packet;
			sbyte **destBuffer;
			int destBufferLineSize;
			ffmpeg.av_samples_alloc_array_and_samples (
				&destBuffer,
				&destBufferLineSize,
				codec_context->channels,
				4096,
				AVSampleFormat.AV_SAMPLE_FMT_S16,
				0);
			while (running) {
				if (ffmpeg.av_read_frame (fctx, &packet) < 0) {
					break;
				}
				while (buffers.Count >= MAX_BUFFERS) {
					Thread.Sleep (10);
					if (!running)
						break;
				}
				if (packet.stream_index != audioStream)
					continue;
				int len;
				while (packet.size > 0) {
					int finishedFrame = 0;
					len = ffmpeg.avcodec_decode_audio4 (codec_context, decodedFrame, &finishedFrame, &packet);
					if (len < 0) {
						Console.WriteLine ("[ffmpeg] decoding error");
						break;
					}
					if (finishedFrame != 0) {
						int outputSamples = ffmpeg.swr_convert (swr_ctx, destBuffer, destBufferLineSize,
							(sbyte**)decodedFrame->extended_data, decodedFrame->nb_samples);
						int buffersize = ffmpeg.av_get_bytes_per_sample (AVSampleFormat.AV_SAMPLE_FMT_S16)
							* codec_context->channels * outputSamples;
						byte[] output = new byte[buffersize];
						Marshal.Copy ((IntPtr)destBuffer [0], output, 0, buffersize);
						buffers.Enqueue (output);
					}
					packet.size -= len;
					packet.data += len;
				}
			}
			ffmpeg.av_free ((void*)destBuffer[0]);
			ffmpeg.av_free ((void*)destBuffer);
			finished = true;
		}

		public void Dispose()
		{
			running = false;
			decoderThread.Join (); //wait for decoder exit
			fixed(SwrContext** swc = &swr_ctx)
			fixed(AVFrame** decf = &decodedFrame)
			fixed(AVFormatContext** fctxp = &fctx) {
				ffmpeg.swr_free (swc);
				ffmpeg.avcodec_close (codec_context);
				ffmpeg.avcodec_free_frame (decf);
				ffmpeg.avformat_close_input (fctxp);
				ffmpeg.avformat_free_context (fctx);
			}
		}

		static AVFormatContext* OpenInput(string filename)
		{
			if (!File.Exists (filename))
				throw new FileNotFoundException ();
			AVFormatContext* fctx = ffmpeg.avformat_alloc_context ();
			if (ffmpeg.avformat_open_input (&fctx, filename, (AVInputFormat*)0, (AVDictionary**)0) != 0)
				throw new Exception ("avformat_open_input failed");
			return fctx;
		}
	}
}

