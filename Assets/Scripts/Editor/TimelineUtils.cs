using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor
{
    public static class TimelineUtils
    {
        public static int GetXPosition(float time, float startTime, float endTime, int width)
        {
            float interval = endTime - startTime;
            float localTime = time - startTime;
            float normalizedTime = localTime / interval;

            int xPos = Mathf.FloorToInt(normalizedTime * width);

            return xPos;
        }

        public static List<int> GetAllPositions(Func<int, float> selector, float range, float startTime, float endTime,
            int width, int minDistance, out List<float> correspondingTimes)
        {
            correspondingTimes = new();
            
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
                        continue;
                }

                correspondingTimes.Add(time);
                positions.Add(pos);
            }

            return positions;
        }
    }
}