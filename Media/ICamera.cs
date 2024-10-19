using static SmartCar.Media.IotBindingsCamera;

namespace SmartCar.Media
{
	public interface ICamera : IDisposable
	{
		Task<byte[]> GetPictureAsJpeg();
		Task<TimelapseReader> CaptureTimelapse();
	}
}