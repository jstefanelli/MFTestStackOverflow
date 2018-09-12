using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaFoundation;
using static MediaFoundation.Misc.COMBase;
using static MediaFoundation.HResult;
using static MediaFoundation.MFAttributesClsid;
using MFTestLib;
using MediaFoundation.Misc;
using MediaFoundation.Alt;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MFTest
{
	class Program
	{
		private const string VideoFile = "test.mp4";

		[STAThread]
		static void Main(string[] args)
		{
			if (MF.Startup(MFStartup.Lite).Succeeded())
			{
				HResult hr = ProcessMediaSession(VideoFile);
			}
		}

		private static HResult ProcessMediaSession(string VideoFile)
		{
			HResult hr = S_OK;

			IMFMediaSource source = null;
			IMFTopology topology = null;
			IMFMediaSinkAlt mediaSink = null;
			IMFMediaSession mediaSession = null;

			hr = CreateMediaSource(VideoFile, out source);

			if (Failed(hr))
				return hr;

			hr = CreateTopology(out topology, out mediaSink, source);
			if (Failed(hr))
			{
				topology = null;
				mediaSink = null;
				source = null;
				return hr;
			}

			hr = MF.CreateMediaSession(null, out mediaSession);
			if (Failed(hr))
			{
				topology = null;
				mediaSink = null;
				source = null;
				mediaSession = null;
				return hr;
			}

			hr = mediaSession.SetTopology(MFSessionSetTopologyFlags.None, topology);
			if (Failed(hr))
			{
				topology = null;
				mediaSink = null;
				source = null;
				mediaSession = null;
				return hr;
			}

			hr = RunMediaSession(mediaSession);

			return hr;
		}

		private static HResult CreateMediaSource(string videoFile, out IMFMediaSource source)
		{
			IMFSourceResolver resolver = null;
			source = null;

			HResult hr = MF.CreateSourceResolver(out resolver);

			if (Failed(hr))
				return hr;

			hr = resolver.CreateObjectFromURL(videoFile, MFResolution.MediaSource, null, out source);

			if (Failed(hr))
				SafeRelease(source);

			SafeRelease(resolver);

			return hr;
		}

		private static HResult CreateTopology(out IMFTopology topology, out IMFMediaSinkAlt mediaSink, IMFMediaSource source)
		{
			HResult hr = S_OK;

			topology = null;
			mediaSink = null;
			IMFPresentationDescriptor presentationDescriptor = null;

			hr = MF.CreateTopology(out topology);
			if (Failed(hr))
				return hr;

			hr = source.CreatePresentationDescriptor(out presentationDescriptor);
			if (Failed(hr))
			{
				topology = null;
				return hr;
			}

			hr = BuildTopology(out mediaSink, topology, presentationDescriptor, source);
			if (Failed(hr))
			{
				mediaSink = null;
				topology = null;
				return hr;
			}

			return hr;
		}

		private static HResult BuildTopology(out IMFMediaSinkAlt mediaSink, IMFTopology topology, IMFPresentationDescriptor presentationDescriptor, IMFMediaSource source)
		{
			HResult hr = S_OK;

			mediaSink = null;
			IMFMediaSinkAlt tempMediaSink = null;
			IMFStreamDescriptor streamDescriptor = null;
			IMFTopologyNode sourceNode = null;
			IMFTopologyNode outputNode = null;
			bool selected = false;
			int streamCount = 0;

			hr = presentationDescriptor.GetStreamDescriptorCount(out streamCount);
			if (Failed(hr))
				return hr;

			for(int i = 0; i < streamCount; i++)
			{
				hr = presentationDescriptor.GetStreamDescriptorByIndex(i, out selected, out streamDescriptor);
				if (Failed(hr))
					return hr;

				if (selected)
				{
					hr = CreateSourceStreamNode(source, streamDescriptor, presentationDescriptor, out sourceNode);
					if (Failed(hr))
					{
						return hr;
					}

					hr = CreateOutputNode(streamDescriptor, out tempMediaSink, out outputNode);
					if (Failed(hr))
					{
						mediaSink = null;
						return hr;
					}

					if (tempMediaSink != null)
						mediaSink = tempMediaSink;

					hr = topology.AddNode(sourceNode);
					if (Failed(hr))
					{
						mediaSink = null;
						return hr;
					}

					hr = topology.AddNode(outputNode);
					if (Failed(hr))
					{
						mediaSink = null;
						return hr;
					}

					hr = sourceNode.ConnectOutput(0, outputNode, 0);
					if (Failed(hr))
					{
						mediaSink = null;
						return hr;
					}
				}
			}

			return hr;
		}

		private static HResult CreateSourceStreamNode(IMFMediaSource source, IMFStreamDescriptor streamDescriptor, IMFPresentationDescriptor presentationDescriptor, out IMFTopologyNode node)
		{
			HResult hr = S_OK;

			node = null;

			hr = MF.CreateTopologyNode(MFTopologyType.SourcestreamNode, out node);
			if (Failed(hr))
				return hr;

			hr = node.SetUnknown(MF_TOPONODE_SOURCE, source);
			if (Failed(hr))
			{
				SafeRelease(node);
				return hr;
			}

			hr = node.SetUnknown(MF_TOPONODE_PRESENTATION_DESCRIPTOR, presentationDescriptor);
			if (Failed(hr))
			{
				SafeRelease(node);
				return hr;
			}

			hr = node.SetUnknown(MF_TOPONODE_STREAM_DESCRIPTOR, streamDescriptor);
			if (Failed(hr))
			{
				SafeRelease(node);
				return hr;
			}

			return hr;
		}

		private static HResult CreateOutputNode(IMFStreamDescriptor streamDescriptor, out IMFMediaSinkAlt mediaSink, out IMFTopologyNode node)
		{
			HResult hr = S_OK;

			mediaSink = null;
			node = null;
			IMFMediaTypeHandler mediaTypeHandler = null;
			IMFActivate activate = null;
			IMFStreamSinkAlt streamSink = null;
			Guid majorType = Guid.Empty;
			int streamSinkCount = 0;

			hr = streamDescriptor.GetMediaTypeHandler(out mediaTypeHandler);
			if (Failed(hr))
				return hr;

			hr = mediaTypeHandler.GetMajorType(out majorType);
			if (Failed(hr))
			{
				SafeRelease(mediaTypeHandler);
				return hr;
			}

			hr = MF.CreateTopologyNode(MFTopologyType.OutputNode, out node);
			if (Failed(hr))
			{
				SafeRelease(mediaTypeHandler);
				return hr;
			}
			
			if(majorType == MFMediaType.Video)
			{
				ExternalMediaSink extMediaSink = new ExternalMediaSink();
				

				mediaSink = (IMFMediaSinkAlt) extMediaSink;

				hr = mediaSink.GetStreamSinkCount(out streamSinkCount);
				if (Failed(hr))
				{
					mediaSink = null;
					return hr;
				}

				hr = mediaSink.GetStreamSinkByIndex(0, out streamSink);
				if (Failed(hr))
				{
					mediaSink = null;
					return hr;
				}

				hr = node.SetObject(streamSink);
				if (Failed(hr))
				{
					mediaSink = null;
					return hr;
				}
			}else if(majorType == MFMediaType.Audio)
			{
				hr = MF.CreateAudioRendererActivate(out activate);
				if (Failed(hr))
				{
					return hr;
				}

				hr = node.SetObject(activate);
				if (Failed(hr))
				{
					return hr;
				}
			}

			mediaTypeHandler = null;
			activate = null;
			streamSink = null;

			return hr;
		}

		private static HResult RunMediaSession(IMFMediaSession mediaSession)
		{
			HResult hr = S_OK;

			bool receiveSessionEvent = true;

			while (receiveSessionEvent)
			{
				HResult hrStatus = S_OK;
				IMFMediaEvent mediaEvent = null;
				MediaEventType eventType = MediaEventType.MEUnknown;

				MFTopoStatus topoStatus = MFTopoStatus.Invalid;

				hr = mediaSession.GetEvent(MFEventFlag.None, out mediaEvent);

				if (Succeeded(hr))
				{
					hr = mediaEvent.GetStatus(out hrStatus);
				}

				if (Succeeded(hr))
				{
					hr = mediaEvent.GetType(out eventType);
				}

				if(Succeeded(hr) && Succeeded(hrStatus))
				{
					switch (eventType)
					{
						case MediaEventType.MESessionTopologySet:
							Debug.WriteLine("MediaSession:TopologySetEvent");
							break;
						case MediaEventType.MESessionTopologyStatus:
							Debug.WriteLine("MediaSession:TopologStatusEvent");

							hr = mediaEvent.GetUINT32(MF_EVENT_TOPOLOGY_STATUS, out int topoStatusInt);

							if (Succeeded(hr))
							{
								topoStatus = (MFTopoStatus)topoStatusInt;
								switch (topoStatus)
								{
									case MFTopoStatus.Ready:
										Debug.WriteLine("MediaSession:TopologyStatus: MFTopoStatus.Ready");
										hr = mediaSession.Start();
										break;
									default:
										Debug.WriteLine("MediaSession:TopologyStatus: MFTopoStatus." + topoStatus);
										break;
								}
							}
							break;
						case MediaEventType.MESessionClosed:
							Debug.WriteLine("MediaSession:SessionClosedEvent");
							receiveSessionEvent = false;
							break;
						case MediaEventType.MESessionStopped:
							Debug.WriteLine("MediaSession:SesssionStoppedEvent");
							hr = mediaSession.Stop();
							break;
						default:
							Debug.WriteLine("MediaSession:Event: " + eventType);
							break;
					}

					mediaEvent = null;

					if (Failed(hr) || Failed(hrStatus))
					{
						receiveSessionEvent = false;
					}
				}
			}

			return hr;
		}
	}
}
