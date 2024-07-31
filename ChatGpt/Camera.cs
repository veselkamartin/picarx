using Emgu.CV;
using Emgu.CV.Structure;

namespace PicarX.ChatGpt;

public class Camera : IDisposable
{
	private bool _disposedValue;
	VideoCapture? _capture;

	public byte[] GetPictureAsJpeg()
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);

		_capture ??= new VideoCapture();

		using var frame = new Mat();
		_capture.Read(frame);
		var jpeg = frame.ToImage<Bgr, byte>().ToJpegData();
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