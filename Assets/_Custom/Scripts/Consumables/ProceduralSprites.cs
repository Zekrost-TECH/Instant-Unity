using System.Collections.Generic;
using UnityEngine;

public static class ProceduralSprites
{
    private const int TextureSize = 64;
    // Medio grosor (px de textura) del contorno. Las figuras son huecas a propósito:
    // rellenas se confundían con enemigos del mismo color. "disc" es la única rellena.
    private const float OutlineHalfWidth = 5f;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string shape)
    {
        if (cache.TryGetValue(shape, out Sprite cached)) return cached;

        Texture2D texture = CreateTexture(shape);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Pickup_" + shape;
        cache[shape] = sprite;
        return sprite;
    }

    private static Texture2D CreateTexture(string shape)
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = "PickupTex_" + shape;

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - TextureSize * 0.5f, y + 0.5f - TextureSize * 0.5f);
                float distance = SignedDistance(shape, p);
                // Antes era 0.5 - distance: el alfa quedaba invertido y cada pickup se veía
                // como un cuadrado opaco con la figura recortada en el centro.
                float alpha = shape == "disc"
                    ? Mathf.Clamp01(0.5f + distance)
                    : Mathf.Clamp01(0.5f + OutlineHalfWidth - Mathf.Abs(distance));
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>Distancia con signo: positiva dentro de la forma, negativa fuera.</summary>
    private static float SignedDistance(string shape, Vector2 p)
    {
        switch (shape)
        {
            case "circle":
            case "disc":
                return 26f - p.magnitude;

            case "dashedcircle":
                // 12 trazos: los huecos quedan muy lejos del contorno y salen transparentes.
                return Mathf.Sin(Mathf.Atan2(p.y, p.x) * 12f) > 0f ? 26f - p.magnitude : -100f;

            case "square":
                return 22f - Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));

            case "diamond":
                return 26f - (Mathf.Abs(p.x) + Mathf.Abs(p.y));

            case "triangle":
                // Triángulo apuntando hacia arriba (sentido anti-horario)
                return PolygonDistance(p, new[]
                {
                    new Vector2(0f, -26f),
                    new Vector2(26f, 24f),
                    new Vector2(-26f, 24f)
                });

            case "hexagon":
                // Hexágono de punta hacia arriba, radio 26
                float r = 26f;
                float h = r * 0.8660254f;
                return PolygonDistance(p, new[]
                {
                    new Vector2(0f, r),
                    new Vector2(-h, r * 0.5f),
                    new Vector2(-h, -r * 0.5f),
                    new Vector2(0f, -r),
                    new Vector2(h, -r * 0.5f),
                    new Vector2(h, r * 0.5f)
                });

            default:
                return 26f - p.magnitude;
        }
    }

    /// <summary>Mínima distancia con signo a los lados de un polígono convexo anti-horario.</summary>
    private static float PolygonDistance(Vector2 p, Vector2[] vertices)
    {
        float minDistance = float.MaxValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[(i + 1) % vertices.Length];
            Vector2 ab = b - a;

            // Normal interior (polígono en sentido anti-horario)
            Vector2 normal = new Vector2(-ab.y, ab.x);
            if (normal.sqrMagnitude > 0.0001f) normal.Normalize();

            float distance = Vector2.Dot(p - a, normal);
            if (distance < minDistance) minDistance = distance;
        }

        return minDistance;
    }
}
