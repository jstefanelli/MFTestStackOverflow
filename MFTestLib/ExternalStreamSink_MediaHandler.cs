using MediaFoundation;
using static MediaFoundation.HResult;
using static MediaFoundation.MFAttributesClsid;
using static MediaFoundation.MFMediaType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MFTestLib
{
	public partial class ExternalStreamSink : IMFMediaTypeHandler
	{
		private IMFMediaType MediaType;

		public HResult IsMediaTypeSupported(IMFMediaType pMediaType, IntPtr ppMediaType)
		{
			Debug.WriteLine("StreamSink:IsMediaTypeSupported");

			if (pMediaType == null)
				return E_INVALIDARG;

			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				Guid MajorType = Guid.Empty;
				Guid MinorType = Guid.Empty;

				hr = pMediaType.GetGUID(MF_MT_MAJOR_TYPE, out MajorType);
				if (Failed(hr))
					return hr;

				hr = pMediaType.GetGUID(MF_MT_SUBTYPE, out MinorType);
				if (Failed(hr))
					return hr;

				if (MajorType != Video)
					return MF_E_INVALIDTYPE;

				if (MinorType != RGB32)
					return MF_E_INVALIDTYPE;

				if(ppMediaType != IntPtr.Zero)
				{
					Marshal.WriteIntPtr(ppMediaType, IntPtr.Zero);
				}

				return S_OK;
			}

		}

		public HResult GetMediaTypeCount(out int pdwTypeCount)
		{
			Debug.WriteLine("StreamSInk:GetMediaTypeCount");
			pdwTypeCount = -1;
			lock (this)
			{
				HResult hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				pdwTypeCount = 1;
			}
			return S_OK;
		}

		public HResult GetMediaTypeByIndex(int dwIndex, out IMFMediaType ppType)
		{
			Debug.WriteLine("StreamSink:GetMediaTypeByIndex");

			ppType = null;

			if (dwIndex != 0)
				return MF_E_NO_MORE_TYPES;

			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;
				try
				{
					MF.CreateMediaType(out IMFMediaType type).ThrowExceptionOnError();
					type.SetGUID(MF_MT_MAJOR_TYPE, Video).ThrowExceptionOnError();
					type.SetGUID(MF_MT_SUBTYPE, RGB32).ThrowExceptionOnError();

					ppType = type;
					//Marshal.AddRef(Marshal.GetIUnknownForObject(ppType));
				}catch(Exception ex)
				{
					hr = (HResult) Marshal.GetHRForException(ex);
				}
			}

			return hr;
		}

		public HResult SetCurrentMediaType(IMFMediaType pMediaType)
		{
			Debug.WriteLine("StreamSink:SetCurrentMediaType");

			HResult hr;
			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				hr = IsMediaTypeSupported(pMediaType, IntPtr.Zero);

				SafeRelease(MediaType);
				MediaType = pMediaType;

				//TODO: Send Width and Height data to Render

				if (State != StreamState.Paused)
					State = StreamState.Ready;
			}
			return hr;
		}

		public HResult GetCurrentMediaType(out IMFMediaType ppMediaType)
		{
			Debug.WriteLine("StreamSink:GetCurrentMediaType");

			ppMediaType = null;
			HResult hr;

			lock (this)
			{
				hr = CheckShutdown();
				if (Failed(hr))
					return hr;

				if (MediaType == null)
					return MF_E_NOT_INITIALIZED;

				ppMediaType = MediaType;

			}
			return hr;
		}

		public HResult GetMajorType(out Guid pguidMajorType)
		{
			pguidMajorType = Video;
			return S_OK;
		}
	}
}
