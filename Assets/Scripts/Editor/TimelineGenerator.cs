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
        private const int MAX_POINTS = 5;
        
        private static readonly Dictionary<string, Sprite> TEXT_SPRITES = new();

        private static Texture2D TIMESTAMPS_TEXTURE;
        
        public static Texture2D GenerateTimeline(Song song, Rect rect, float scrollX, float zoom, out Texture2D timestampsTexture)
        {
            RebuildSpriteDictionary();
            
            Texture2D timelineTexture = new Texture2D((int) rect.width, 300);

            Color[] pixels = new Color[timelineTexture.width * timelineTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            timelineTexture.SetPixels(pixels);
            
            TIMESTAMPS_TEXTURE = new((int) rect.width, 32);
            pixels = new Color[TIMESTAMPS_TEXTURE.width * TIMESTAMPS_TEXTURE.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            TIMESTAMPS_TEXTURE.SetPixels(pixels);
            
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;

            float timeWidth = endTime - startTime;
            int pixelWidth = Mathf.FloorToInt(rect.width);

            float timestampTimeDistance = Mathf.Max(timeWidth / MAX_POINTS, 1);
            int timestampPixelDistance = Mathf.FloorToInt(pixelWidth * timestampTimeDistance / timeWidth);

            int firstTimestampTime = Mathf.CeilToInt(startTime);
            int firstTimestampPixel = Mathf.FloorToInt(pixelWidth * (firstTimestampTime - startTime) / timeWidth);

            for (int i = 0; i < MAX_POINTS; i++)
            {
                int time = Mathf.FloorToInt(firstTimestampTime + i * timestampTimeDistance);
                int x = firstTimestampPixel + i * timestampPixelDistance;

                for (int y = 0; y < rect.height; y++)
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
            
            int width = 32 * timeText.Length;
            int halfWidth = width / 2;

            Color[] pixels = new Color[width * 32];

            for (int i = 0; i < timeText.Length; i++)
            {
                string s = timeText[i].ToString();
                s = s == ":" ? "colon" : s;
                
                if (!TEXT_SPRITES.TryGetValue(s, out var sprite))
                {
                    Debug.LogError("Could not find sprite for " + s);
                    return;
                }
                
                int startX = i * 32;
                var spritePixels = sprite.texture.GetPixels();
                
                Debug.Log($"s: {s}, sprite: {sprite}");
                
                for (int x = 0; x < 32; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        int spriteIndex = x + y * 32;
                        
                        TIMESTAMPS_TEXTURE.SetPixel(startX + xPos + x, y, spritePixels[spriteIndex]);
                    }
                }
            }

            goto retur;
            
            xPos -= halfWidth;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    int index = x + y * 32;

                    var spritePixel = pixels[index];
                    var backgroundPixel = TIMESTAMPS_TEXTURE.GetPixel(xPos + x, y);

                    var pixelColor = spritePixel.a * spritePixel + (1 - spritePixel.a) * backgroundPixel;
                    
                    TIMESTAMPS_TEXTURE.SetPixel(xPos + x, y, pixelColor);
                }
            }
            
            retur:
            TIMESTAMPS_TEXTURE.Apply();
        }

        private static void RebuildSpriteDictionary()
        {
            TEXT_SPRITES.Clear();
            
            var sprites = Resources.LoadAll<Sprite>("Text Sprites");

            foreach (var sprite in sprites)
                TEXT_SPRITES.Add(sprite.name, sprite);
        }
    }
}