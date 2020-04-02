using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace StarCiv
{
    public struct PaletteElement
    {
        public float pos;
        public int r, g, b;
        public PaletteElement(float _pos, int _r, int _g, int _b)
        {
            pos = _pos;
            r = _r;
            g = _g;
            b = _b;
        }
    }

    public enum OutputFormats
    {
        Flat,
        Tileable,
        SphereMapped
    };

    public struct PerlinVarsStruct
    {
        public float freq;
        public float amp;
        public float persistance;
        public int octaves;
        public int randomSeed;
        public OutputFormats format;
        public bool flat;
        public bool tileable;
        public bool sphereMapped;

        public delegate float INTERPOLATE_FUNC(float x, float y, float z);
        public delegate float CLAMP_FUNC(float x);
    };

    public struct TilableData
    {
        public float cur_freq;
        public int width;
    };

    public struct ColorPt
    {
        public float pos;
        public float r, g, b;
    };

    public struct ColorPaletteStruct
    {
        public ColorPt[] pts;
        public int num_pts;
    };

    public class Perlin
    {
        ColorPaletteStruct ColorPalette;
        public PerlinVarsStruct PerlinVars;
        TilableData TileData;

        Color[] colorArray;// = new Color[256 * 256];
        int width = 0;
        int height = 0;

        PerlinVarsStruct.INTERPOLATE_FUNC InterpolateFn;
        PerlinVarsStruct.CLAMP_FUNC ClampFn;

        public Perlin(int Width, int Height, float init_freq, float init_amp, float persistance, int octaves, int random_seed)
        {
            width = Width;
            height = Height;
            colorArray = new Color[width * height];
            InterpolateFn = new PerlinVarsStruct.INTERPOLATE_FUNC(CosineInterpolate);
            ClampFn = new PerlinVarsStruct.CLAMP_FUNC(AbsBoundNoise);

            ColorPalette.pts = new ColorPt[100];

            PerlinVars.freq = init_freq;
            PerlinVars.amp = init_amp;
            PerlinVars.persistance = persistance;
            PerlinVars.octaves = octaves;
            PerlinVars.randomSeed = random_seed;
        }

        public Color[] GetNoise()
        {
            return colorArray;
        }

        //public void SetPerliVars(float init_freq, float init_amp, float persistance, int octaves, int random_seed)
        //{
        //    PerlinVars.freq = init_freq;
        //    PerlinVars.amp = init_amp;
        //    PerlinVars.persistance = persistance;
        //    PerlinVars.octaves = octaves;
        //    PerlinVars.randomSeed = random_seed;
        //}

        public Color RGBA(byte r, byte g, byte b, byte a)
        {
            return new Color(r, g, b, a);
        }
        public Color PixelAt(int x, int y, int w)
        {
            return colorArray[x + y * w];
        }
        public void Pset(int x, int y, int w, byte a, byte r, byte g, byte b)
        {
            colorArray[x + y * w] = RGBA(r, g, b, a);
        }
        public void Pset(int x, int y, int w, Color color)
        {
            colorArray[x + y * w] = color;
        }

        public void GeneratePerlinTexture(int w, int h)
        {
            float p;

            if (PerlinVars.format == OutputFormats.SphereMapped)
            {
                GenerateSphereMappedTexture(w, h);
                return;
            }

            TileData.width = w;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    p = PerlinNoise((float)x, (float)y);
                    p = ClampFn(p);
                    Pset(x, y, w, GetColor(p));
                }
            }
        }

        public void GenerateSphereMappedTexture(int w, int h)
        {
            float p;

            float u, v, r, s, a, b, c;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    u = (float)x / (float)w;
                    v = (float)y / (float)h;

                    r = u * 2.0f * 3.14156f;
                    s = (v - 0.5f) * 3.14156f;

                    a = (float)Math.Cos(r) * (float)Math.Cos(s);
                    b = (float)Math.Sin(r) * (float)Math.Cos(s);
                    c = (float)Math.Sin(s);

                    a += 1.0f;
                    b += 1.0f;
                    c += 1.0f;

                    a *= w;
                    b *= w;
                    c *= w;

                    p = PerlinNoise(a, b, c);
                    p = ClampFn(p);
                    Pset(x, y, w, GetColor(p));
                }
            }
        }

        public float Noise(int x)
        {
            x += PerlinVars.randomSeed;
            x = (x << 13) ^ x;
            return (1.0f - ((x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
        }
        public float Noise(int x, int y)
        {
            if (PerlinVars.format == OutputFormats.Tileable)
            {
                // round f to nearest int s
                float f = TileData.cur_freq * TileData.width;
                int s = (int)f;
                if (f - s > .5f) s++;

                if (s > 0)
                {
                    x %= s;
                    y %= s;
                }
                if (x < 0) x += s;
                if (y < 0) y += s;
            }

            return Noise(x + y * 8997587);//57);
        }
        float Noise(int x, int y, int z)
        {
            return Noise(x + (y * 89213) + (z * 8997587));
        }

        public float PerlinNoise(float x, float y)
        {
            float total = 0;

            float freq = PerlinVars.freq;
            float amp = PerlinVars.amp;

            for (int i = 0; i < PerlinVars.octaves; i++)
            {
                TileData.cur_freq = freq;
                total += InterpolatedNoise(x * freq, y * freq) * amp;
                freq *= 2;
                amp *= PerlinVars.persistance;
            }

            return total;
        }
        public float PerlinNoise(float x, float y, float z)
        {
            float total = 0;

            float freq = PerlinVars.freq;
            float amp = PerlinVars.amp;

            for (int i = 0; i < PerlinVars.octaves; i++)
            {
                total += InterpolatedNoise(x * freq, y * freq, z * freq) * amp;
                freq *= 2;
                amp *= PerlinVars.persistance;
            }

            return total;
        }

        float InterpolatedNoise(float x, float y)
        {
            int a = (int)x;
            int b = (int)y;
            float frac_a = x - a;
            float frac_b = y - b;

            float v1 = Noise(a, b);
            float v2 = Noise(a + 1, b);
            float v3 = Noise(a, b + 1);
            float v4 = Noise(a + 1, b + 1);

            float i1 = InterpolateFn(v1, v2, frac_a);
            float i2 = InterpolateFn(v3, v4, frac_a);

            return InterpolateFn(i1, i2, frac_b);
        }

        public float InterpolatedNoise(float x, float y, float z)
        {
            int a = (int)x;
            int b = (int)y;
            int c = (int)z;

            float frac_a = x - a;
            float frac_b = y - b;
            float frac_c = z - c;

            float v1 = Noise(a, b, c);
            float v2 = Noise(a + 1, b, c);
            float v3 = Noise(a, b + 1, c);
            float v4 = Noise(a + 1, b + 1, c);

            float i1 = InterpolateFn(v1, v2, frac_a);
            float i2 = InterpolateFn(v3, v4, frac_a);
            float i3 = InterpolateFn(i1, i2, frac_b);

            float v5 = Noise(a, b, c + 1);
            float v6 = Noise(a + 1, b, c + 1);
            float v7 = Noise(a, b + 1, c + 1);
            float v8 = Noise(a + 1, b + 1, c + 1);

            float j1 = InterpolateFn(v5, v6, frac_a);
            float j2 = InterpolateFn(v7, v8, frac_a);
            float j3 = InterpolateFn(j1, j2, frac_b);

            return InterpolateFn(i3, j3, frac_c);
        }

        //public void InitializePalette(int r1, int g1, int b1, int r2, int g2, int b2)
        //{
        //    ColorPalette.pts[0].pos = 0.0f;
        //    ColorPalette.pts[0].r = (float)r1;
        //    ColorPalette.pts[0].g = (float)g1;
        //    ColorPalette.pts[0].b = (float)b1;
        //    ColorPalette.pts[1].pos = 1.0f;
        //    ColorPalette.pts[1].r = (float)r2;
        //    ColorPalette.pts[1].g = (float)g2;
        //    ColorPalette.pts[1].b = (float)b2;
        //    ColorPalette.num_pts = 2;
        //}

        public void AddPalette(float pos, int r, int g, int b)
        {
            if (ColorPalette.num_pts + 2 >= 100) return;
            if (pos < 0 || pos > 1) return;

            //for (int i = 0; i < ColorPalette.num_pts; i++)
            {
                //if (pos > ColorPalette.pts[i].pos && pos <= ColorPalette.pts[i + 1].pos)
                {
                    ColorPalette.pts[ColorPalette.num_pts].pos = pos;
                    ColorPalette.pts[ColorPalette.num_pts].r = (float)r;
                    ColorPalette.pts[ColorPalette.num_pts].g = (float)g;
                    ColorPalette.pts[ColorPalette.num_pts].b = (float)b;
                    //ColorPalette.num_pts++;
                }
            }
            ColorPalette.num_pts++;
        }

        public Color GetColor(float f)
        {
            float d, s;
            int r, g, b;

            if (f <= 0) return RGBA((byte)ColorPalette.pts[0].r, (byte)ColorPalette.pts[0].g, (byte)ColorPalette.pts[0].b, (byte)255);
            if (f >= 1) return RGBA((byte)ColorPalette.pts[ColorPalette.num_pts - 1].r, (byte)ColorPalette.pts[ColorPalette.num_pts - 1].g, (byte)ColorPalette.pts[ColorPalette.num_pts - 1].b, (byte)255);

            for (int i = 0; i < ColorPalette.num_pts; i++)
            {
                if (f >= ColorPalette.pts[i].pos && f < ColorPalette.pts[i + 1].pos)
                {
                    d = ColorPalette.pts[i + 1].pos - ColorPalette.pts[i].pos;
                    s = (f - ColorPalette.pts[i].pos) / d;
                    r = (int)((ColorPalette.pts[i].r * (1.0f - s)) + (ColorPalette.pts[i + 1].r * s));
                    g = (int)((ColorPalette.pts[i].g * (1.0f - s)) + (ColorPalette.pts[i + 1].g * s));
                    b = (int)((ColorPalette.pts[i].b * (1.0f - s)) + (ColorPalette.pts[i + 1].b * s));
                    return RGBA((byte)r, (byte)g, (byte)b, (byte)255);
                }
            }

            return RGBA((byte)0, (byte)0, (byte)0, (byte)255);
        }

        #region HelperFunctions
        // Helper Functions - Interpolations
        public float LinearInterpolate(float a, float b, float x)
        {
            return a * (1 - x) + b * x;
        }
        public float CosineInterpolate(float a, float b, float x)
        {
            float ft = x * 3.1415927f;
            float f = (float)((1 - Math.Cos(ft)) * 0.5f);
            return a * (1 - f) + b * f;
        }
        public float CubicInterpolate(float a, float b, float x)
        {
            float fac1 = (float)(3 * Math.Pow(1 - x, 2) - 2 * Math.Pow(1 - x, 3));
            float fac2 = (float)(3 * Math.Pow(x, 2) - 2 * Math.Pow(x, 3));

            return a * fac1 + b * fac2; //add the weighted factors
        }
        // Helper Functions - BoundNoise
        public float AbsBoundNoise(float n)
        {
            if (n < 0.0f) n = -n;
            if (n > 1.0f) n = 1.0f;
            return n;
        }
        public float TruncateBoundNoise(float n)
        {
            if (n < 0.0f) n = 0.0f;
            if (n > 1.0f) n = 1.0f;
            return n;
        }
        public float NormalizedTruncateBoundNoise(float n)
        {
            if (n < -1.0f) n = -1.0f;
            if (n > 1.0f) n = 1.0f;
            return (n * .5f) + .5f;
        }
        #endregion
    }
}
