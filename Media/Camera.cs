﻿using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;

namespace SmartCar.Media;

public class Camera : IDisposable
{
	private bool _disposedValue;
	VideoCapture? _capture;
	private readonly ILogger<Camera> _logger;

	public Camera(ILogger<Camera> logger)
	{
		_logger = logger;
	}

	public byte[] GetPictureAsJpeg()
	{
		//ldd libcvextern.so | grep "not found"
		//sudo apt-get install python3-vtk9

		//		Iot.Device.Graphics.SkiaSharpAdapter.SkiaSharpAdapter.Register();
		//		VideoConnectionSettings settings = new VideoConnectionSettings(busId: 0, captureSize: (2560, 1920), pixelFormat: VideoPixelFormat.YUYV);
		//using VideoDevice device = VideoDevice.Create(settings);
		//// Capture static image
		//device.Capture("jpg_direct_output.jpg");

		//		// Change capture setting
		//		device.Settings.PixelFormat = VideoPixelFormat.YUV420;

		//		// Get image stream, convert pixel format and save to file
		//		var ms = device.Capture();
		//		Color[] colors = VideoDevice.Yv12ToRgb(new MemoryStream( ms), settings.CaptureSize);
		//		var bitmap = VideoDevice.RgbToBitmap(settings.CaptureSize, colors);
		//		bitmap.SaveToFile("yuyv_to_jpg.jpg", Iot.Device.Graphics.ImageFileType.Jpg);

		ObjectDisposedException.ThrowIf(_disposedValue, this);

		_logger.LogInformation("Camera taking picture");
		_capture ??= new VideoCapture();

		using var frame = new Mat();
		_capture.Read(frame);
		var jpeg = frame.ToImage<Bgr, byte>().ToJpegData();
		_logger.LogInformation("Camera picture taken");
		return jpeg;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_capture?.Dispose();
				_capture = null;
			}
			_disposedValue = true;
		}
	}

	~Camera()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}