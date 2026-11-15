// Pioneer Base 3D - PioneerGame.World.cs
// The outpost and its planet, generated from code: materials, terrain, sky, modules.
// What you see mirrors the simulation state (crates, plants, dust, lights, shield walls).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PioneerBase;

public partial class PioneerGame
{
    // ------------------------------------------------------------------ materials
    Shader shLit, shGlass, shUnlit, shSprite, shAdd, shBlend;
    Texture2D glowTex, softTex;
    readonly Dictionary<int, Material> litCache = new Dictionary<int, Material>();
    readonly Dictionary<PrimitiveType, Mesh> primMesh = new Dictionary<PrimitiveType, Mesh>();

    static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

    void InitMaterials()
    {
        shSprite = Shader.Find("Sprites/Default");
        shLit = Shader.Find("Legacy Shaders/Diffuse") ?? shSprite;
        shGlass = Shader.Find("Legacy Shaders/Transparent/Diffuse") ?? shSprite;
        shUnlit = Shader.Find("Unlit/Color") ?? shSprite;
        shAdd = Shader.Find("Legacy Shaders/Particles/Additive") ?? shSprite;
        shBlend = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? shSprite;
        glowTex = RadialTex(64, 2f); softTex = RadialTex(64, 1f);
    }
    Texture2D RadialTex(int n, float pow)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), pow)));
        }
        t.Apply(); return t;
    }
    Material Lit(Color c)
    {
        var c32 = (Color32)c; int key = c32.r | (c32.g << 8) | (c32.b << 16);
        Material m; if (litCache.TryGetValue(key, out m)) return m;
        m = new Material(shLit); m.color = c; litCache[key] = m; return m;
    }
    Material Lit(float r, float g, float b) { return Lit(new Color(r, g, b)); }
    Material Unlit(Color c) { var m = new Material(shUnlit); m.color = c; return m; }
    Material Glass(Color c) { var m = new Material(shGlass); m.color = c; return m; }
    Material SpriteMat(Texture tex, Color c, int queue = -1)
    {
        var m = new Material(shSprite); m.mainTexture = tex; m.color = c; if (queue >= 0) m.renderQueue = queue; return m;
    }

    GameObject P(PrimitiveType type, Transform parent, Vector3 lpos, Vector3 scale, Material mat, Vector3? euler = null, bool shadow = true)
    {
        // one template per shape; every other object reuses its mesh (no colliders are created)
        Mesh mesh;
        if (!primMesh.TryGetValue(type, out mesh))
        {
            var tmp = GameObject.CreatePrimitive(type); mesh = tmp.GetComponent<MeshFilter>().sharedMesh; primMesh[type] = mesh; Destroy(tmp);
        }
        var go = new GameObject(type.ToString());
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.transform.SetParent(parent, false); go.transform.localPosition = lpos; go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        if (!shadow) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
        return go;
    }
    Transform Empty(string name, Transform parent, Vector3 lpos)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = lpos; return t;
    }

    // ------------------------------------------------------------------ world data
    class Mod
    {
        public string id, station, title; public Transform root; public Vector3 pos, ipos; public float radius, height;
        public List<Material> lamps = new List<Material>(); public List<Color> lampCols = new List<Color>();
        public List<Transform> spin = new List<Transform>(); public List<Vector3> spinAxis = new List<Vector3>();
        public TextMesh holo; public bool on = true, broken, noPower, solid = true; public float pop = 1f, smokeT;
        public Transform bob; public Vector3 bobBase; public List<Transform> plants = new List<Transform>(); public string state = "";
    }
    class Slot { public string type; public int index; public Vector3 pos; public float radius; public GameObject built, scaffold, marker; public TextMesh label; public Mod mod; public int state = -1; public float pop = 1f; public Transform pivot; public Material panel; public Transform bar; public Material barMat; }

    Transform siteRoot;
    readonly List<Mod> mods = new List<Mod>();
    readonly Dictionary<Collider, Mod> modByCol = new Dictionary<Collider, Mod>();
    readonly List<Slot> slots = new List<Slot>();
    readonly List<Transform> billboards = new List<Transform>();
    readonly List<GameObject> shieldRings = new List<GameObject>();
    readonly List<GameObject> crates = new List<GameObject>();
    Mod habMod, oxyMod, labMod, depotMod, landerMod, solarMod, batteryMod, reactorMod;
    Collider terrainCol; GameObject shelterBubble, reliefShip; Material shelterMat;
    Light habLight; bool initialBuild; Transform objMarker;
    float seedX, seedZ; readonly List<Vector4> craterList = new List<Vector4>();

    static readonly Vector3 HabPos = new Vector3(0, 0, 0), OxyPos = new Vector3(-13, 0, -1), LabPos = new Vector3(13, 0, -1),
        DepotPos = new Vector3(10, 0, -15), LanderPos = new Vector3(-2, 0, -32), ReactorPos = new Vector3(31, 0, -15),
        SolarCenter = new Vector3(-19, 0, -15), BatteryPos = new Vector3(7.5f, 0, -7), RoverPos = new Vector3(19, 0, -9);
    static readonly Vector3[] GhPos = { new Vector3(-14, 0, 11), new Vector3(-6, 0, 16.5f), new Vector3(3, 0, 18) };
    static readonly Vector3[] DrillPos = { new Vector3(14, 0, 11), new Vector3(21, 0, 15), new Vector3(25, 0, 7) };

    // ------------------------------------------------------------------ terrain
    static float Fbm(float x, float z)
    {
        float s = 0, a = 0.5f, f = 1f;
        for (int i = 0; i < 4; i++) { s += a * Mathf.PerlinNoise(x * f, z * f); a *= 0.5f; f *= 2.03f; }
        return s / 0.9375f;
    }
    float H(float x, float z)
    {
        float d = Mathf.Sqrt(x * x + z * z);
        float mask = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(42, 88, d));
        float far = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(100, 165, d));
        float n = Fbm(x * 0.017f + seedX, z * 0.017f + seedZ);
        float h;
        if (site == "mars")
        {
            h = (n - 0.42f) * 15f * mask + Mathf.Sin(x * 0.08f + n * 7f) * 0.7f * mask
                + far * (6f + 30f * Fbm(x * 0.028f + seedZ, z * 0.028f + seedX));
        }
        else
        {
            h = (n - 0.48f) * 7f * mask + far * (3f + 13f * Fbm(x * 0.03f + seedZ, z * 0.03f + seedX));
            for (int i = 0; i < craterList.Count; i++)
            {
                var c = craterList[i]; float cd = Mathf.Sqrt((x - c.x) * (x - c.x) + (z - c.y) * (z - c.y)), r = c.z;
                if (cd < r * 1.8f)
                {
                    if (cd < r) h -= c.w * (1f - (cd / r) * (cd / r));
                    float rim = (cd - r) / (r * 0.28f); h += c.w * 0.38f * Mathf.Exp(-rim * rim);
                }
            }
        }
        float micro = (Mathf.PerlinNoise(x * 0.45f + 3f, z * 0.45f + 9f) - 0.5f) * 0.22f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(30, 46, d));
        return h + micro;
    }
    Vector3 Ground(Vector3 p) { return new Vector3(p.x, H(p.x, p.z), p.z); }

    void BuildTerrain()
    {
        const int N = 150; const float size = 380f;
        var verts = new Vector3[(N + 1) * (N + 1)]; var uv = new Vector2[verts.Length]; var tris = new int[N * N * 6];
        for (int j = 0; j <= N; j++) for (int i = 0; i <= N; i++)
        {
            float x = (i / (float)N - 0.5f) * size, z = (j / (float)N - 0.5f) * size;
            verts[j * (N + 1) + i] = new Vector3(x, H(x, z), z); uv[j * (N + 1) + i] = new Vector2(x / 16f, z / 16f);
        }
        int t = 0;
        for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
        {
            int a = j * (N + 1) + i, b = a + 1, c = a + N + 1, d = c + 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b; tris[t++] = b; tris[t++] = c; tris[t++] = d;
        }
        var mesh = new Mesh(); mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject("Terrain"); go.transform.SetParent(siteRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(shLit); mat.mainTexture = GroundTex(); mat.color = Color.white; mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; terrainCol = mc;

        // rocks
        var rnd = new System.Random(site == "mars" ? 11 : 23);
        Color rc = site == "mars" ? C(0.42f, 0.22f, 0.14f) : C(0.38f, 0.38f, 0.4f);
        for (int i = 0; i < 110; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f, dist = 16f + (float)rnd.NextDouble() * 130f;
            var p = new Vector3(Mathf.Cos(ang) * dist, 0, Mathf.Sin(ang) * dist);
            if (NearAnyPad(p, 4f)) continue;
            float s = 0.3f + (float)rnd.NextDouble() * (dist > 45 ? 2.6f : 0.7f);
            var col = Color.Lerp(rc, rc * 1.5f, (float)rnd.NextDouble());
            var rock = P(rnd.Next(2) == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube, siteRoot, Ground(p) + Vector3.up * s * 0.2f,
                new Vector3(s * (0.7f + (float)rnd.NextDouble()), s * (0.5f + (float)rnd.NextDouble() * 0.6f), s * (0.7f + (float)rnd.NextDouble())), Lit(col),
                new Vector3(rnd.Next(40), rnd.Next(360), rnd.Next(40)));
            rock.name = "Rock";
        }
    }
    bool NearAnyPad(Vector3 p, float margin)
    {
        Func<Vector3, float, bool> near = (c, r) => (new Vector2(p.x - c.x, p.z - c.z)).magnitude < r + margin;
        if (near(HabPos, 7) || near(OxyPos, 3) || near(LabPos, 3.5f) || near(DepotPos, 3.5f) || near(LanderPos, 7) || near(ReactorPos, 3) || near(SolarCenter, 10) || near(BatteryPos, 4) || near(RoverPos, 3) || near(new Vector3(15, 0, -36), 6)) return true;
        foreach (var g in GhPos) if (near(g, 3.5f)) return true;
        foreach (var g in DrillPos) if (near(g, 3f)) return true;
        return false;
    }
    Texture2D GroundTex()
    {
        const int n = 256; var t = new Texture2D(n, n, TextureFormat.RGB24, true); t.wrapMode = TextureWrapMode.Mirror;
        Color a = site == "mars" ? C(0.66f, 0.36f, 0.22f) : C(0.50f, 0.50f, 0.52f), b = site == "mars" ? C(0.48f, 0.24f, 0.15f) : C(0.33f, 0.33f, 0.36f);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float k = Fbm(x * 0.035f + 5f, y * 0.035f + 2f); float g = Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.18f;
            px[y * n + x] = Color.Lerp(b, a, Mathf.Clamp01(k * 1.3f - 0.1f)) * (0.9f + g);
        }
        t.SetPixels(px); t.Apply(true); return t;
    }

    // ------------------------------------------------------------------ sky
    Transform skyRoot, sunQuad, sunCore, earthBall, starRoot; Material skyMat, sunMat, sunCoreMat, starMat, earthGlowMat;
    Texture2D skyTex; Light sunLight, earthLight; Color[] skyPx = new Color[64];
    float dayT = 0.34f, moonDayF = 1f, stormK, solarK, sunElev, sunAz, dayA = 1f;
    Vector3 sunDir = Vector3.up;

    void BuildSky()
    {
        skyRoot = new GameObject("Sky").transform; skyRoot.SetParent(world, false);
        skyTex = new Texture2D(1, 64, TextureFormat.RGB24, false); skyTex.wrapMode = TextureWrapMode.Clamp;
        skyMat = SpriteMat(skyTex, Color.white, 1000);
        var dome = P(PrimitiveType.Sphere, skyRoot, Vector3.zero, Vector3.one * 1900f, skyMat, null, false); dome.name = "Dome";

        // stars: one mesh of small quads on a sphere
        var rnd = new System.Random(5); int count = 650;
        var v = new Vector3[count * 4]; var uv = new Vector2[count * 4]; var col = new Color[count * 4]; var tr = new int[count * 6];
        for (int i = 0; i < count; i++)
        {
            Vector3 d = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 1.3f - 0.3f, (float)rnd.NextDouble() * 2 - 1).normalized;
            Vector3 right = Vector3.Cross(d, Vector3.up).normalized, up = Vector3.Cross(right, d);
            float s = 1.6f + (float)rnd.NextDouble() * 3.2f; Vector3 c = d * 900f; float br = 0.4f + (float)rnd.NextDouble() * 0.6f;
            v[i * 4] = c - right * s - up * s; v[i * 4 + 1] = c + right * s - up * s; v[i * 4 + 2] = c + right * s + up * s; v[i * 4 + 3] = c - right * s + up * s;
            uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(1, 0); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(0, 1);
            var cc = Color.Lerp(new Color(0.8f, 0.9f, 1f), new Color(1f, 0.9f, 0.8f), (float)rnd.NextDouble()); cc.a = br;
            for (int k = 0; k < 4; k++) col[i * 4 + k] = cc;
            tr[i * 6] = i * 4; tr[i * 6 + 1] = i * 4 + 2; tr[i * 6 + 2] = i * 4 + 1; tr[i * 6 + 3] = i * 4; tr[i * 6 + 4] = i * 4 + 3; tr[i * 6 + 5] = i * 4 + 2;
        }
        var sm = new Mesh(); sm.vertices = v; sm.uv = uv; sm.colors = col; sm.triangles = tr; sm.bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
        var sg = new GameObject("Stars"); sg.transform.SetParent(skyRoot, false); starRoot = sg.transform;
        sg.AddComponent<MeshFilter>().sharedMesh = sm; starMat = SpriteMat(glowTex, Color.white, 1001);
        var smr = sg.AddComponent<MeshRenderer>(); smr.sharedMaterial = starMat; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        sunMat = SpriteMat(softTex, Color.white, 1002); sunCoreMat = SpriteMat(glowTex, Color.white, 1003);
        sunQuad = P(PrimitiveType.Quad, skyRoot, Vector3.zero, Vector3.one * 420f, sunMat, null, false).transform;
        sunCore = P(PrimitiveType.Quad, skyRoot, Vector3.zero, Vector3.one * 70f, sunCoreMat, null, false).transform;

        // Earth, seen from the Moon
        var et = new Texture2D(128, 64, TextureFormat.RGB24, false);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
        {
            float land = Fbm(x * 0.05f + 1f, y * 0.07f + 4f), cloud = Fbm(x * 0.09f + 9f, y * 0.12f + 2f);
            Color c = land > 0.55f ? Color.Lerp(C(0.2f, 0.5f, 0.25f), C(0.55f, 0.45f, 0.3f), (land - 0.55f) * 4f) : C(0.1f, 0.3f, 0.75f);
            if (cloud > 0.56f) c = Color.Lerp(c, Color.white, Mathf.Clamp01((cloud - 0.56f) * 6f));
            et.SetPixel(x, y, c);
        }
        et.Apply();
        earthBall = P(PrimitiveType.Sphere, skyRoot, Vector3.zero, Vector3.one * 62f, SpriteMat(et, Color.white, 1005), new Vector3(0, 30, 18), false).transform;
        earthGlowMat = SpriteMat(softTex, C(0.3f, 0.55f, 1f, 0.5f), 1004);
        var eg = P(PrimitiveType.Quad, earthBall, Vector3.zero, Vector3.one * 2.6f, earthGlowMat, null, false); eg.name = "EarthGlow"; billboards.Remove(eg.transform);

        var sl = new GameObject("Sun"); sl.transform.SetParent(world, false); sunLight = sl.AddComponent<Light>();
        sunLight.type = LightType.Directional; sunLight.shadows = LightShadows.Soft; sunLight.shadowStrength = 0.85f; sunLight.shadowBias = 0.08f; sunLight.shadowNormalBias = 0.5f;
        var el = new GameObject("Earthshine"); el.transform.SetParent(world, false); earthLight = el.AddComponent<Light>();
        earthLight.type = LightType.Directional; earthLight.shadows = LightShadows.None; earthLight.color = C(0.55f, 0.7f, 1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fogMode = FogMode.Linear;
    }

    static readonly Vector3 EarthDir = (Quaternion.Euler(-15f, 24f, 0) * Vector3.forward).normalized;

    void TickSky(float dt)
    {
        if (skyRoot == null) return;
        bool mars = site == "mars";
        if (mars) { sunElev = Mathf.Sin((dayT - 0.25f) * Mathf.PI * 2f) * 58f; sunAz = dayT * 360f; }
        else { float ph = (moonDayF - 25.5f) / 30f; sunElev = Mathf.Sin(ph * Mathf.PI * 2f) * 28f; sunAz = 90f + ph * 360f; }
        sunLight.transform.rotation = Quaternion.Euler(sunElev, sunAz, 0);
        sunDir = -sunLight.transform.forward;
        float a = Mathf.Clamp01((sunElev + 8f) / 20f); dayA = a;
        float sunK = Mathf.Clamp01(sunElev / 12f);

        // weather targets follow the simulation
        float wantStorm = 0, wantSolar = 0;
        if (st != null && !st.over && mode != Mode.Title)
        {
            var env = Sim.EnvFor(st, st.day);
            if (env.dustStorm) wantStorm = 1f;
            if (env.solarStorm) wantSolar = 1f;
        }
        if (debugStorm >= 0) wantStorm = debugStorm;
        stormK = Mathf.MoveTowards(stormK, wantStorm, dt * 0.6f); solarK = Mathf.MoveTowards(solarK, wantSolar, dt * 0.8f);
        float pulse = solarK * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));

        Color zen, hor, amb, sunCol; float sunI;
        if (mars)
        {
            Color dz = C(0.56f, 0.40f, 0.29f), dh = C(0.87f, 0.69f, 0.51f), nz = C(0.012f, 0.014f, 0.03f), nh = C(0.05f, 0.045f, 0.06f);
            Color kz = C(0.22f, 0.22f, 0.32f), kh = C(0.44f, 0.50f, 0.68f);
            float dusk = 1f - Mathf.Abs(2f * a - 1f);
            zen = Color.Lerp(Color.Lerp(nz, dz, a), kz, dusk * 0.8f); hor = Color.Lerp(Color.Lerp(nh, dh, a), kh, dusk * 0.85f);
            Color sc = C(0.52f, 0.33f, 0.2f) * (0.25f + 0.75f * a);
            zen = Color.Lerp(zen, sc * 0.8f, stormK * 0.9f); hor = Color.Lerp(hor, sc, stormK * 0.9f);
            amb = Color.Lerp(C(0.07f, 0.07f, 0.12f), C(0.50f, 0.40f, 0.33f), a) * (1f - 0.45f * stormK);
            sunCol = Color.Lerp(C(0.75f, 0.8f, 1f), C(1f, 0.93f, 0.82f), Mathf.Clamp01(sunElev / 25f));
            sunI = sunK * 1.15f * (1f - 0.8f * stormK);
            RenderSettings.fog = true; RenderSettings.fogColor = hor;
            RenderSettings.fogStartDistance = Mathf.Lerp(70f, 4f, stormK); RenderSettings.fogEndDistance = Mathf.Lerp(520f, 75f, stormK);
            starMat.color = new Color(1, 1, 1, (1f - a) * (1f - stormK));
            earthBall.gameObject.SetActive(false); earthLight.intensity = 0;
        }
        else
        {
            zen = C(0f, 0f, 0.004f); hor = C(0.012f, 0.012f, 0.02f);
            amb = Color.Lerp(C(0.05f, 0.06f, 0.09f), C(0.13f, 0.13f, 0.14f), sunK);
            sunCol = Color.white; sunI = sunK * 1.4f;
            RenderSettings.fog = false;
            starMat.color = new Color(1, 1, 1, 0.5f + 0.5f * (1f - sunK));
            earthBall.gameObject.SetActive(true); earthLight.intensity = 0.3f * (1f - sunK * 0.7f);
            earthLight.transform.rotation = Quaternion.LookRotation(-EarthDir);
        }
        zen += C(0.3f, 0.0f, 0.06f) * pulse; hor += C(0.45f, 0.05f, 0.05f) * pulse; amb += C(0.25f, 0.02f, 0.02f) * pulse;
        sunLight.color = sunCol; sunLight.intensity = sunI; sunLight.enabled = sunI > 0.01f;
        RenderSettings.ambientLight = amb;
        cam.backgroundColor = hor;

        for (int i = 0; i < 64; i++)
        {
            float vv = i / 63f;
            skyPx[i] = vv < 0.5f ? Color.Lerp(hor * 0.55f, hor, Mathf.Clamp01(vv * 2f)) : Color.Lerp(hor, zen, Mathf.Pow((vv - 0.5f) * 2f, 0.55f));
        }
        skyTex.SetPixels(skyPx); skyTex.Apply(false);

        Vector3 cp = cam.transform.position;
        skyRoot.position = cp;
        sunQuad.position = cp + sunDir * 880f; sunQuad.rotation = cam.transform.rotation;
        sunCore.position = cp + sunDir * 870f; sunCore.rotation = cam.transform.rotation;
        bool sunVis = sunElev > -4f;
        sunQuad.gameObject.SetActive(sunVis); sunCore.gameObject.SetActive(sunVis);
        Color halo = mars ? Color.Lerp(C(0.45f, 0.68f, 1f), C(1f, 0.9f, 0.72f), Mathf.Clamp01(sunElev / 22f)) : Color.white;
        halo.a = (mars ? 0.75f : 0.4f) * (1f - 0.7f * stormK); sunMat.color = halo;
        sunCoreMat.color = new Color(1f, 1f, 0.95f, 1f - 0.8f * stormK);
        earthBall.position = cp + EarthDir * 860f;
        var eg = earthBall.GetChild(0); eg.rotation = cam.transform.rotation;
    }

    // ------------------------------------------------------------------ site
    void BuildSite(string s)
    {
        site = s;
        if (siteRoot != null) Destroy(siteRoot.gameObject);
        mods.Clear(); modByCol.Clear(); slots.Clear(); billboards.Clear(); shieldRings.Clear(); crates.Clear(); crew.Clear(); prints.Clear();
        siteRoot = new GameObject("Site").transform; siteRoot.SetParent(world, false);
        var rnd = new System.Random(s == "mars" ? 4 : 9);
        seedX = (float)rnd.NextDouble() * 50f; seedZ = (float)rnd.NextDouble() * 50f;
        craterList.Clear();
        if (s == "moon")
            for (int i = 0; i < 26; i++)
            {
                float r = 4f + (float)rnd.NextDouble() * 16f, ang = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 48f + r + (float)rnd.NextDouble() * 110f;
                craterList.Add(new Vector4(Mathf.Cos(ang) * d, Mathf.Sin(ang) * d, r, 1.2f + r * 0.22f));
            }
        BuildTerrain();
        BuildHab(); BuildOxygen(); BuildLab(); BuildDepot(); BuildLander(); BuildRover();
        for (int i = 0; i < 3; i++) AddSlot("greenhouse", i, GhPos[i], 3.2f);
        for (int i = 0; i < 3; i++) AddSlot("drill", i, DrillPos[i], 2.6f);
        for (int i = 0; i < 6; i++) AddSlot("solar", i, SolarCenter + new Vector3((i % 3 - 1) * 5.4f, 0, (i / 3) * 6f - 3f), 2.2f);
        for (int i = 0; i < 4; i++) AddSlot("battery", i, BatteryPos + new Vector3(i * 1.7f, 0, 0), 0.9f);
        AddSlot("reactor", 0, ReactorPos, 2.6f);
        solarMod = NewMod("solar", "solar", "SOLAR FIELD", SolarCenter, 9f, 3.4f, false);
        batteryMod = NewMod("battery", "battery", "BATTERIES", BatteryPos + new Vector3(2.5f, 0, 0), 3.6f, 2f, false);
        Cable(OxyPos, HabPos); Cable(LabPos, HabPos); Cable(SolarCenter + new Vector3(0, 0, 4), new Vector3(-4, 0, -4)); Cable(ReactorPos, new Vector3(5, 0, -4));
        Cable(GhPos[1], HabPos); Cable(DrillPos[0], HabPos);
        BuildActors();
        initialBuild = true; RefreshWorld(); initialBuild = false;
    }

    Mod NewMod(string id, string station, string title, Vector3 pos, float radius, float height, bool solid = true)
    {
        var m = new Mod { id = id, station = station, title = title, radius = radius, height = height, solid = solid };
        m.pos = Ground(pos); m.root = Empty(id, siteRoot, m.pos);
        var bc = m.root.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, height * 0.5f, 0); bc.size = new Vector3(radius * 1.7f, height, radius * 1.7f);
        modByCol[bc] = m;
        Vector3 toC = new Vector3(-pos.x, 0, -pos.z); if (toC.sqrMagnitude < 0.01f) toC = Vector3.back;
        m.ipos = Ground(pos + toC.normalized * (radius + 1.4f));
        m.holo = Label3D(siteRoot, m.pos + Vector3.up * (height + 1.1f), title, 0.085f, C(0.75f, 0.95f, 1f));
        mods.Add(m); return m;
    }
    TextMesh Label3D(Transform parent, Vector3 pos, string text, float size, Color col)
    {
        var go = new GameObject("Holo"); go.transform.SetParent(parent, false); go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 56; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center; tm.color = col; tm.text = text;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        billboards.Add(go.transform); return tm;
    }
    void Lamp(Mod m, PrimitiveType type, Transform parent, Vector3 lpos, Vector3 scale, Color col, Vector3? euler = null)
    {
        var mat = Unlit(col); P(type, parent, lpos, scale, mat, euler, false); m.lamps.Add(mat); m.lampCols.Add(col);
    }
    void Spin(Mod m, Transform t, Vector3 axis) { m.spin.Add(t); m.spinAxis.Add(axis); }
    void Cable(Vector3 a, Vector3 b)
    {
        a = Ground(a) + Vector3.up * 0.04f; b = Ground(b) + Vector3.up * 0.04f;
        var c = P(PrimitiveType.Cube, siteRoot, (a + b) * 0.5f, new Vector3(0.14f, 0.07f, Vector3.Distance(a, b)), Lit(0.1f, 0.1f, 0.12f), null, false);
        c.transform.rotation = Quaternion.LookRotation(b - a); c.name = "Cable";
    }

    void BuildHab()
    {
        var m = habMod = NewMod("hab", "hab", "HABITAT", HabPos, 6.3f, 6.2f); var r = m.root;
        m.ipos = Ground(new Vector3(0, 0, -8.4f));
        var white = Lit(0.9f, 0.91f, 0.94f); var grey = Lit(0.5f, 0.54f, 0.62f); var orange = Lit(0.95f, 0.5f, 0.15f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.6f, 0), new Vector3(9, 1.6f, 9), white);
        P(PrimitiveType.Sphere, r, new Vector3(0, 3.2f, 0), new Vector3(9, 5f, 9), white);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.9f, 0), new Vector3(9.15f, 0.22f, 9.15f), orange);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 3.15f, 0), new Vector3(9.2f, 0.1f, 9.2f), grey);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f + 18f; if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 30f) continue;
            var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
            Lamp(m, PrimitiveType.Cube, r, d * 4.48f + Vector3.up * 2.15f, new Vector3(1.1f, 0.7f, 0.12f), C(1f, 0.86f, 0.55f), new Vector3(0, a, 0));
        }
        P(PrimitiveType.Cube, r, new Vector3(0, 1.3f, -5.0f), new Vector3(2.7f, 2.6f, 2.4f), grey);
        P(PrimitiveType.Cube, r, new Vector3(0, 1.1f, -6.22f), new Vector3(1.4f, 2.0f, 0.1f), Lit(0.2f, 0.22f, 0.28f));
        Lamp(m, PrimitiveType.Cube, r, new Vector3(0, 2.35f, -6.23f), new Vector3(1.6f, 0.18f, 0.1f), C(0.3f, 1f, 0.6f));
        P(PrimitiveType.Cylinder, r, new Vector3(2.4f, 6.4f, 1.4f), new Vector3(0.12f, 2.2f, 0.12f), grey);
        var dish = P(PrimitiveType.Sphere, r, new Vector3(2.4f, 7.6f, 1.4f), new Vector3(1.5f, 0.35f, 1.5f), white, new Vector3(35, 0, 0));
        Lamp(m, PrimitiveType.Sphere, r, new Vector3(2.4f, 8.7f, 1.4f), Vector3.one * 0.25f, C(1f, 0.2f, 0.2f));
        // shield walls (regolith), one ring per layer
        var bag = Lit(site == "mars" ? C(0.5f, 0.28f, 0.18f) : C(0.42f, 0.42f, 0.45f));
        for (int layer = 0; layer < 3; layer++)
        {
            var ring = new GameObject("Shield" + layer); ring.transform.SetParent(r, false);
            float rad = 5.2f + layer * 0.75f, hgt = 1.2f + layer * 0.9f;
            for (int i = 0; i < 26; i++)
            {
                float a = i * (360f / 26f); if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 26f) continue;
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                P(PrimitiveType.Cube, ring.transform, d * rad + Vector3.up * hgt * 0.5f, new Vector3(1.45f, hgt, 0.8f), bag, new Vector3(0, a, 0));
            }
            shieldRings.Add(ring);
        }
        shelterMat = Glass(C(0.3f, 0.7f, 1f, 0.0f));
        shelterBubble = P(PrimitiveType.Sphere, r, new Vector3(0, 1.5f, 0), Vector3.one * 15f, shelterMat, null, false);
        var lg = new GameObject("HabLight"); lg.transform.SetParent(r, false); lg.transform.localPosition = new Vector3(0, 4.5f, -7.5f);
        habLight = lg.AddComponent<Light>(); habLight.type = LightType.Point; habLight.range = 30f; habLight.color = C(1f, 0.85f, 0.6f); habLight.shadows = LightShadows.None;
    }
    void BuildOxygen()
    {
        var m = oxyMod = NewMod("oxygen", "oxygen", "OXYGEN PLANT", OxyPos, 3.0f, 4.2f); var r = m.root;
        var white = Lit(0.88f, 0.9f, 0.94f); var dark = Lit(0.22f, 0.25f, 0.32f);
        P(PrimitiveType.Cube, r, new Vector3(0, 0.15f, 0), new Vector3(5f, 0.3f, 4f), Lit(0.4f, 0.43f, 0.5f));
        foreach (float x in new[] { -1.3f, 1.3f })
        {
            P(PrimitiveType.Capsule, r, new Vector3(x, 2.0f, 0.7f), new Vector3(1.4f, 1.7f, 1.4f), white);
            P(PrimitiveType.Cylinder, r, new Vector3(x, 2.0f, 0.7f), new Vector3(1.45f, 0.12f, 1.45f), Lit(0.2f, 0.7f, 0.95f));
        }
        P(PrimitiveType.Cube, r, new Vector3(0, 1.0f, -0.9f), new Vector3(2.4f, 1.6f, 1.3f), dark);
        var fan = Empty("Fan", r, new Vector3(0, 1.9f, -0.9f));
        P(PrimitiveType.Cube, fan, Vector3.zero, new Vector3(1.6f, 0.06f, 0.22f), white); P(PrimitiveType.Cube, fan, Vector3.zero, new Vector3(0.22f, 0.06f, 1.6f), white);
        Spin(m, fan, Vector3.up * 420f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 2.6f, 0.7f), new Vector3(0.18f, 1.3f, 0.18f), dark, new Vector3(0, 0, 90));
        Lamp(m, PrimitiveType.Sphere, r, new Vector3(0, 4.1f, 0.7f), Vector3.one * 0.4f, C(0.3f, 0.9f, 1f));
        Lamp(m, PrimitiveType.Cube, r, new Vector3(0, 1.2f, -1.57f), new Vector3(1.4f, 0.5f, 0.06f), C(0.3f, 0.9f, 1f));
    }
    void BuildLab()
    {
        var m = labMod = NewMod("lab", "lab", "SCIENCE LAB", LabPos, 3.4f, 4.4f); var r = m.root;
        var white = Lit(0.9f, 0.9f, 0.95f); var grey = Lit(0.45f, 0.48f, 0.58f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.7f, 0), new Vector3(2.8f, 2.6f, 2.8f), white, new Vector3(0, 0, 90));
        P(PrimitiveType.Sphere, r, new Vector3(-2.6f, 1.7f, 0), Vector3.one * 2.8f, white); P(PrimitiveType.Sphere, r, new Vector3(2.6f, 1.7f, 0), Vector3.one * 2.8f, white);
        foreach (float x in new[] { -1.8f, 1.8f }) P(PrimitiveType.Cube, r, new Vector3(x, 0.25f, 0), new Vector3(0.5f, 0.5f, 2.6f), grey);
        for (int i = 0; i < 4; i++) Lamp(m, PrimitiveType.Cube, r, new Vector3(-1.8f + i * 1.2f, 1.9f, -1.38f), new Vector3(0.7f, 0.55f, 0.1f), C(0.72f, 0.55f, 1f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 3.5f, 0), new Vector3(0.14f, 0.6f, 0.14f), grey);
        var dishP = Empty("Dish", r, new Vector3(0, 4.2f, 0));
        P(PrimitiveType.Sphere, dishP, new Vector3(0, 0, 0.2f), new Vector3(1.9f, 1.9f, 0.4f), white, new Vector3(-30, 0, 0));
        P(PrimitiveType.Cylinder, dishP, new Vector3(0, 0.3f, -0.3f), new Vector3(0.06f, 0.5f, 0.06f), grey, new Vector3(60, 0, 0));
        Spin(m, dishP, Vector3.up * 40f);
    }
    void BuildDepot()
    {
        var m = depotMod = NewMod("depot", "depot", "SUPPLY DEPOT", DepotPos, 3.3f, 3.2f); var r = m.root;
        P(PrimitiveType.Cube, r, new Vector3(0, 0.06f, 0), new Vector3(6.4f, 0.12f, 5f), Lit(0.3f, 0.32f, 0.38f));
        var cr = Lit(0.85f, 0.55f, 0.2f); var cr2 = Lit(0.7f, 0.42f, 0.16f);
        for (int i = 0; i < 14; i++)
        {
            int layer = i < 8 ? 0 : (i < 12 ? 1 : 2); int k = layer == 0 ? i : (layer == 1 ? i - 8 : i - 12);
            int perRow = layer == 0 ? 4 : 2; float sp = 1.15f;
            float x = layer == 0 ? (k % perRow - 1.5f) * sp : (layer == 1 ? (k % 2 - 0.5f) * sp : 0);
            float z = layer == 0 ? (k / perRow - 0.5f) * sp + 0.6f : (layer == 1 ? (k / 2 - 0.5f) * sp + 0.6f : 0.6f + (k - 0.5f) * 0.01f);
            if (layer == 2) x = (k - 0.5f) * sp * 0.5f;
            var c = P(PrimitiveType.Cube, r, new Vector3(x, 0.6f + layer * 0.98f, z), Vector3.one * 0.95f, i % 2 == 0 ? cr : cr2, new Vector3(0, (i * 37) % 12 - 6, 0));
            crates.Add(c);
        }
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.9f, -1.9f), new Vector3(0.14f, 0.9f, 0.14f), Lit(0.4f, 0.4f, 0.48f));
        Lamp(m, PrimitiveType.Cube, r, new Vector3(0, 1.75f, -1.95f), new Vector3(1.1f, 0.7f, 0.08f), C(1f, 0.85f, 0.3f), new Vector3(-15, 0, 0));
        // "start here" marker for the first day
        objMarker = P(PrimitiveType.Cube, siteRoot, m.pos + Vector3.up * 5.6f, Vector3.one * 0.7f, Unlit(C(1f, 0.85f, 0.25f)), new Vector3(45, 45, 0), false).transform;
    }
    void BuildLander()
    {
        var m = landerMod = NewMod("lander", "lander", "ASCENT VEHICLE", LanderPos, 5.2f, 8.5f); var r = m.root;
        var pad = P(PrimitiveType.Cylinder, siteRoot, m.pos + Vector3.up * 0.03f, new Vector3(13f, 0.03f, 13f), Lit(0.2f, 0.21f, 0.25f), null, false);
        for (int i = 0; i < 12; i++) { var d = Quaternion.Euler(0, i * 30f, 0) * Vector3.forward; Lamp(m, PrimitiveType.Sphere, siteRoot, m.pos + d * 6.3f + Vector3.up * 0.12f, Vector3.one * 0.25f, C(1f, 0.6f, 0.2f)); }
        BuildShip(r);
        // relief ship pad (used for the success ending)
        reliefShip = new GameObject("ReliefShip"); reliefShip.transform.SetParent(siteRoot, false);
        BuildShip(reliefShip.transform); reliefShip.SetActive(false);
    }
    void BuildShip(Transform r)
    {
        var white = Lit(0.92f, 0.92f, 0.95f); var dark = Lit(0.18f, 0.19f, 0.24f); var grey = Lit(0.5f, 0.52f, 0.6f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 3.7f, 0), new Vector3(3.2f, 2.2f, 3.2f), white);
        P(PrimitiveType.Sphere, r, new Vector3(0, 5.9f, 0), new Vector3(3.2f, 4.6f, 3.2f), white);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 4.6f, 0), new Vector3(3.26f, 0.2f, 3.26f), Lit(0.95f, 0.5f, 0.15f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.15f, 0), new Vector3(1.9f, 0.45f, 1.9f), dark);
        P(PrimitiveType.Sphere, r, new Vector3(0, 5.6f, -1.45f), new Vector3(1f, 0.8f, 0.5f), Lit(0.1f, 0.25f, 0.45f));
        for (int i = 0; i < 4; i++)
        {
            var d = Quaternion.Euler(0, i * 90f + 45f, 0) * Vector3.forward;
            Vector3 a = d * 1.35f + Vector3.up * 2.6f, b = d * 3.1f + Vector3.up * 0.12f;
            var leg = P(PrimitiveType.Cylinder, r, (a + b) * 0.5f, new Vector3(0.18f, Vector3.Distance(a, b) * 0.5f, 0.18f), grey);
            leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            P(PrimitiveType.Cylinder, r, d * 3.1f + Vector3.up * 0.08f, new Vector3(0.9f, 0.08f, 0.9f), grey);
        }
    }
    void BuildRover()
    {
        var r = Empty("Rover", siteRoot, Ground(RoverPos)); r.localEulerAngles = new Vector3(0, -35, 0);
        var white = Lit(0.88f, 0.88f, 0.92f); var dark = Lit(0.15f, 0.15f, 0.18f);
        P(PrimitiveType.Cube, r, new Vector3(0, 1.0f, 0), new Vector3(1.8f, 0.7f, 3.2f), white);
        P(PrimitiveType.Cube, r, new Vector3(0, 1.6f, 0.6f), new Vector3(1.5f, 0.7f, 1.4f), Glass(C(0.3f, 0.6f, 0.9f, 0.55f)));
        foreach (float x in new[] { -1.05f, 1.05f }) foreach (float z in new[] { -1.15f, 0f, 1.15f })
            P(PrimitiveType.Cylinder, r, new Vector3(x, 0.5f, z), new Vector3(1f, 0.16f, 1f), dark, new Vector3(0, 0, 90));
        P(PrimitiveType.Cylinder, r, new Vector3(0.5f, 2.2f, -1.2f), new Vector3(0.05f, 0.9f, 0.05f), dark);
        P(PrimitiveType.Cube, r, new Vector3(0, 1.45f, -1.0f), new Vector3(1.6f, 0.06f, 1.1f), Lit(0.1f, 0.15f, 0.4f));
    }

    // ------------------------------------------------------------------ slots (things the player can build)
    void AddSlot(string type, int index, Vector3 pos, float radius)
    {
        var s = new Slot { type = type, index = index, pos = Ground(pos), radius = radius };
        var acc = type == "greenhouse" ? C(0.4f, 1f, 0.55f) : type == "drill" ? C(0.4f, 0.9f, 1f) : type == "reactor" ? C(0.4f, 1f, 0.9f) : C(1f, 0.85f, 0.4f);
        s.marker = P(PrimitiveType.Cylinder, siteRoot, s.pos + Vector3.up * 0.03f, new Vector3(radius * 2f, 0.02f, radius * 2f), Glass(new Color(acc.r, acc.g, acc.b, 0.16f)), null, false);
        s.label = Label3D(siteRoot, s.pos + Vector3.up * 1.6f, "", 0.07f, acc);
        s.scaffold = new GameObject("Scaffold"); s.scaffold.transform.SetParent(siteRoot, false); s.scaffold.transform.position = s.pos;
        var pole = Lit(0.75f, 0.7f, 0.3f); float w = Mathf.Min(radius, 2.2f) * 0.75f;
        foreach (float x in new[] { -w, w }) foreach (float z in new[] { -w, w }) P(PrimitiveType.Cylinder, s.scaffold.transform, new Vector3(x, 1.2f, z), new Vector3(0.1f, 1.2f, 0.1f), pole);
        P(PrimitiveType.Cube, s.scaffold.transform, new Vector3(0, 2.4f, -w), new Vector3(w * 2f, 0.08f, 0.08f), pole); P(PrimitiveType.Cube, s.scaffold.transform, new Vector3(0, 2.4f, w), new Vector3(w * 2f, 0.08f, 0.08f), pole);
        P(PrimitiveType.Cube, s.scaffold.transform, new Vector3(0, 0.45f, 0), Vector3.one * 0.9f, Lit(0.85f, 0.55f, 0.2f), new Vector3(0, 20, 0));
        s.built = new GameObject(type + index); s.built.transform.SetParent(siteRoot, false); s.built.transform.position = s.pos;
        var r = s.built.transform;
        if (type == "greenhouse") BuildGreenhouse(s, r);
        else if (type == "drill") BuildDrill(s, r);
        else if (type == "solar") BuildSolar(s, r);
        else if (type == "battery") BuildBattery(s, r);
        else BuildReactor(s, r);
        slots.Add(s);
    }
    Mod SlotMod(Slot s, string title, float height)
    {
        var m = new Mod { id = s.type + s.index, station = s.type, title = title, radius = s.radius, height = height, pos = s.pos, root = s.built.transform };
        var bc = s.built.AddComponent<BoxCollider>(); bc.center = new Vector3(0, height * 0.5f, 0); bc.size = new Vector3(s.radius * 1.7f, height, s.radius * 1.7f); modByCol[bc] = m;
        Vector3 toC = new Vector3(-s.pos.x, 0, -s.pos.z); m.ipos = Ground(s.pos + toC.normalized * (s.radius + 1.4f));
        m.holo = Label3D(s.built.transform, s.pos + Vector3.up * (height + 0.9f), title, 0.075f, C(0.75f, 0.95f, 1f));
        s.mod = m; mods.Add(m); return m;
    }
    void BuildGreenhouse(Slot s, Transform r)
    {
        var m = SlotMod(s, "GREENHOUSE", 3.4f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.25f, 0), new Vector3(5.6f, 0.25f, 5.6f), Lit(0.3f, 0.33f, 0.4f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.52f, 0), new Vector3(5f, 0.04f, 5f), Lit(0.25f, 0.16f, 0.1f));
        var leaf = Lit(0.25f, 0.75f, 0.3f); var leaf2 = Lit(0.35f, 0.85f, 0.35f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f, rad = i % 2 == 0 ? 1.7f : 0.85f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward * rad;
            var p = Empty("Plant", r, new Vector3(d.x, 0.55f, d.z));
            P(PrimitiveType.Sphere, p, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.6f, 0.6f), i % 3 == 0 ? leaf2 : leaf);
            P(PrimitiveType.Cylinder, p, new Vector3(0, 0.1f, 0), new Vector3(0.06f, 0.15f, 0.06f), leaf);
            m.plants.Add(p);
        }
        Lamp(m, PrimitiveType.Cube, r, new Vector3(0, 2.3f, 0), new Vector3(3.2f, 0.08f, 0.3f), C(1f, 0.35f, 0.9f));
        Lamp(m, PrimitiveType.Cube, r, new Vector3(0, 2.3f, 0), new Vector3(0.3f, 0.08f, 3.2f), C(1f, 0.35f, 0.9f));
        P(PrimitiveType.Sphere, r, new Vector3(0, 0.5f, 0), new Vector3(5.6f, 5.4f, 5.6f), Glass(C(0.65f, 0.92f, 1f, 0.3f)), null, false);
    }
    void BuildDrill(Slot s, Transform r)
    {
        var m = SlotMod(s, "ICE DRILL", 5.6f);
        var grey = Lit(0.5f, 0.53f, 0.6f); var yellow = Lit(0.95f, 0.78f, 0.15f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.08f, 0), new Vector3(3.6f, 0.08f, 3.6f), Lit(0.28f, 0.3f, 0.36f));
        for (int i = 0; i < 3; i++)
        {
            var d = Quaternion.Euler(0, i * 120f, 0) * Vector3.forward;
            P(PrimitiveType.Cylinder, r, d * 0.75f + Vector3.up * 2.6f, new Vector3(0.14f, 2.75f, 0.14f), yellow, new Vector3(0, i * 120f, 0) + new Vector3(-17, 0, 0));
        }
        P(PrimitiveType.Cube, r, new Vector3(0, 5.2f, 0), new Vector3(0.9f, 0.7f, 0.9f), grey);
        var bit = Empty("Bit", r, new Vector3(0, 2.5f, 0));
        P(PrimitiveType.Cylinder, bit, Vector3.zero, new Vector3(0.3f, 2.4f, 0.3f), Lit(0.2f, 0.2f, 0.25f));
        P(PrimitiveType.Cube, bit, new Vector3(0, -1.2f, 0), new Vector3(0.6f, 0.1f, 0.14f), grey); P(PrimitiveType.Cube, bit, new Vector3(0, 0.2f, 0), new Vector3(0.14f, 0.1f, 0.6f), grey);
        Spin(m, bit, Vector3.up * 500f); m.bob = bit; m.bobBase = bit.localPosition;
        P(PrimitiveType.Sphere, r, new Vector3(1.5f, 0.85f, 0.4f), Vector3.one * 1.5f, Lit(0.7f, 0.9f, 1f));
        Lamp(m, PrimitiveType.Sphere, r, new Vector3(0, 5.75f, 0), Vector3.one * 0.3f, C(0.4f, 0.9f, 1f));
    }
    void BuildSolar(Slot s, Transform r)
    {
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.0f, 0), new Vector3(0.16f, 1.0f, 0.16f), Lit(0.5f, 0.52f, 0.6f));
        s.pivot = Empty("Pivot", r, new Vector3(0, 2.1f, 0));
        s.panel = new Material(shLit); s.panel.color = C(0.08f, 0.14f, 0.42f);
        P(PrimitiveType.Cube, s.pivot, Vector3.zero, new Vector3(4.4f, 0.08f, 2.6f), s.panel);
        P(PrimitiveType.Cube, s.pivot, new Vector3(0, -0.05f, 0), new Vector3(4.5f, 0.05f, 0.1f), Lit(0.7f, 0.72f, 0.78f));
    }
    void BuildBattery(Slot s, Transform r)
    {
        P(PrimitiveType.Cube, r, new Vector3(0, 0.8f, 0), new Vector3(1.3f, 1.6f, 1.0f), Lit(0.25f, 0.28f, 0.34f));
        P(PrimitiveType.Cube, r, new Vector3(0, 0.8f, -0.51f), new Vector3(0.36f, 1.3f, 0.02f), Lit(0.08f, 0.09f, 0.12f), null, false);
        s.barMat = Unlit(C(0.3f, 1f, 0.5f));
        s.bar = Empty("Bar", r, new Vector3(0, 0.15f, -0.53f));
        P(PrimitiveType.Cube, s.bar, new Vector3(0, 0.5f, 0), new Vector3(0.3f, 1f, 0.02f), s.barMat, null, false);
    }
    void BuildReactor(Slot s, Transform r)
    {
        var m = reactorMod = SlotMod(s, "FISSION REACTOR", 5.6f);
        var dark = Lit(0.22f, 0.24f, 0.3f); var white = Lit(0.9f, 0.9f, 0.94f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.1f, 0), new Vector3(4.6f, 0.1f, 4.6f), Lit(0.3f, 0.3f, 0.36f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.3f, 0), new Vector3(1.7f, 1.2f, 1.7f), dark);
        Lamp(m, PrimitiveType.Cylinder, r, new Vector3(0, 1.9f, 0), new Vector3(1.85f, 0.14f, 1.85f), C(0.3f, 1f, 0.9f));
        Lamp(m, PrimitiveType.Cylinder, r, new Vector3(0, 0.8f, 0), new Vector3(1.85f, 0.08f, 1.85f), C(0.3f, 1f, 0.9f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 3.4f, 0), new Vector3(0.3f, 1.1f, 0.3f), dark);
        for (int i = 0; i < 4; i++) P(PrimitiveType.Cube, r, new Vector3(0, 4.3f, 0) + Quaternion.Euler(0, i * 90f, 0) * Vector3.forward * 1.25f, new Vector3(0.08f, 2.3f, 2.3f), white, new Vector3(0, i * 90f, 0));
    }

    // ------------------------------------------------------------------ mirror the simulation
    int CountPending(string type) { return st.pending.Count(p => p.type == type); }
    int CountPlanned(string type) { return plan.build.Count(b => b == type); }

    void RefreshWorld()
    {
        if (st == null || siteRoot == null) return;
        foreach (var s in slots)
        {
            int built = st.mod[s.type], pend = CountPending(s.type), planned = CountPlanned(s.type);
            int state = s.index < built ? 3 : (s.index < built + pend ? 2 : (s.index < built + pend + planned ? 1 : 0));
            if (state != s.state)
            {
                bool appear = state == 3 && s.state != 3 && !initialBuild;
                s.state = state;
                s.built.SetActive(state == 3); s.scaffold.SetActive(state == 2); s.marker.SetActive(state <= 1);
                s.label.text = state == 1 ? "PLANNED: " + Sim.GetTypeInfo(s.type).name.ToUpper() : (state == 2 ? "UNDER CONSTRUCTION" : "");
                if (appear) { s.pop = 0f; Burst(s.pos + Vector3.up, C(0.6f, 0.95f, 1f), 26, 6f, 0.5f, 1f); DustPuff(s.pos, 14); }
            }
        }
        for (int i = 0; i < shieldRings.Count; i++) shieldRings[i].SetActive(i < st.mod["shield"]);
        int left = st.crates - QueuedCost() - (plan.repair ? 1 : 0);
        for (int i = 0; i < crates.Count; i++) crates[i].SetActive(i < left);

        bool hasPv = pv != null;
        int ghLit = hasPv ? pv.rec.litGh : 0;
        int drBroken = st.faults.Count(f => f.type == "drill");
        int drOn = hasPv ? Mathf.Max(0, Mathf.Min(plan.drill, st.mod["drill"] - drBroken) - pv.shut.Count(x => x == "drill")) : 0;
        bool ghShort = hasPv && (pv.shut.Contains("greenhouse") || pv.idle.Contains("greenhouse"));
        foreach (var s in slots)
        {
            if (s.mod == null || s.state != 3) continue;
            var m = s.mod;
            if (s.type == "greenhouse")
            {
                bool br = s.index < st.faults.Count(f => f.type == "greenhouse");
                m.broken = br; m.on = s.index < ghLit; m.noPower = !m.on && !br && s.index < plan.greenhouse && ghShort;
                float g = s.index < st.gh.Count ? st.gh[s.index] / (float)V("gh_cycle") : 0f;
                foreach (var p in m.plants) p.localScale = Vector3.one * (0.25f + 1.05f * g);
                m.state = br ? "BROKEN" : m.noPower ? "NO POWER" : m.on ? "growing  " + Mathf.RoundToInt(g * 100) + "%" : "OFF";
            }
            else if (s.type == "drill")
            {
                bool br = s.index < drBroken;
                m.broken = br; m.on = !br && (s.index - drBroken) < drOn && (s.index - drBroken) >= 0;
                m.noPower = !m.on && !br && (s.index - drBroken) < plan.drill && hasPv && pv.shut.Contains("drill");
                m.state = br ? "BROKEN" : m.noPower ? "NO POWER" : m.on ? "ON  +" + Fmt(V("drill_out")) + " L" : "OFF";
            }
            else if (s.type == "reactor") { m.on = true; m.state = "+" + Fmt(V("reactor_out")) + " energy, day and night"; }
            m.holo.text = m.title + "\n" + m.state;
        }
        bool oxBroken = st.faults.Any(f => f.type == "oxygen"), labBroken = st.faults.Any(f => f.type == "lab");
        oxyMod.broken = oxBroken; oxyMod.on = hasPv && pv.rec.oxOn; oxyMod.noPower = !oxyMod.on && !oxBroken && plan.oxygen;
        oxyMod.state = oxBroken ? "BROKEN" : oxyMod.noPower ? (hasPv && pv.rec.noWater ? "NO WATER" : "NO POWER") : oxyMod.on ? "ON  +" + Fmt(V("o2p_out")) + " kg" : "OFF";
        labMod.broken = labBroken; labMod.on = hasPv && pv.rec.labOn; labMod.noPower = !labMod.on && !labBroken && plan.lab;
        labMod.state = labBroken ? "BROKEN" : labMod.noPower ? "NO POWER" : labMod.on ? "ON" : "OFF";
        habMod.on = !hasPv || pv.habOn; habMod.noPower = hasPv && !pv.habOn;
        habMod.state = habMod.noPower ? "NO POWER" : "rations: " + plan.ration + "   activity: " + plan.activity;
        depotMod.state = left + " crates";
        landerMod.on = !st.emergencyUsed; landerMod.state = st.emergencyUsed ? "fuel reserve used" : (plan.emergency ? "EMERGENCY: fuel to water" : "ready");
        float dust = DustFactor();
        solarMod.state = st.mod["solar"] + " arrays" + (hasPv ? "   +" + Fmt(pv.rec.solar) + " energy" : "") + (site == "mars" ? "\ndust: " + Mathf.RoundToInt(dust * 100) + "%" : "");
        batteryMod.state = Fmt(st.battery) + " / " + Fmt(V("battery_per") * st.mod["battery"]);
        foreach (var m in new[] { oxyMod, labMod, habMod, depotMod, landerMod, solarMod, batteryMod }) m.holo.text = m.title + "\n" + m.state;

        Color clean = C(0.08f, 0.14f, 0.42f), dusty = site == "mars" ? C(0.5f, 0.3f, 0.2f) : clean;
        float cap = (float)(V("battery_per") * st.mod["battery"]); float charge = cap > 0 ? Mathf.Clamp01((float)st.battery / cap) : 0;
        foreach (var s in slots)
        {
            if (s.type == "solar" && s.panel != null) s.panel.color = Color.Lerp(dusty, clean, Mathf.InverseLerp(0.4f, 1f, dust));
            if (s.type == "battery" && s.bar != null)
            {
                s.bar.localScale = new Vector3(1, Mathf.Max(0.03f, charge) * 1.25f, 1);
                s.barMat.color = charge > 0.5f ? C(0.3f, 1f, 0.5f) : (charge > 0.2f ? C(1f, 0.8f, 0.25f) : C(1f, 0.3f, 0.3f));
            }
        }
    }
    float DustFactor() { return site == "mars" ? (float)Math.Max(V("dust_floor"), 1 - V("dust_loss") * st.dustDays) : 1f; }

    void TickWorld(float dt)
    {
        if (siteRoot == null) return;
        float t = Time.time; var cr = cam.transform.rotation;
        Vector3 cpos = cam.transform.position;
        for (int i = 0; i < billboards.Count; i++)
        {
            var b = billboards[i]; if (b == null) continue;
            float d = Vector3.Distance(cpos, b.position);
            b.rotation = cr; b.localScale = Vector3.one * Mathf.Clamp(d / 24f, 0.55f, 3.2f);
            var mr = b.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = d < 95f;
        }
        foreach (var m in mods)
        {
            bool running = m.on && !m.broken && !m.noPower;
            for (int i = 0; i < m.lamps.Count; i++)
            {
                Color c = m.lampCols[i];
                if (m.broken) c = Color.Lerp(Color.black, C(1f, 0.15f, 0.1f), 0.5f + 0.5f * Mathf.Sin(t * 8f));
                else if (m.noPower) c *= Mathf.PerlinNoise(t * 9f, m.pos.x) > 0.72f ? 0.45f : 0.02f;
                else if (!m.on) c *= 0.1f;
                m.lamps[i].color = c;
            }
            if (running) for (int i = 0; i < m.spin.Count; i++) m.spin[i].Rotate(m.spinAxis[i] * dt, Space.Self);
            if (m.bob != null && running) m.bob.localPosition = m.bobBase + Vector3.up * Mathf.Sin(t * 2.2f + m.pos.x) * 0.45f;
            if (m.broken) { m.smokeT -= dt; if (m.smokeT <= 0) { m.smokeT = 0.12f; Smoke(m.pos + Vector3.up * (m.height * 0.7f), C(0.15f, 0.15f, 0.15f, 0.6f), 1.2f, 2.4f); if (UnityEngine.Random.value < 0.25f) Sparks(m.pos + Vector3.up * m.height * 0.6f); } }
        }
        foreach (var s in slots)
        {
            if (s.pop < 1f) { s.pop = Mathf.Min(1f, s.pop + dt * 1.6f); float k = s.pop - 1f; s.built.transform.localScale = Vector3.one * Mathf.Max(0.01f, k * k * (2.7f * k + 1.7f) + 1f); }
            if (s.type == "solar" && s.pivot != null && s.state == 3)
            {
                Vector3 flat = new Vector3(sunDir.x, 0, sunDir.z); float tilt = Mathf.Clamp(90f - Mathf.Max(sunElev, 15f), 10f, 70f);
                if (flat.sqrMagnitude > 0.001f) s.pivot.rotation = Quaternion.Slerp(s.pivot.rotation, Quaternion.LookRotation(flat) * Quaternion.Euler(tilt, 0, 0), dt * 2f);
            }
        }
        if (objMarker != null)
        {
            bool show = mode != Mode.Title && st != null && st.day == 1 && !tipBuild;
            objMarker.gameObject.SetActive(show);
            if (show) { objMarker.position = depotMod.pos + Vector3.up * (5.6f + Mathf.Sin(t * 3f) * 0.4f); objMarker.Rotate(Vector3.up * 90f * dt, Space.World); }
        }
        bool shelter = plan != null && plan.activity == "shelter" && mode != Mode.Title;
        float sa = shelter ? 0.10f + 0.05f * Mathf.Sin(t * 4f) : 0f; var sc = shelterMat.color; sc.a = Mathf.MoveTowards(sc.a, sa, dt * 0.5f); shelterMat.color = sc;
        shelterBubble.SetActive(sc.a > 0.003f);
        if (habLight != null) habLight.intensity = (habMod.noPower ? 0.15f : 1.7f) * (1f - dayA * 0.85f);
    }
}
