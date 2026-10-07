using UnityEngine;
using System.Collections.Generic;

public static class MarkerDot
{
    public enum Kind { None, Green, Red, Blue, Orange, Magenta }

    // คืนตำแหน่งกึ่งกลางของกลุ่มพิกเซลสีที่ต้องการ (พิกัดพิกเซล, origin บนซ้าย) หรือ null ถ้าไม่เจอ
    public static Vector2? Find(Texture2D markTex, Kind kind)
    {
        Color32[] px = markTex.GetPixels32();
        int w = markTex.width, h = markTex.height;
        float sx = 0, sy = 0; int n = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 c = px[y * w + x];
                if (c.a < 200) continue;
                if (!Matches(c, kind)) continue;
                sx += x; sy += y; n++;
            }
        }

        if (n == 0) return null;
        // texture ของ Unity นับ y จากล่างขึ้นบน ส่วนที่เราต้องการคือพิกัดแบบภาพ (y จากบนลงล่าง)
        return new Vector2(sx / n, h - 1 - (sy / n));
    }

        // หาจุดสีเดียวกันหลายกลุ่มในภาพเดียว (สำหรับจุดที่มีหลายจุด เช่น glow หรือ trail)
    public static List<Vector2> FindAll(Texture2D markTex, Kind kind)
    {
        Color32[] px = markTex.GetPixels32();
        int w = markTex.width, h = markTex.height;
        bool[] visited = new bool[w * h];
        var results = new List<Vector2>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (visited[idx]) continue;
                Color32 c = px[idx];
                if (c.a < 200 || !Matches(c, kind)) continue;

                var stack = new Stack<(int, int)>();
                stack.Push((x, y));
                visited[idx] = true;
                float sx = 0, sy = 0; int n = 0;

                while (stack.Count > 0)
                {
                    (int cx, int cy) = stack.Pop();
                    sx += cx; sy += cy; n++;

                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        int nidx = ny * w + nx;
                        if (visited[nidx]) continue;
                        Color32 nc = px[nidx];
                        if (nc.a < 200 || !Matches(nc, kind)) continue;
                        visited[nidx] = true;
                        stack.Push((nx, ny));
                    }
                }

                results.Add(new Vector2(sx / n, h - 1 - (sy / n)));
            }
        }

        return results;
    }

    static bool Matches(Color32 c, Kind kind)
    {
        switch (kind)
        {
            case Kind.Green: return c.g > 150 && c.r < 100 && c.b < 100;
            case Kind.Red:   return c.r > 150 && c.g < 100 && c.b < 100;
            case Kind.Blue:  return c.b > 150 && c.r < 100 && c.g < 100;
            case Kind.Orange: return c.r > 150 && c.g > 80 && c.g < 180 && c.b < 80;
            case Kind.Magenta: return c.r > 150 && c.b > 150 && c.g < 100;
            default: return false;
        }
    }
}