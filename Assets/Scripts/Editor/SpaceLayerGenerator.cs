using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// เครื่องมือเจนภาพอวกาศแบบต่อขอบได้ (seamless) เก็บเป็น PNG ในโปรเจกต์ แล้วตั้งค่า import เป็น Sprite ให้เอง
// เมนู: Tools > Space Layers > Generator
//   Nebula   = พื้นทึบ เมฆสี 3 โทน + เส้นฝุ่นมืด (ใช้ noise แบบวนขอบ + domain warp)
//   StarsFar = ดาวไกล เล็กๆ จำนวนมาก (พื้นโปร่งใส)
//   StarsNear= ดาวใกล้ น้อยกว่า ใหญ่/สว่างกว่า มี glow และประกายบางดวง
// PPU คำนวณให้อัตโนมัติ = ขนาดภาพ / ขนาดต่อชิ้นในโลก (Tile Units)
public class SpaceLayerGenerator : EditorWindow
{
    [MenuItem("Tools/Space Layers/Generator")]
    static void Open() => GetWindow<SpaceLayerGenerator>("Space Layers");

    // ---------- ทั่วไป ----------
    string folder = "Assets/Art/Space";
    int seed = 12345;

    // ---------- Nebula ----------
    int nebulaSize = 2048;
    float nebulaTileUnits = 41f;
    Color nebulaBg = new Color(0.008f, 0.010f, 0.025f);
    Color nebulaColA = new Color(0.45f, 0.18f, 0.75f);
    Color nebulaColB = new Color(0.10f, 0.50f, 0.70f);
    Color nebulaColC = new Color(0.70f, 0.20f, 0.45f);
    float nebulaMaxBright = 0.30f;      // ความสว่างสูงสุดของเมฆ (ต่ำ = ไม่แย่งสายตาจากหุ่น)
    int nebulaBaseCells = 3;            // เมฆก้อนใหญ่แค่ไหน (น้อย = ก้อนใหญ่)
    int nebulaOctaves = 5;
    float nebulaCoverage = 0.5f;        // ปริมาณเมฆ (ต่ำ = เมฆเยอะ)
    float nebulaWarp = 0.6f;            // ความบิดเป็นริ้ว
    float nebulaDust = 0.5f;            // เส้นฝุ่นมืดตัดเมฆ

    // ---------- Stars ----------
    int starSize = 2048;
    float starTileUnits = 26f;

    int farCount = 2600;
    float farBrightPower = 3.0f;        // สูง = ดาวส่วนใหญ่หรี่ มีสว่างน้อยดวง
    Vector2 farSigma = new Vector2(0.55f, 1.2f);

    int nearCount = 520;
    float nearBrightPower = 2.2f;
    Vector2 nearSigma = new Vector2(0.8f, 2.0f);
    float nearGlowChance = 0.5f;
    float nearSpikeChance = 0.25f;

    void OnGUI()
    {
        EditorGUILayout.LabelField("ทั่วไป", EditorStyles.boldLabel);
        folder = EditorGUILayout.TextField("โฟลเดอร์ปลายทาง", folder);
        seed = EditorGUILayout.IntField("Seed", seed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Nebula (ชั้นล่างสุด ทึบ)", EditorStyles.boldLabel);
        nebulaSize = EditorGUILayout.IntPopup("ขนาดภาพ", nebulaSize, new[] { "1024", "2048", "4096" }, new[] { 1024, 2048, 4096 });
        nebulaTileUnits = EditorGUILayout.FloatField("ขนาดต่อชิ้น (หน่วยโลก)", nebulaTileUnits);
        nebulaBg = EditorGUILayout.ColorField("สีพื้น", nebulaBg);
        nebulaColA = EditorGUILayout.ColorField("สีเมฆ A", nebulaColA);
        nebulaColB = EditorGUILayout.ColorField("สีเมฆ B", nebulaColB);
        nebulaColC = EditorGUILayout.ColorField("สีเมฆ C", nebulaColC);
        nebulaMaxBright = EditorGUILayout.Slider("ความสว่างสูงสุด", nebulaMaxBright, 0.05f, 0.8f);
        nebulaBaseCells = EditorGUILayout.IntSlider("ขนาดก้อนเมฆ (ต่ำ=ใหญ่)", nebulaBaseCells, 1, 8);
        nebulaOctaves = EditorGUILayout.IntSlider("รายละเอียด (octaves)", nebulaOctaves, 3, 7);
        nebulaCoverage = EditorGUILayout.Slider("ปริมาณเมฆ (ต่ำ=เยอะ)", nebulaCoverage, 0.3f, 0.8f);
        nebulaWarp = EditorGUILayout.Slider("ความบิดเป็นริ้ว", nebulaWarp, 0f, 1.5f);
        nebulaDust = EditorGUILayout.Slider("เส้นฝุ่นมืด", nebulaDust, 0f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stars (ชั้นโปร่งใส)", EditorStyles.boldLabel);
        starSize = EditorGUILayout.IntPopup("ขนาดภาพ", starSize, new[] { "1024", "2048", "4096" }, new[] { 1024, 2048, 4096 });
        starTileUnits = EditorGUILayout.FloatField("ขนาดต่อชิ้น (หน่วยโลก)", starTileUnits);

        EditorGUILayout.LabelField("ดาวไกล");
        farCount = EditorGUILayout.IntField("  จำนวน", farCount);
        farBrightPower = EditorGUILayout.Slider("  ความหรี่ (สูง=หรี่เยอะ)", farBrightPower, 1f, 6f);
        farSigma = EditorGUILayout.Vector2Field("  ขนาดดาว (px) เล็ก/ใหญ่", farSigma);

        EditorGUILayout.LabelField("ดาวใกล้");
        nearCount = EditorGUILayout.IntField("  จำนวน", nearCount);
        nearBrightPower = EditorGUILayout.Slider("  ความหรี่ (สูง=หรี่เยอะ)", nearBrightPower, 1f, 6f);
        nearSigma = EditorGUILayout.Vector2Field("  ขนาดดาว (px) เล็ก/ใหญ่", nearSigma);
        nearGlowChance = EditorGUILayout.Slider("  โอกาสมี glow", nearGlowChance, 0f, 1f);
        nearSpikeChance = EditorGUILayout.Slider("  โอกาสมีประกาย", nearSpikeChance, 0f, 1f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate All", GUILayout.Height(28))) { GenNebula(); GenStars(true); GenStars(false); }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Nebula")) GenNebula();
            if (GUILayout.Button("Stars Far")) GenStars(true);
            if (GUILayout.Button("Stars Near")) GenStars(false);
        }
    }

    // ================= Nebula =================

    void GenNebula()
    {
        var cols = new[] { nebulaColA, nebulaColB, nebulaColC };
        Color32[] px = BuildNebula(nebulaSize, seed, nebulaBg, cols, nebulaMaxBright,
                                   nebulaBaseCells, nebulaOctaves, nebulaCoverage, nebulaWarp, nebulaDust);
        Save(px, nebulaSize, "Space_Nebula", nebulaSize / Mathf.Max(1f, nebulaTileUnits), false);
    }

    static Color32[] BuildNebula(int size, int seed, Color bg, Color[] cols, float maxBright,
                                 int baseCells, int octaves, float coverage, float warp, float dust)
    {
        var px = new Color32[size * size];
        float inv = 1f / size;
        int colorOctaves = Mathf.Max(3, octaves - 1);

        Parallel.For(0, size, y =>
        {
            for (int x = 0; x < size; x++)
            {
                float u = x * inv, v = y * inv;

                // บิดพิกัดให้เป็นริ้วเมฆ (ยังต่อขอบได้เพราะทุก noise เป็น periodic)
                float wu = u + warp * 0.25f * Fbm(u, v, 3, 3, seed + 11);
                float wv = v + warp * 0.25f * Fbm(u, v, 3, 3, seed + 23);

                float d0 = Fbm(wu, wv, baseCells, octaves, seed) * 0.5f + 0.5f;
                float main = Sm(0.30f, 0.70f, d0);   // เมฆรวมเป็นกลุ่ม ไม่กระจายทั่ว

                float r = 0f, g = 0f, b = 0f;
                for (int k = 0; k < cols.Length; k++)
                {
                    float f = Fbm(wu, wv, baseCells + k, colorOctaves, seed + 100 * (k + 1)) * 0.5f + 0.5f;
                    float s = Sm(coverage - 0.12f, coverage + 0.28f, f);
                    s *= Mathf.Sqrt(s);
                    r += cols[k].r * s;
                    g += cols[k].g * s;
                    b += cols[k].b * s;
                }

                // เส้นฝุ่นมืด: ตรงจุดที่ noise ข้ามศูนย์ = เส้นบางๆ ที่คดเคี้ยว
                float lane = Mathf.Abs(Fbm(wu, wv, baseCells + 1, 3, seed + 777));
                float dark = 1f - dust * (1f - Sm(0f, 0.22f, lane));

                float m = (0.2f + 0.8f * main) * dark * maxBright / 1.3f;

                // dither เล็กน้อย กันแถบสี (banding) ในส่วนมืด
                float dth = ((Hash(x, y, 999) & 255u) / 255f - 0.5f) / 255f;

                px[y * size + x] = new Color32(
                    ToByte(bg.r + r * m + dth),
                    ToByte(bg.g + g * m + dth),
                    ToByte(bg.b + b * m + dth),
                    255);
            }
        });
        return px;
    }

    // ================= Stars =================

    void GenStars(bool far)
    {
        int count = far ? farCount : nearCount;
        float power = far ? farBrightPower : nearBrightPower;
        Vector2 sigma = far ? farSigma : nearSigma;
        float glow = far ? 0f : nearGlowChance;
        float spike = far ? 0f : nearSpikeChance;

        Color32[] px = BuildStars(starSize, count, power, sigma, glow, spike, new System.Random(seed + (far ? 1 : 2)));
        Save(px, starSize, far ? "Space_StarsFar" : "Space_StarsNear", starSize / Mathf.Max(1f, starTileUnits), true);
    }

    static Color32[] BuildStars(int size, int count, float power, Vector2 sigma,
                                float glowChance, float spikeChance, System.Random rng)
    {
        var acc = new float[size * size * 3];

        for (int i = 0; i < count; i++)
        {
            float cx = (float)rng.NextDouble() * size;
            float cy = (float)rng.NextDouble() * size;
            float b = Mathf.Pow((float)rng.NextDouble(), power);
            float intensity = 0.25f + 0.75f * b;
            float sg = Mathf.Lerp(sigma.x, sigma.y, b);

            // สีดาว: ส่วนใหญ่ขาวนวล บางดวงฟ้า/ส้ม
            float t = (float)rng.NextDouble();
            Color tint = t < 0.70f ? new Color(1f, 0.97f, 0.92f)
                       : t < 0.85f ? new Color(0.70f, 0.82f, 1f)
                                   : new Color(1f, 0.78f, 0.58f);

            Splat(acc, size, cx, cy, sg, intensity, tint);

            if (b > 0.8f && rng.NextDouble() < glowChance)
                Splat(acc, size, cx, cy, sg * 4f, 0.12f * intensity, tint);

            if (b > 0.9f && rng.NextDouble() < spikeChance)
            {
                int len = Mathf.RoundToInt(sg * 14f);
                for (int k = 1; k <= len; k++)
                {
                    float val = intensity * 0.55f * Mathf.Exp(-k / (len * 0.35f));
                    Plot(acc, size, Mathf.RoundToInt(cx) + k, Mathf.RoundToInt(cy), tint, val);
                    Plot(acc, size, Mathf.RoundToInt(cx) - k, Mathf.RoundToInt(cy), tint, val);
                    Plot(acc, size, Mathf.RoundToInt(cx), Mathf.RoundToInt(cy) + k, tint, val);
                    Plot(acc, size, Mathf.RoundToInt(cx), Mathf.RoundToInt(cy) - k, tint, val);
                }
            }
        }

        // แปลงเป็นภาพ straight alpha: alpha = ความสว่างสูงสุด, สี = ปรับให้ช่องสูงสุด = 1
        var px = new Color32[size * size];
        for (int p = 0; p < size * size; p++)
        {
            float r = acc[p * 3], g = acc[p * 3 + 1], bl = acc[p * 3 + 2];
            float m = Mathf.Max(r, Mathf.Max(g, bl));
            if (m <= 0.002f) { px[p] = new Color32(0, 0, 0, 0); continue; }
            px[p] = new Color32(ToByte(r / m), ToByte(g / m), ToByte(bl / m), ToByte(m));
        }
        return px;
    }

    static void Splat(float[] acc, int size, float cx, float cy, float sigma, float intensity, Color c)
    {
        int rad = Mathf.CeilToInt(sigma * 3f);
        int ix = Mathf.RoundToInt(cx), iy = Mathf.RoundToInt(cy);
        float k = 1f / (2f * sigma * sigma);
        for (int dy = -rad; dy <= rad; dy++)
            for (int dx = -rad; dx <= rad; dx++)
            {
                float w = Mathf.Exp(-(dx * dx + dy * dy) * k);
                if (w < 0.01f) continue;
                Plot(acc, size, ix + dx, iy + dy, c, intensity * w);
            }
    }

    static void Plot(float[] acc, int size, int x, int y, Color c, float v)
    {
        x = Mod(x, size); y = Mod(y, size);   // วนขอบ: ดาวที่คร่อมขอบไปโผล่อีกฝั่ง = ต่อขอบได้
        int i = (y * size + x) * 3;
        acc[i] += c.r * v;
        acc[i + 1] += c.g * v;
        acc[i + 2] += c.b * v;
    }

    // ================= Noise (periodic Perlin) =================

    static readonly float[] GX = new float[256];
    static readonly float[] GY = new float[256];

    static SpaceLayerGenerator()
    {
        for (int i = 0; i < 256; i++)
        {
            float a = i * (Mathf.PI * 2f / 256f);
            GX[i] = Mathf.Cos(a);
            GY[i] = Mathf.Sin(a);
        }
    }

    static uint Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return h;
        }
    }

    static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

    static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    static float Sm(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float Grad(uint h, float dx, float dy)
    {
        int i = (int)(h & 255u);
        return GX[i] * dx + GY[i] * dy;
    }

    // noise ต่อขอบได้: lattice วนทุก 'period' ช่อง
    static float PNoise(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        int xa = Mod(x0, period), xb = Mod(x0 + 1, period);
        int ya = Mod(y0, period), yb = Mod(y0 + 1, period);

        float n00 = Grad(Hash(xa, ya, seed), fx, fy);
        float n10 = Grad(Hash(xb, ya, seed), fx - 1f, fy);
        float n01 = Grad(Hash(xa, yb, seed), fx, fy - 1f);
        float n11 = Grad(Hash(xb, yb, seed), fx - 1f, fy - 1f);

        float u = Fade(fx), v = Fade(fy);
        return Mathf.Lerp(Mathf.Lerp(n00, n10, u), Mathf.Lerp(n01, n11, u), v);
    }

    // u,v ในช่วง 0..1 = หนึ่งรอบภาพ  คืนค่าประมาณ -1..1
    static float Fbm(float u, float v, int baseCells, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, norm = 0f;
        int cells = baseCells;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * PNoise(u * cells, v * cells, cells, seed + o * 131);
            norm += amp;
            amp *= 0.5f;
            cells *= 2;
        }
        return Mathf.Clamp(sum / norm * 1.6f, -1f, 1f);
    }

    static byte ToByte(float f) => (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);

    // ================= บันทึก + ตั้งค่า import =================

    void Save(Color32[] px, int size, string name, float ppu, bool alpha)
    {
        Directory.CreateDirectory(folder);
        string path = folder + "/" + name + ".png";

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = alpha;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);

            ti.SaveAndReimport();
        }

        Debug.Log($"SpaceLayerGenerator: สร้าง {path} ({size}px, PPU {ppu:0.0})");
    }
}