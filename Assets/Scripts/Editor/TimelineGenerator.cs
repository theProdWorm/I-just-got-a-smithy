using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Editor
{
    public static class TimelineGenerator
    {
        private const int FONT_HEIGHT = 16;
        private const int FONT_WIDTH = 8;
        private const int STEPS_BETWEEN_SECONDS = 5;
        private const int MINIMUM_SECOND_DISTANCE = 56;
        
        public static void DrawBeatTimeline(Song song, Rect rect, float scrollX, float zoom)
        {
            // Fill background
            EditorGUI.DrawRect(rect, Color.black);
            
            float bps = song.BPM / 60.0f; // Beats per second
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;

            int width = Mathf.FloorToInt(rect.width);

            int numParts = TimelineUtils.GetBeatResolution(song, rect, scrollX, zoom, out var fullBeatPositions);
            List<int> partBeatPositions = TimelineUtils.GetAllPositions(x => x / bps / numParts,
                bps * songLength * numParts, startTime, endTime, width, numParts > 1 ? 1 : TimelineUtils.MINIMUM_BEAT_DISTANCE, out _, out _);

            int offset = 0;
            foreach (var pos in partBeatPositions)
            {
                if (fullBeatPositions.Contains(pos))
                    break;

                offset++;
            }

            for (int i = 0; numParts > 0 && i < partBeatPositions.Count; i++)
            {
                int offsetI = Mathf.Abs(i - offset);

                var color = Color.gray;
                int sizeCut = 0;

                int n = numParts;
                while (offsetI % n != 0)
                {
                    sizeCut += 10;
                    n /= 2;
                    
                    color = Color.gray2;
                }
                
                int x = partBeatPositions[i];
                Rect line = new Rect(rect.x + x, rect.y + sizeCut, 1, rect.height - sizeCut * 2);
                EditorGUI.DrawRect(line, color);
            }

            int numInputs = TimelineUtils.NUM_INPUTS;
            // Horizontal (input) lines
            for (int y = 0; y < numInputs; y++)
            {
                int yPos = (int) rect.height * (y + 1) / (numInputs + 1) + y / numInputs;
                
                Rect line = new Rect(rect.x, rect.y + yPos - 1, width, 3);
                EditorGUI.DrawRect(line, Color.white);
            }

            Rect dividerLine = new Rect(rect.x, rect.y + rect.height - 3, width, 3);
            EditorGUI.DrawRect(dividerLine, Color.white);
        }
        
        public static void DrawTimeline(Song song, Rect rect, float scrollX, float zoom)
        {
            EditorGUI.DrawRect(rect, Color.black);
            
            // Fill timestamp background
            Rect timestampRect = new Rect(rect.x, rect.y, rect.width, FONT_HEIGHT);
            EditorGUI.DrawRect(timestampRect, Color.black);
            
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;
        
            // Total widths
            int width = Mathf.FloorToInt(rect.width);
            
            var secondPositions = TimelineUtils.GetAllPositions(x => x, songLength, startTime, endTime, width, 
                MINIMUM_SECOND_DISTANCE, out var times, out _);
            for (int i = 0; i < secondPositions.Count; i++)
            {
                int x = secondPositions[i];
                int time = Mathf.FloorToInt(times[i]);
                
                Rect line = new Rect(rect.x + x, rect.y, 1, rect.height);
                EditorGUI.DrawRect(line, Color.gray3);
                
                AddTimestamp(rect, time, x);
            }
            
            // var stepPositions = TimelineUtils.GetAllPositions(x => (float) x / (STEPS_BETWEEN_SECONDS + 1),
            //     secondPositions.Count * (STEPS_BETWEEN_SECONDS + 1), startTime, endTime, width,
            //     1, out _, out _);
            // foreach (var x in stepPositions)
            // {
            //     for (int y = 10; y < timelineTexture.height; y++)
            //     {
            //         timelineTexture.SetPixel(x, y, Color.gray3);
            //     }
            // }
        }
        
        private static void AddTimestamp(Rect rect, int timeInSeconds, int xPos)
        {
            int minutes = timeInSeconds / 60;
            int seconds = timeInSeconds % 60;
        
            string timeText = $"{minutes}:{seconds:00}";

            int width = timeText.Length * FONT_WIDTH;
            int halfWidth = width / 2;
            int startX = Mathf.Clamp(xPos - halfWidth, 0, (int) rect.width - width);
            
            Rect labelRect = new Rect(rect.x + startX, rect.y, width, FONT_HEIGHT);
            
            EditorGUI.LabelField(labelRect, timeText);
        }
    }
}