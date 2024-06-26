using CSCore;
namespace CsXFL;
public static class SoundUtils
{
    public static ArraySegment<byte> ConvertFlacToWav(string flacFilePath)
    {
        using CSCore.Codecs.FLAC.FlacFile flacFile = new(flacFilePath);
        MemoryStream memStream = new();
        flacFile.ChangeSampleRate(44000).ToSampleSource().ToWaveSource(16).WriteToWaveStream(memStream);
        bool success = memStream.TryGetBuffer(out ArraySegment<byte> segment);
        if (!success)
            throw new InvalidOperationException("Failed to convert FLAC to WAV.");
        return segment;
    }
    public static ArraySegment<byte> ConvertFlacToWav(string flacFilePath, SoundItem itemToUpdate)
    {
        using CSCore.Codecs.FLAC.FlacFile flacFile = new(flacFilePath);
        MemoryStream memStream = new();
        var wavData = flacFile.ChangeSampleRate(44000).ToSampleSource().ToWaveSource(16);
        wavData.WriteToWaveStream(memStream);
        bool success = memStream.TryGetBuffer(out ArraySegment<byte> segment);
        if (!success)
            throw new InvalidOperationException("Failed to convert FLAC to WAV.");
        string format = GetSoundFormatString(wavData.WaveFormat.SampleRate, wavData.WaveFormat.BitsPerSample, wavData.WaveFormat.Channels);
        itemToUpdate.Format = format;
        itemToUpdate.SampleCount = (int)Math.Round(wavData.Length / (wavData.WaveFormat.BitsPerSample / 8.0) / wavData.WaveFormat.Channels);
        return segment;
    }
    public static string GetSoundFormatString(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        string format = $"{file.Properties.AudioSampleRate / 1000}kHz {file.Properties.BitsPerSample}bit {(file.Properties.AudioChannels == 1 ? "Mono" : "Stereo")}";
        return format;
    }
    public static string GetSoundFormatString(int sampleRate, int bitsPerSample, int channels)
    {
        string format = $"{sampleRate / 1000}kHz {bitsPerSample}bit {(channels == 1 ? "Mono" : "Stereo")}";
        return format;
    }
    public static int GetSoundSampleCount(string soundFilePath)
    {
        using var file = TagLib.File.Create(soundFilePath);
        int sampleCount = (int)Math.Round(file.Properties.AudioSampleRate * file.Properties.Duration.TotalSeconds);
        return sampleCount;
    }
    public static double GetSoundDuration(string soundFilePath)
    {
        using var file = CSCore.Codecs.CodecFactory.Instance.GetCodec(soundFilePath);
        double duration = file.GetLength().TotalMilliseconds;
        return duration;
    }
}