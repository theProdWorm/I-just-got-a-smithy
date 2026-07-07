using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Editor
{
    public static class TimelineGenerator
    {
        private const int TEXT_SPRITE_HEIGHT = 16;
        private const int MAX_TIMESTAMPS = 10;
        private const int STEPS_BETWEEN_POINTS = 5;
        private const int NUM_INPUTS = 4;
        
        private static readonly Dictionary<string, Sprite> TEXT_SPRITES = new();

        private static Texture2D TIMESTAMPS_TEXTURE;

        public static Texture2D GenerateBeatTimeline(Song song, Rect rect, float scrollX, float zoom)
        {
            // Fill background
            Texture2D beatTimelineTexture = new((int) rect.width, 300);
            Color[] pixels = new Color[beatTimelineTexture.width * beatTimelineTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            beatTimelineTexture.SetPixels(pixels);
            
            float bps = song.BPM / 60.0f; // Beats per second
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;

            // Total widths
            float timeWidth = endTime - startTime;
            int pixelWidth = Mathf.FloorToInt(rect.width);

            // Starting points
            int firstBeat = Mathf.FloorToInt(bps * startTime);
            float firstBeatTime = firstBeat / bps;
            int firstBeatXPos = Mathf.FloorToInt(pixelWidth * (firstBeatTime - startTime) / timeWidth);
            
            // Intervals
            float beatTimeDistance = 1 / bps;
            int beatPixelDistance = Mathf.CeilToInt(pixelWidth * (beatTimeDistance / timeWidth));
            int beatsOnScreen = Mathf.FloorToInt(timeWidth * bps);

            int maxBeats = (int) rect.width / 20;
            
            // Find maximum amount of beats to display
            int numBeats = beatsOnScreen;
            int n = 1;
            while (numBeats > maxBeats)
                numBeats = Mathf.CeilToInt(beatsOnScreen / (float) ++n);

            // Vertical lines
            for (int i = 0; i < numBeats; i++)
            {
                int x = firstBeatXPos + i * n * beatPixelDistance;

                for (int y = 0; y < beatTimelineTexture.height; y++)
                {
                    beatTimelineTexture.SetPixel(x, y, Color.gray3);
                }
            }

            for (int y = 0; y < NUM_INPUTS * 3; y++)
            {
                int yPos = beatTimelineTexture.height * (y % NUM_INPUTS + 1) / (NUM_INPUTS + 1) + y / NUM_INPUTS;
                
                for (int x = 0; x < pixelWidth; x++)
                {
                    beatTimelineTexture.SetPixel(x, yPos, Color.white);
                }
            }

            // Divider line
            for (int x = 0; x < beatTimelineTexture.width; x++)
            {
                beatTimelineTexture.SetPixel(x, 0, Color.white);
            }
            
            beatTimelineTexture.Apply();
            
            return beatTimelineTexture;
        }
        
        public static Texture2D GenerateTimeline(Song song, Rect rect, float scrollX, float zoom, out Texture2D timestampsTexture)
        {
            RebuildSpriteDictionary();
            
            Texture2D timelineTexture = new Texture2D((int) rect.width, 32);

            // Fill timeline background
            Color[] pixels = new Color[timelineTexture.width * timelineTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            timelineTexture.SetPixels(pixels);
            
            // Fill timestamp background
            TIMESTAMPS_TEXTURE = new((int) rect.width, TEXT_SPRITE_HEIGHT);
            pixels = new Color[TIMESTAMPS_TEXTURE.width * TIMESTAMPS_TEXTURE.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.gray1;
            }
            TIMESTAMPS_TEXTURE.SetPixels(pixels);
            
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;

            // Total widths
            float timeWidth = endTime - startTime;
            int pixelWidth = Mathf.FloorToInt(rect.width);

            // Intervals
            float timestampTimeDistance = Mathf.Max(timeWidth / MAX_TIMESTAMPS, 1);
            int timestampPixelDistance = Mathf.FloorToInt(pixelWidth * timestampTimeDistance / timeWidth);

            // Starting points
            int firstTimestampTime = Mathf.CeilToInt(startTime);
            int firstTimestampPixel = Mathf.FloorToInt(pixelWidth * (firstTimestampTime - startTime) / timeWidth);

            int smallStepDistance = timestampPixelDistance / (STEPS_BETWEEN_POINTS + 1);
            
            int numPoints = Mathf.Min(MAX_TIMESTAMPS, Mathf.FloorToInt(timeWidth));
            
            for (int i = 0; i < numPoints; i++)
            {
                int time = Mathf.FloorToInt(firstTimestampTime + i * timestampTimeDistance);
                int x = firstTimestampPixel + i * timestampPixelDistance;

                // Small steps
                for (int j = 0; j < STEPS_BETWEEN_POINTS; j++)
                {
                    int xPos = x + (j + 1) * smallStepDistance;

                    for (int y = 10; y < timelineTexture.height; y++)
                    {
                        timelineTexture.SetPixel(xPos, y, Color.gray3);
                    }
                }
                
                // Big steps
                for (int y = 0; y < timelineTexture.height; y++)
                {
                    timelineTexture.SetPixel(x, y, Color.white);
                }

                AddTimestamp(time, x);
            }
            
            timelineTexture.Apply();

            timestampsTexture = TIMESTAMPS_TEXTURE;
            return timelineTexture;
        }

        private static void AddTimestamp(int timeInSeconds, int xPos)
        {
            int minutes = timeInSeconds / 60;
            int seconds = timeInSeconds % 60;

            string timeText = $"{minutes}:{seconds:00}";
            
            Sprite[] sprites = new Sprite[timeText.Length];
            int totalWidth = 0;
            
            // Sprites and width from text
            for (int i = 0; i < timeText.Length; i++)
            {
                string s = timeText[i].ToString();
                s = s == ":" ? "colon" : s;
                
                if (!TEXT_SPRITES.TryGetValue(s, out var sprite))
                {
                    Debug.LogError("Could not find sprite for " + s);
                    return;
                }
                
                sprites[i] = sprite;
                totalWidth += sprite.texture.width;
            }

            int halfWidth = totalWidth / 2;
            int startX = xPos - halfWidth;
            
            // Offset from the far left of the texture
            int firstOffset = startX < 0 ? Mathf.Abs(startX) : 0;

            // Combine sprites into one texture
            foreach (var sprite in sprites)
            {
                var texture = sprite.texture;
                int width = texture.width;
                var pixels = sprite.texture.GetPixels();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < TEXT_SPRITE_HEIGHT; y++)
                    {
                        int index = x + y * width;
                        int finalXPos = startX + x + firstOffset;
                        
                        var pixel = pixels[index];
                        var backgroundPixel = TIMESTAMPS_TEXTURE.GetPixel(finalXPos, y);
                        
                        // Alpha influence to allow background to shine through
                        var pixelBlend = pixel.a * pixel + (1 - pixel.a) * backgroundPixel;
                        
                        TIMESTAMPS_TEXTURE.SetPixel(finalXPos, y, pixelBlend);
                    }
                }
                
                startX += width;
            }

            TIMESTAMPS_TEXTURE.Apply();
        }

        // Find all text sprites from the resources folder
        private static void RebuildSpriteDictionary()
        {
            TEXT_SPRITES.Clear();
            
            var sprites = Resources.LoadAll<Sprite>("Text Sprites");

            foreach (var sprite in sprites)
                TEXT_SPRITES.Add(sprite.name, sprite);
        }
    }
}