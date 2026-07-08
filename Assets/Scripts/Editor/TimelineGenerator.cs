using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Editor
{
    public static class TimelineGenerator
    {
        private const int TEXT_SPRITE_HEIGHT = 16;
        private const int STEPS_BETWEEN_SECONDS = 5;
        private const int PARTS_PER_BEAT = 16;
        private const int MINIMUM_BEAT_DISTANCE = 20;
        private const int MINIMUM_SECOND_DISTANCE = 56;
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

            int width = Mathf.FloorToInt(rect.width);

            List<int> fullBeatPositions = TimelineUtils.GetAllPositions(x => x / bps, bps * songLength, startTime,
                endTime, width, MINIMUM_BEAT_DISTANCE, out _);
            foreach (var x in fullBeatPositions)
            {
                for (int y = 0; y < beatTimelineTexture.height; y++)
                {
                    beatTimelineTexture.SetPixel(x, y, Color.gray);
                }
            }
            
            int beatDistance = fullBeatPositions.Count == 1 ? 1000 : fullBeatPositions[1] - fullBeatPositions[0];
            int numParts = beatDistance / 16;
            int n = 0;
            while (numParts != 1)
            {
                numParts >>= 1;
                n++;
            }
            numParts <<= Mathf.Min(n, (int) Mathf.Log(PARTS_PER_BEAT, 2));

            List<int> partBeatPositions = TimelineUtils.GetAllPositions(x => x / bps / numParts,
                bps * songLength * numParts, startTime, endTime, width, 1, out _);

            int offset = 0;
            foreach (var pos in partBeatPositions)
            {
                if (fullBeatPositions.Contains(pos))
                    break;

                offset++;
            }

            for (int i = 0; i < partBeatPositions.Count; i++)
            {
                int offsetI = Mathf.Abs(i - offset);
                
                if (offsetI % numParts == 0)
                    continue;

                int sizeCut = 0;

                if (offsetI % (numParts / 2) == 0) // Half
                    sizeCut = 10;
                else if (offsetI % (numParts / 4) == 0) // Quarter
                    sizeCut = 20;
                else if (offsetI % (numParts / 8) == 0) // Eighth
                    sizeCut = 30;
                else if (offsetI % (numParts / 16) == 0) // Sixteenth
                    sizeCut = 40;

                int x = partBeatPositions[i];
                for (int y = sizeCut; y < beatTimelineTexture.height - sizeCut; y++)
                {
                    beatTimelineTexture.SetPixel(x, y, Color.gray2);
                }
            }
            
            // Horizontal (input) lines
            for (int y = 0; y < NUM_INPUTS * 3; y++)
            {
                int yPos = beatTimelineTexture.height * (y % NUM_INPUTS + 1) / (NUM_INPUTS + 1) + y / NUM_INPUTS;
                
                for (int x = 0; x < width; x++)
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
            int width = Mathf.FloorToInt(rect.width);

            var stepPositions = TimelineUtils.GetAllPositions(x => (float) x / (STEPS_BETWEEN_SECONDS + 1),
                songLength * (STEPS_BETWEEN_SECONDS + 1), startTime, endTime, width,
                1, out _);
            foreach (var x in stepPositions)
            {
                for (int y = 10; y < timelineTexture.height; y++)
                {
                    timelineTexture.SetPixel(x, y, Color.gray3);
                }
            }
            
            var secondPositions = TimelineUtils.GetAllPositions(x => x, songLength, startTime, endTime, width, 
                MINIMUM_SECOND_DISTANCE, out var times);
            for (int i = 0; i < secondPositions.Count; i++)
            {
                int x = secondPositions[i];
                int time = Mathf.FloorToInt(times[i]);
                
                for (int y = 0; y < timelineTexture.height; y++)
                {
                    timelineTexture.SetPixel(x, y, Color.gray3);
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
            int startX = Mathf.Clamp(xPos - halfWidth, 0, TIMESTAMPS_TEXTURE.width - totalWidth);
            
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
                        int finalXPos = startX + x;
                        
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