namespace SmartCar.Media;

public interface ISoundPlayer
{
	Task PlayWavOnSpeaker(byte[] data, CancellationToken ct);
	Task PlaySoundOnSpeaker(SoundData data, CancellationToken ct);
}