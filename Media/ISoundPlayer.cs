namespace SmartCar.Media;

public interface ISoundPlayer
{
	Task PlayWavOnSpeaker(byte[] data);
	Task PlaySoundOnSpeaker(SoundData data);
}