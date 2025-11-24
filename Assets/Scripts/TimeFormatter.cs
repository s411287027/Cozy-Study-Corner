using UnityEngine;

public static class TimeFormatter
{
    public static string FormatHMS(float seconds)
    {
        if (seconds < 0) seconds = 0;

        int h = Mathf.FloorToInt(seconds / 3600f);
        int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);

        return string.Format("{0:00}:{1:00}:{2:00}", h, m, s);
    }
}
