using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterglow
{
    public static class AfterglowArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Material spriteMaterial;

        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial != null) return spriteMaterial;
                spriteMaterial = Resources.Load<Material>("AfterglowSprite");
                if (spriteMaterial != null) return spriteMaterial;

                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null) spriteMaterial = new Material(shader) { name = "Afterglow Runtime Sprite Material" };
                return spriteMaterial;
            }
        }

        public static Sprite AuthoredSprite(string resourceName, float pixelsPerUnit, Vector2? pivot = null)
        {
            string key = "authored_" + resourceName + "_" + pixelsPerUnit;
            if (Cache.TryGetValue(key, out Sprite found)) return found;
            Texture2D texture = Resources.Load<Texture2D>("Art/" + resourceName);
            if (texture == null)
            {
                Debug.LogError("Missing authored AFTERGLOW art: Resources/Art/" + resourceName + ".png");
                return Pixel("pixel");
            }
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Cache[key] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                pivot ?? new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        public static Sprite Disc(string key, Color fill, Color rim)
        {
            if (Cache.TryGetValue(key, out Sprite found)) return found;
            const int size = 128;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f - size * .5f) / (size * .5f);
                float ny = (y + .5f - size * .5f) / (size * .5f);
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = 1f - Mathf.SmoothStep(.94f, 1f, d);
                Color c = Color.Lerp(fill, rim, Mathf.Clamp01((d - .68f) / .28f));
                c.a *= alpha;
                pixels[y * size + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Cache[key] = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }

        public static Sprite Panel(string key, Color body, Color edge, Color highlight)
        {
            if (Cache.TryGetValue(key, out Sprite found)) return found;
            const int size = 96;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + .5f - size * .5f) / (size * .5f));
                float ny = Mathf.Abs((y + .5f - size * .5f) / (size * .5f));
                float d = Mathf.Max(nx, ny);
                float corner = Mathf.Sqrt(Mathf.Max(0f, nx * nx + ny * ny - .78f));
                float alpha = 1f - Mathf.SmoothStep(.96f, 1f, Mathf.Max(d, corner));
                Color c = Color.Lerp(body * .78f, body, y / (float)size);
                if (y > size - 7) c = Color.Lerp(highlight, body, (size - y) / 7f);
                if (d > .88f) c = Color.Lerp(c, edge, Mathf.InverseLerp(.88f, .98f, d));
                c.a = alpha;
                pixels[y * size + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Cache[key] = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24f, 24f, 24f, 24f));
        }

        public static Sprite Polygon(string key, Color fill, Color rim, params Vector2[] points)
        {
            if (Cache.TryGetValue(key, out Sprite found)) return found;
            const int size = 160;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + .5f) / size, (y + .5f) / size);
                bool inside = PointInside(p, points);
                float edgeDistance = DistanceToEdges(p, points);
                float alpha = inside ? 1f : 0f;
                if (!inside && edgeDistance < .008f) alpha = 1f - Mathf.SmoothStep(0f, .008f, edgeDistance);
                if (alpha <= 0f) continue;
                Color c = Color.Lerp(fill * .72f, fill, y / (float)size);
                if (edgeDistance < .018f) c = Color.Lerp(c, rim, 1f - edgeDistance / .018f);
                c.a = alpha;
                pixels[y * size + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Cache[key] = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }









        public static Sprite Pixel(string key)
        {
            if (Cache.TryGetValue(key, out Sprite found)) return found;
            // Most callers scale this sprite in world units. At 100 PPU, a 2x2 texture
            // was only .02 units wide, so floor edges and other broad lines nearly vanished.
            const int size = 100;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Cache[key] = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.name = "Afterglow Generated Art";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static bool PointInside(Vector2 p, Vector2[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[i], b = points[j];
                if (((a.y > p.y) != (b.y > p.y)) && p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + .00001f) + a.x) inside = !inside;
            }
            return inside;
        }

        private static float DistanceToEdges(Vector2 p, Vector2[] points)
        {
            float best = 10f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.00001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + t * ab));
            }
            return best;
        }
    }
}
