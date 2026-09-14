using UnityEngine;

public static class ToneGenerator
{
    public enum Wave { Sine, Square, Sawtooth }

    private const int SampleRate = 44100;

    public static AudioClip CreateTone(string name, float frequency, float duration, Wave wave, bool fadeOut)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float value = Oscillate(wave, frequency, t);
            if (fadeOut)
            {
                value *= 1f - (i / (float)sampleCount);
            }
            samples[i] = value * 0.4f;
        }

        return BuildClip(name, samples);
    }

    public static AudioClip CreateArpeggio(string name, float[] frequencies, float noteDuration)
    {
        int samplesPerNote = Mathf.CeilToInt(SampleRate * noteDuration);
        int sampleCount = samplesPerNote * frequencies.Length;
        float[] samples = new float[sampleCount];

        for (int n = 0; n < frequencies.Length; n++)
        {
            for (int i = 0; i < samplesPerNote; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = 1f - (i / (float)samplesPerNote);
                samples[n * samplesPerNote + i] = Oscillate(Wave.Sine, frequencies[n], t) * 0.4f * envelope;
            }
        }

        return BuildClip(name, samples);
    }

    public static AudioClip CreateLoopMelody(string name, float[] notes, float noteDuration)
    {
        int samplesPerNote = Mathf.CeilToInt(SampleRate * noteDuration);
        int sampleCount = samplesPerNote * notes.Length;
        float[] samples = new float[sampleCount];

        for (int n = 0; n < notes.Length; n++)
        {
            for (int i = 0; i < samplesPerNote; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / (float)samplesPerNote);
                samples[n * samplesPerNote + i] = Oscillate(Wave.Sine, notes[n], t) * 0.25f * envelope;
            }
        }

        AudioClip clip = BuildClip(name, samples);
        return clip;
    }

    private static AudioClip BuildClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Oscillate(Wave wave, float frequency, float t)
    {
        float phase = frequency * t;
        switch (wave)
        {
            case Wave.Square:
                return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * phase));
            case Wave.Sawtooth:
                return 2f * (phase - Mathf.Floor(phase + 0.5f));
            default:
                return Mathf.Sin(2f * Mathf.PI * phase);
        }
    }
}
