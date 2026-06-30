using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class WaveformGenerator
    {
        public static Texture2D GenerateAudioTexture(Song song, Rect bounds, float scrollX, float zoom)
        {
            var backgroundColor = new Color(0.1568628f, 0.1568628f, 0.1568628f);
            // var waveformColor = new Color(0.7372549f, 0.7372549f, 0.7372549f);

            var width = Mathf.FloorToInt(bounds.width);
            var height = 240;
            
            

            var audioTexture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = backgroundColor;
            
            audioTexture.SetPixels(pixels);
            
            var clip = song.Clip;
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            int samplesPerPixel = Mathf.Max(1, samples.Length / width);
            int centerY = height / 2;

            List<Vector2Int> waveformCoordinates = new();
            float maxAmplitude = 0f;
            
            for (int x = 0; x < width; x++)
            {
                int startSample = x * samplesPerPixel;

                float amplitude = 0f;

                for (int i = 0; i < 3; i++)
                {
                    int sampleIndex = startSample + i;
                    if (sampleIndex >= samples.Length)
                        break;

                    amplitude = Mathf.Max(amplitude, Mathf.Abs(samples[sampleIndex]));
                }
                
                maxAmplitude = Mathf.Max(maxAmplitude, amplitude);

                int waveformHeight = Mathf.RoundToInt(amplitude * centerY);
                
                for (int y = centerY - waveformHeight; y <= centerY + waveformHeight; y++)
                {
                    waveformCoordinates.Add(new Vector2Int(x, y));
                }
            }
            
            var leftColor = new Color(0.8f, 0, 0);
            var rightColor = new Color(0, 0, 0.8f);

            foreach (var pixel in waveformCoordinates)
            {
                float yOffset = Mathf.Abs(pixel.y - centerY) / (height * maxAmplitude);
                float xFactor = pixel.x / (float) width;

                var xColor = Color.Lerp(leftColor, rightColor, xFactor);
                var green = Mathf.Pow(yOffset, 0.5f);

                var color = new Color(xColor.r, green, xColor.b);
                
                audioTexture.SetPixel(pixel.x, pixel.y, color);
            }
            
            audioTexture.Apply();
            return audioTexture;
        }
    }
}