using UnityEngine;

public static class VesperArtFactory
{
    private static Sprite square;
    private static Sprite glow;
    private static Sprite hero;
    private static Sprite orb;

    public static Sprite Square
    {
        get
        {
            if (square != null) return square;
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return square;
        }
    }

    public static Sprite Glow
    {
        get
        {
            if (glow != null) return glow;
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f)) / (size * 0.5f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            glow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return glow;
        }
    }

    public static Sprite Hero
    {
        get
        {
            if (hero != null) return hero;
            const int w = 96, h = 128, scale = 4;
            Texture2D texture = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.025f, 0.045f, 0.075f, 1f);
            Color cloak = new Color(0.09f, 0.17f, 0.23f, 1f);
            Color cloakLight = new Color(0.18f, 0.31f, 0.37f, 1f);
            Color glowColor = new Color(0.39f, 0.95f, 0.87f, 1f);
            for (int y = 0; y < h * scale; y++)
            for (int x = 0; x < w * scale; x++) texture.SetPixel(x, y, clear);
            FillEllipse(texture, 48, 81, 27, 31, outline, scale);
            FillEllipse(texture, 48, 82, 21, 25, cloak, scale);
            FillPolygon(texture, new Vector2[] { new(30, 63), new(66, 63), new(83, 7), new(13, 7) }, outline, scale);
            FillPolygon(texture, new Vector2[] { new(34, 59), new(62, 59), new(73, 12), new(23, 12) }, cloak, scale);
            FillPolygon(texture, new Vector2[] { new(35, 51), new(45, 49), new(35, 16), new(27, 14) }, cloakLight, scale);
            FillEllipse(texture, 48, 89, 17, 18, new Color(0.035f, 0.075f, 0.1f), scale);
            FillPolygon(texture, new Vector2[] { new(33, 94), new(39, 97), new(43, 94), new(39, 91) }, glowColor, scale);
            FillPolygon(texture, new Vector2[] { new(53, 94), new(59, 97), new(63, 94), new(59, 91) }, glowColor, scale);
            FillPolygon(texture, new Vector2[] { new(43, 78), new(48, 74), new(53, 78), new(48, 83) }, new Color(0.88f, 0.63f, 0.34f), scale);
            texture.Apply();
            hero = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.08f), 60f);
            return hero;
        }
    }

    public static Sprite Orb
    {
        get
        {
            if (orb != null) return orb;
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) * 0.5f) / (size * 0.5f);
                float a = Mathf.Clamp01((1f - d) * 8f);
                float core = Mathf.Clamp01(1f - d * 1.8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * (0.35f + core * 0.65f)));
            }
            texture.Apply();
            orb = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return orb;
        }
    }

    public static void FillEllipse(Texture2D t, float cx, float cy, float rx, float ry, Color color, int scale)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt((cx - rx) * scale));
        int maxX = Mathf.Min(t.width - 1, Mathf.CeilToInt((cx + rx) * scale));
        int minY = Mathf.Max(0, Mathf.FloorToInt((cy - ry) * scale));
        int maxY = Mathf.Min(t.height - 1, Mathf.CeilToInt((cy + ry) * scale));
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            float nx = (x / (float)scale - cx) / rx;
            float ny = (y / (float)scale - cy) / ry;
            if (nx * nx + ny * ny <= 1f) t.SetPixel(x, y, color);
        }
    }

    public static void FillPolygon(Texture2D t, Vector2[] points, Color color, int scale)
    {
        float minX = points[0].x, maxX = minX, minY = points[0].y, maxY = minY;
        foreach (Vector2 p in points) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
        for (int y = Mathf.Max(0, Mathf.FloorToInt(minY * scale)); y <= Mathf.Min(t.height - 1, Mathf.CeilToInt(maxY * scale)); y++)
        for (int x = Mathf.Max(0, Mathf.FloorToInt(minX * scale)); x <= Mathf.Min(t.width - 1, Mathf.CeilToInt(maxX * scale)); x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / scale, (y + 0.5f) / scale);
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                if (((points[i].y > p.y) != (points[j].y > p.y)) && p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
            if (inside) t.SetPixel(x, y, color);
        }
    }
}
