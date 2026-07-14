using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;

namespace Editor
{
    public static class TimelineUtils
    {
        public const int MINIMUM_BEAT_DISTANCE = 20;
        public const int PARTS_PER_BEAT = 16;
        public const int NUM_INPUTS = 4;
        
        public static int GetXPosition(float time, float startTime, float endTime, int width)
        {
            float interval = endTime - startTime;
            float localTime = time - startTime;
            float normalizedTime = localTime / interval;

            int xPos = Mathf.FloorToInt(normalizedTime * width);

            return xPos;
        }

        /// <summary>
        /// Rounds a time value to the closest beat. 
        /// </summary>
        /// <returns>A time value rounded to the closest beat.</returns>
        public static float SnapToBeat(float inputTime, Song song, Rect rect, float scrollX, float zoom)
        {
            float bps = song.BPM / 60.0f; // Beats per second
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;
            
            int width = Mathf.FloorToInt(rect.width);
            
            int numParts = GetBeatResolution(song, rect, scrollX, zoom, out _);
            List<int> partBeatPositions = GetAllPositions(x => x / bps / numParts,
                bps * songLength * numParts, startTime, endTime, width, numParts > 1 ? 1 : MINIMUM_BEAT_DISTANCE,
                out var times, out _);

            float closestTime = 0;
            float smallestTimeDiff = 100000;
            foreach (var time in times)
            {
                float timeDiff = Mathf.Abs(time - inputTime);

                if (timeDiff < smallestTimeDiff)
                {
                    closestTime = time;
                    smallestTimeDiff = timeDiff;
                }
            }
            
            return closestTime;
        }

        public static int GetBeatResolution(Song song, Rect rect, float scrollX, float zoom, out List<int> fullBeatPositions)
        {
            float bps = song.BPM / 60.0f; // Beats per second
            float songLength = song.Clip.length;
            
            float startTime = songLength * scrollX;
            float endTime = startTime + songLength * zoom;
            
            int width = Mathf.FloorToInt(rect.width);
            
            fullBeatPositions = GetAllPositions(x => x / bps, bps * songLength, startTime,
                endTime, width, MINIMUM_BEAT_DISTANCE, out _, out bool wasDataCut);
            
            int beatDistance = wasDataCut ? 0 : fullBeatPositions.Count == 1 ? 1000 : fullBeatPositions[1] - fullBeatPositions[0];
            
            int numParts = beatDistance / 16;
            int maxParts = (int) Mathf.Log(PARTS_PER_BEAT, 2);
            
            int n = 0;
            while (numParts > 0 && numParts != 1 && n < maxParts)
            {
                numParts >>= 1;
                n++;
            }
            numParts = 1;
            numParts <<= n;

            return numParts;
        }
        
        public static List<int> GetAllPositions(Func<int, float> selector, float range, float startTime, float endTime,
            int width, int minDistance, out List<float> correspondingTimes, out bool wasDataCut)
        {
            correspondingTimes = new();
            wasDataCut = false;
            
            float[] times = Enumerable.Range(0, Mathf.FloorToInt(range))
                .Select(selector)
                .Where(t => t > startTime && t < endTime)
                .ToArray();

            List<int> positions = new();
            foreach (var time in times)
            {
                int pos = GetXPosition(time, startTime, endTime, width);

                if (positions.Count > 0)
                {
                    int lastPos = positions[^1];
                    int distance = pos - lastPos;

                    if (distance < minDistance)
                    {
                        wasDataCut = true;
                        continue;
                    }
                }

                correspondingTimes.Add(time);
                positions.Add(pos);
            }

            return positions;
        }
    }
}