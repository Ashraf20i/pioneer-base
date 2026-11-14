// Pioneer Base - PioneerGame.Art.cs
// Visual style: futuristic neon station seen from a raised "oblique" angle (walls and furniture
// show a front face, so the base has depth). Everything is drawn from code, no art files needed.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class PioneerGame
{
    const float RH = 3.3f;          // room floor height
    const float RW = 4.4f;          // room floor width
    static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }
    static readonly Color Metal = new Color(0.07f, 0.10f, 0.16f);
    static readonly Color MetalDark = new Color(0.04f, 0.06f, 0.10f);

    Sprite glow;
    Transform planetRoot; SpriteRenderer planetBody, planetRim, planetShade;
    readonly List<Transform> hallLights = new List<Transform>();
    Transform armL, armR, eyeL, eyeR, pupilL, pupilR;
    SpriteRenderer shieldBubble;

    // ------------------------------------------------------------------ sprites
    void MakeSprites()
    {
        var t = new Texture2D(4, 4); t.filterMode = FilterMode.Point;
        t.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); t.Apply();
        square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);

        const int n = 64;
        var c = new Texture2D(n, n); c.filterMode = FilterMode.Bilinear;
        var rg = new Texture2D(n, n); rg.filterMode = FilterMode.Bilinear;
        var gl = new Texture2D(n, n); gl.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
            c.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d)));
            rg.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d) * Mathf.Clamp01(d - n * 0.36f)));
            float k = Mathf.Clamp01(1f - d / (n / 2f)); gl.SetPixel(x, y, new Color(1, 1, 1, k * k));
        }
        c.Apply(); rg.Apply(); gl.Apply();
        circle = Sprite.Create(c, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        ring = Sprite.Create(rg, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        glow = Sprite.Create(gl, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);

        const int m = 32; const float rad = 12f;
        var s = new Texture2D(m, m); s.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < m; y++) for (int x = 0; x < m; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, rad, m - rad), cy = Mathf.Clamp(y + 0.5f, rad, m - rad);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            float shade = Mathf.Lerp(0.78f, 1.05f, (y + 0.5f) / m) - ((x + 0.5f) / m) * 0.06f;
            s.SetPixel(x, y, new Color(shade, shade, shade, Mathf.Clamp01(rad - d + 0.5f)));
        }
        s.Apply();
        slice = Sprite.Create(s, new Rect(0, 0, m, m), new Vector2(0.5f, 0.5f), m, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
    }

    // ------------------------------------------------------------------ drawing helpers
    SpriteRenderer Quad(string name, Transform parent, Vector2 pos, Vector2 size, Color col, int order, Sprite spr = null)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = spr ?? square; sr.color = col; sr.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        return sr;
    }
    SpriteRenderer Slab(string name, Transform parent, Vector2 pos, Vector2 size, Color col, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = slice; sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size; sr.color = col; sr.sortingOrder = order;
        return sr;
    }
    TextMesh Label(Transform parent, Vector2 pos, string text, float size, Color col, int order = 30)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = font; tm.fontSize = 64; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center; tm.color = col; tm.text = text;
        var mr = go.GetComponent<MeshRenderer>(); mr.material = font.material; mr.sortingOrder = order;
        return tm;
    }
    // furniture seen from the oblique angle: shadow, front face, lit top face
    void Block(Vector2 pos, Vector2 size, Color col, int order = 2)
    {
        float face = Mathf.Min(0.2f, size.y * 0.3f);
        Slab("Shadow", world, pos + new Vector2(0.1f, -0.12f - face * 0.5f), size, new Color(0, 0, 0, 0.4f), order - 1);
        Slab("Front", world, pos + new Vector2(0, -size.y * 0.5f - face * 0.1f), new Vector2(size.x, face * 1.6f), col * 0.5f + new Color(0, 0, 0, 0.5f), order);
        Slab("Top", world, pos, size, col, order);
    }
    void Disc(Vector2 pos, float d, Color col, int order = 3) { Quad("Disc", world, pos, new Vector2(d, d), col, order, circle); }
    void Glow(Transform parent, Vector2 pos, float d, Color col, int order) { Quad("Glow", parent, pos, new Vector2(d, d), col, order, glow); }

    // ------------------------------------------------------------------ world
    static readonly string[] topIds = { "oxygen", "lab", "greenhouse", "drill" };
    static readonly string[] botIds = { "kitchen", "command", "build", "airlock" };

    static Color Accent(string id)
    {
        switch (id)
        {
            case "oxygen": return C(0.25f, 0.85f, 1f);
            case "lab": return C(0.65f, 0.5f, 1f);
            case "greenhouse": return C(0.35f, 1f, 0.55f);
            case "drill": return C(0.3f, 0.95f, 0.9f);
            case "kitchen": return C(1f, 0.7f, 0.3f);
            case "command": return C(1f, 0.35f, 0.55f);
            case "build": return C(1f, 0.88f, 0.3f);
            default: return C(0.75f, 0.85f, 1f);
        }
    }

    void BuildWorld()
    {
        // distant planet (changes with the site), parallax-follows the astronaut a little
        planetRoot = new GameObject("Planet").transform; planetRoot.SetParent(world, false);
        planetRim = Quad("PlanetRim", planetRoot, new Vector2(-7.5f, -3.5f), new Vector2(8.4f, 8.4f), C(0.4f, 0.7f, 1f, 0.35f), -9, glow);
        planetBody = Quad("PlanetBody", planetRoot, new Vector2(-7.5f, -3.5f), new Vector2(6.4f, 6.4f), C(0.55f, 0.6f, 0.7f), -8, circle);
        planetShade = Quad("PlanetShade", planetRoot, new Vector2(-6.7f, -4.1f), new Vector2(6.2f, 6.2f), C(0, 0, 0, 0.45f), -7, circle);
        Quad("Earth", planetRoot, new Vector2(9.2f, 4.4f), new Vector2(1.1f, 1.1f), C(0.3f, 0.55f, 1f), -8, circle);
        Quad("EarthGlow", planetRoot, new Vector2(9.2f, 4.4f), new Vector2(1.9f, 1.9f), C(0.4f, 0.7f, 1f, 0.35f), -9, glow);

        var rnd = new System.Random(7);
        for (int i = 0; i < 110; i++)
        {
            float x = (float)(rnd.NextDouble() * 32 - 16), y = (float)(rnd.NextDouble() * 20 - 10);
            float sz = 0.04f + (float)rnd.NextDouble() * 0.08f;
            var s = Quad("Star", world, new Vector2(x, y), new Vector2(sz, sz), C(1, 1, 1, 0.3f + (float)rnd.NextDouble() * 0.6f), -10, circle);
            stars.Add(s);
        }

        // hall: dark metal floor with moving guide lights
        Quad("HallGlow", world, Vector2.zero, new Vector2(19.9f, 3.1f), C(0.3f, 0.6f, 1f, 0.12f), -5, glow);
        Slab("HallFrame", world, Vector2.zero, new Vector2(19.5f, 2.75f), MetalDark, -3);
        Slab("HallEdge", world, Vector2.zero, new Vector2(19.3f, 2.55f), C(0.35f, 0.65f, 1f, 0.85f), -2);
        Quad("Hall", world, Vector2.zero, new Vector2(19f, 2.3f), C(0.06f, 0.09f, 0.15f), -1);
        for (int i = -9; i <= 9; i++) Quad("Tile", world, new Vector2(i, 0), new Vector2(0.025f, 2.3f), C(0.4f, 0.7f, 1f, 0.07f), 0);
        Quad("Tile", world, new Vector2(0, 0), new Vector2(19f, 0.025f), C(0.4f, 0.7f, 1f, 0.07f), 0);
        Quad("HallFront", world, new Vector2(0, -1.45f), new Vector2(19.2f, 0.34f), C(0.03f, 0.05f, 0.09f), -2);
        for (int i = -8; i <= 8; i += 2) Quad("HallWin", world, new Vector2(i, -1.45f), new Vector2(0.9f, 0.06f), C(0.4f, 0.8f, 1f, 0.5f), -1);
        for (int i = 0; i < 14; i++)
        {
            var l = Quad("Guide", world, new Vector2(0, i % 2 == 0 ? 0.92f : -0.92f), new Vector2(0.55f, 0.05f), C(0.45f, 0.85f, 1f, 0.8f), 1);
            hallLights.Add(l.transform);
        }
        hallDark = Quad("HallDark", world, Vector2.zero, new Vector2(19f, 2.3f), Color.black, 6); hallDark.color = new Color(0, 0, 0, 0);
        Label(world, Vector2.zero, "H A B I T A T   C O R E", 0.045f, C(0.5f, 0.85f, 1f, 0.4f), 3);

        float[] xs = { -7.2f, -2.4f, 2.4f, 7.2f };
        string[] topNames = { "OXYGEN PLANT", "SCIENCE LAB", "GREENHOUSE", "ICE DRILL" };
        string[] botNames = { "KITCHEN", "COMMAND", "BUILD BAY", "AIRLOCK" };
        for (int i = 0; i < 4; i++)
        {
            AddRoom(topIds[i], topNames[i], new Vector2(xs[i], 3.25f), true, topIds[i]);
            AddRoom(botIds[i], botNames[i], new Vector2(xs[i], -3.25f), false, null);
        }
        foreach (var r in rooms.Values) AddStation(r.id, r.id, r.center + new Vector2(0, r.top ? -0.2f : 0.2f), r.accent);

        BuildAstronaut();
        BuildFxRoot();
    }

    readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();

    void AddRoom(string id, string name, Vector2 c, bool top, string system)
    {
        float s = top ? 1f : -1f; Color acc = Accent(id);
        Quad(id + "Glow", world, c, new Vector2(RW + 0.9f, RH + 0.9f), new Color(acc.r, acc.g, acc.b, 0.16f), -5, glow);
        Slab(id + "Frame", world, c, new Vector2(RW + 0.3f, RH + 0.3f), MetalDark, -3);
        Slab(id + "Edge", world, c, new Vector2(RW + 0.14f, RH + 0.14f), new Color(acc.r, acc.g, acc.b, 0.9f), -2);
        Quad(id + "Floor", world, c, new Vector2(RW, RH), Color.Lerp(Metal, acc, 0.1f), -1);
        for (int i = -2; i <= 2; i++) Quad("Tile", world, c + new Vector2(i * 0.9f, 0), new Vector2(0.025f, RH), new Color(acc.r, acc.g, acc.b, 0.1f), 0);
        for (int j = -1; j <= 1; j++) Quad("Tile", world, c + new Vector2(0, j * 0.95f), new Vector2(RW, 0.025f), new Color(acc.r, acc.g, acc.b, 0.1f), 0);
        // corner brackets
        foreach (float sx in new[] { -1f, 1f }) foreach (float sy in new[] { -1f, 1f })
        {
            Quad("Br", world, c + new Vector2(sx * (RW / 2 - 0.2f), sy * (RH / 2 - 0.05f)), new Vector2(0.4f, 0.05f), acc, 1);
            Quad("Br", world, c + new Vector2(sx * (RW / 2 - 0.05f), sy * (RH / 2 - 0.2f)), new Vector2(0.05f, 0.4f), acc, 1);
        }
        // oblique front face of the room (south wall seen from the raised camera)
        float fy = -RH / 2 - 0.17f;
        Quad(id + "Front", world, c + new Vector2(0, fy), new Vector2(RW + 0.14f, 0.34f), C(0.03f, 0.05f, 0.09f), -2);
        for (int i = -3; i <= 3; i++) Quad("Win", world, c + new Vector2(i * 0.6f, fy), new Vector2(0.3f, 0.05f), new Color(acc.r, acc.g, acc.b, 0.55f), -1);

        // sliding door toward the hall
        Vector2 doorPos = new Vector2(c.x, top ? 1.375f : -1.375f);
        Quad(id + "Tunnel", world, doorPos, new Vector2(1.5f, 0.55f), C(0.06f, 0.09f, 0.15f), 0);
        var dl = Slab(id + "DoorL", world, doorPos + new Vector2(-0.37f, 0), new Vector2(0.8f, 0.3f), C(0.3f, 0.36f, 0.5f), 2);
        var dr = Slab(id + "DoorR", world, doorPos + new Vector2(0.37f, 0), new Vector2(0.8f, 0.3f), C(0.3f, 0.36f, 0.5f), 2);
        Quad("DoorLight", dl.transform, new Vector2(0.3f, 0), new Vector2(0.1f, 0.1f), acc, 3, circle);

        Furnish(id, c, s);
        var dark = Quad(id + "Dark", world, c, new Vector2(RW, RH), Color.black, 6); dark.color = new Color(0, 0, 0, 0);
        var alarm = Quad(id + "Alarm", world, c, new Vector2(RW, RH), C(1f, 0.1f, 0.15f), 7); alarm.color = new Color(1, 0.1f, 0.15f, 0);
        Slab("Pill", world, c + new Vector2(0, s * 1.15f), new Vector2(3.3f, 0.75f), C(0.02f, 0.04f, 0.08f, 0.7f), 29);
        Quad("PillLine", world, c + new Vector2(0, s * 1.15f - 0.34f), new Vector2(3.0f, 0.03f), acc, 29);
        var tm = Label(world, c + new Vector2(0, s * 1.15f), name, 0.045f, Color.white);
        rooms[id] = new Room { id = id, name = name, system = system, center = c, top = top, dark = dark, label = tm,
            accent = acc, alarm = alarm, doorL = dl.transform, doorR = dr.transform, doorPos = doorPos };
    }

    void AddStation(string id, string roomId, Vector2 pos, Color acc)
    {
        stations.Add(new Station { id = id, roomId = roomId, pos = pos });
        var pad = Slab("Pad_" + id, world, pos + new Vector2(0, -0.08f), new Vector2(1.0f, 0.6f), C(0.1f, 0.13f, 0.2f), 9);
        var s = Quad("Station_" + id, world, pos, new Vector2(0.9f, 0.9f), acc, 10, ring);
        Glow(s.transform, Vector2.zero, 1.5f, new Color(acc.r, acc.g, acc.b, 0.55f), 9);
        Quad("StationCore", s.transform, Vector2.zero, new Vector2(0.45f, 0.45f), Color.Lerp(acc, Color.white, 0.5f), 11, circle);
        pulses.Add(s.transform);
    }

    void Furnish(string id, Vector2 c, float s)
    {
        float L = -1.7f, R = 1.7f; var ac = Accent(id);
        Color metal = C(0.42f, 0.48f, 0.6f), light = C(0.62f, 0.68f, 0.78f);
        switch (id)
        {
            case "oxygen":
                foreach (float x in new[] { L, R }) foreach (float y in new[] { -0.5f, 0.5f })
                { Block(c + new Vector2(x, s * y), new Vector2(0.75f, 0.75f), light); Disc(c + new Vector2(x, s * y), 0.34f, new Color(ac.r, ac.g, ac.b, 0.9f), 4); }
                Block(c + new Vector2(0, s * 0.8f), new Vector2(2.2f, 0.16f), metal);
                break;
            case "lab":
                foreach (float x in new[] { L, R })
                {
                    Block(c + new Vector2(x, 0), new Vector2(0.6f, 2.2f), light);
                    Block(c + new Vector2(x, s * 0.5f), new Vector2(0.4f, 0.5f), ac, 4);
                    Block(c + new Vector2(x, s * -0.5f), new Vector2(0.35f, 0.35f), C(0.7f, 1f, 0.6f), 4);
                }
                break;
            case "greenhouse":
                foreach (float x in new[] { L, R })
                {
                    Block(c + new Vector2(x, 0), new Vector2(0.7f, 2.4f), C(0.3f, 0.36f, 0.42f));
                    for (int i = 0; i < 4; i++) Disc(c + new Vector2(x, -0.85f + i * 0.57f), 0.42f, C(0.25f + 0.05f * i, 0.8f, 0.35f), 4);
                }
                break;
            case "drill":
                Block(c + new Vector2(L, 0), new Vector2(0.7f, 2.1f), metal);
                Block(c + new Vector2(L, s * 0.85f), new Vector2(0.7f, 0.28f), C(1f, 0.8f, 0.15f), 4);
                Block(c + new Vector2(R, s * -0.3f), new Vector2(0.8f, 0.8f), C(0.7f, 0.92f, 1f));
                Block(c + new Vector2(R - 0.1f, s * 0.6f), new Vector2(0.6f, 0.6f), C(0.8f, 0.95f, 1f));
                break;
            case "kitchen":
                Block(c + new Vector2(L, 0), new Vector2(0.65f, 2.2f), C(0.8f, 0.72f, 0.55f));
                Block(c + new Vector2(R, s * 0.4f), new Vector2(0.7f, 1.0f), light);
                Disc(c + new Vector2(L, s * 0.5f), 0.4f, C(0.2f, 0.2f, 0.24f), 4);
                break;
            case "command":
                foreach (float x in new[] { L, R })
                {
                    Block(c + new Vector2(x, 0), new Vector2(0.65f, 2.2f), C(0.22f, 0.26f, 0.36f));
                    Block(c + new Vector2(x, s * 0.4f), new Vector2(0.4f, 0.8f), C(0.35f, 0.9f, 1f), 4);
                    Block(c + new Vector2(x, s * -0.7f), new Vector2(0.4f, 0.4f), C(1f, 0.5f, 0.4f), 4);
                }
                break;
            case "build":
                for (int i = 0; i < 3; i++) Block(c + new Vector2(L, -0.75f + i * 0.75f), new Vector2(0.7f, 0.65f), C(0.65f, 0.5f, 0.3f));
                Block(c + new Vector2(R, 0.4f), new Vector2(0.8f, 0.8f), C(0.68f, 0.52f, 0.32f));
                Block(c + new Vector2(R, -0.5f), new Vector2(0.65f, 0.65f), C(0.6f, 0.46f, 0.28f));
                break;
            case "airlock":
                foreach (float x in new[] { L, R }) Block(c + new Vector2(x, 0), new Vector2(0.55f, 2.2f), metal);
                Block(c + new Vector2(0, s * 0.95f), new Vector2(1.4f, 0.2f), C(1f, 0.8f, 0.2f), 3);
                break;
        }
    }

    // ------------------------------------------------------------------ astronaut (friendly cartoon crew member)
    void BuildAstronaut()
    {
        astro = new GameObject("Astronaut").transform; astro.SetParent(world, false); astro.localPosition = Vector2.zero;
        Quad("Shadow", astro, new Vector2(0, -0.62f), new Vector2(0.95f, 0.28f), C(0, 0, 0, 0.45f), 18, circle);
        rig = new GameObject("Rig").transform; rig.SetParent(astro, false); rig.localScale = new Vector3(0.66f, 0.66f, 1);
        var white = C(0.96f, 0.97f, 1f); var shade = C(0.78f, 0.82f, 0.9f); var grey = C(0.55f, 0.6f, 0.7f);
        // legs + boots
        legL = Slab("LegL", rig, new Vector2(-0.2f, -0.68f), new Vector2(0.3f, 0.5f), shade, 19).transform;
        legR = Slab("LegR", rig, new Vector2(0.2f, -0.68f), new Vector2(0.3f, 0.5f), shade, 19).transform;
        Slab("BootL", legL, new Vector2(0, -0.27f), new Vector2(0.38f, 0.2f), grey, 19);
        Slab("BootR", legR, new Vector2(0, -0.27f), new Vector2(0.38f, 0.2f), grey, 19);
        // backpack + torso
        Slab("Pack", rig, new Vector2(0, -0.12f), new Vector2(1.0f, 0.78f), C(0.62f, 0.7f, 0.82f), 19);
        Slab("Torso", rig, new Vector2(0, -0.2f), new Vector2(0.72f, 0.78f), white, 20);
        Slab("Belt", rig, new Vector2(0, -0.47f), new Vector2(0.74f, 0.1f), C(0.35f, 0.75f, 0.95f), 21);
        Slab("Chest", rig, new Vector2(0, -0.1f), new Vector2(0.34f, 0.24f), grey, 21);
        Quad("ChestLight", rig, new Vector2(0.08f, -0.1f), new Vector2(0.09f, 0.09f), C(0.3f, 1f, 0.9f), 22, circle);
        Quad("ChestLight2", rig, new Vector2(-0.08f, -0.1f), new Vector2(0.09f, 0.09f), C(1f, 0.7f, 0.3f), 22, circle);
        // arms + hands
        armL = Slab("ArmL", rig, new Vector2(-0.5f, -0.2f), new Vector2(0.22f, 0.56f), white, 21).transform;
        armR = Slab("ArmR", rig, new Vector2(0.5f, -0.2f), new Vector2(0.22f, 0.56f), white, 21).transform;
        Quad("HandL", armL, new Vector2(0, -0.3f), new Vector2(0.2f, 0.2f), C(1f, 0.8f, 0.65f), 21, circle);
        Quad("HandR", armR, new Vector2(0, -0.3f), new Vector2(0.2f, 0.2f), C(1f, 0.8f, 0.65f), 21, circle);
        Quad("StripeL", armL, new Vector2(0, 0.05f), new Vector2(0.23f, 0.06f), C(0.3f, 0.3f, 0.35f), 22);
        Quad("StripeR", armR, new Vector2(0, 0.05f), new Vector2(0.23f, 0.06f), C(0.3f, 0.3f, 0.35f), 22);
        // helmet with headphones
        Quad("Helmet", rig, new Vector2(0, 0.5f), new Vector2(1.12f, 1.12f), shade, 22, circle);
        Quad("HelmetTop", rig, new Vector2(0, 0.54f), new Vector2(1.05f, 1.05f), white, 22, circle);
        Quad("Ear L", rig, new Vector2(-0.54f, 0.5f), new Vector2(0.3f, 0.34f), white, 22, circle);
        Quad("Ear R", rig, new Vector2(0.54f, 0.5f), new Vector2(0.3f, 0.34f), white, 22, circle);
        Quad("EarLightL", rig, new Vector2(-0.6f, 0.5f), new Vector2(0.11f, 0.11f), C(0.3f, 0.9f, 1f), 23, circle);
        Quad("EarLightR", rig, new Vector2(0.6f, 0.5f), new Vector2(0.11f, 0.11f), C(0.3f, 0.9f, 1f), 23, circle);
        Slab("HelmetLight", rig, new Vector2(0.0f, 1.0f), new Vector2(0.3f, 0.09f), C(0.3f, 0.7f, 1f), 23);
        // visor glass, face, hair
        Quad("Glass", rig, new Vector2(0, 0.48f), new Vector2(0.86f, 0.84f), C(0.16f, 0.2f, 0.34f), 24, circle);
        Quad("Hair", rig, new Vector2(0, 0.62f), new Vector2(0.66f, 0.56f), C(0.45f, 0.27f, 0.15f), 25, circle);
        Quad("Face", rig, new Vector2(0, 0.46f), new Vector2(0.64f, 0.6f), C(1f, 0.8f, 0.66f), 26, circle);
        Quad("Fringe", rig, new Vector2(-0.1f, 0.7f), new Vector2(0.4f, 0.2f), C(0.45f, 0.27f, 0.15f), 27, circle);
        // eyes: big blue eyes with a highlight
        eyeL = Quad("EyeL", rig, new Vector2(-0.14f, 0.47f), new Vector2(0.2f, 0.22f), Color.white, 27, circle).transform;
        eyeR = Quad("EyeR", rig, new Vector2(0.14f, 0.47f), new Vector2(0.2f, 0.22f), Color.white, 27, circle).transform;
        pupilL = Quad("IrisL", eyeL, Vector2.zero, new Vector2(0.78f, 0.82f), C(0.2f, 0.5f, 0.9f), 28, circle).transform;
        pupilR = Quad("IrisR", eyeR, Vector2.zero, new Vector2(0.78f, 0.82f), C(0.2f, 0.5f, 0.9f), 28, circle).transform;
        Quad("PupilL", pupilL, Vector2.zero, new Vector2(0.5f, 0.5f), C(0.05f, 0.1f, 0.25f), 29, circle);
        Quad("PupilR", pupilR, Vector2.zero, new Vector2(0.5f, 0.5f), C(0.05f, 0.1f, 0.25f), 29, circle);
        Quad("ShineL", pupilL, new Vector2(0.18f, 0.2f), new Vector2(0.28f, 0.28f), Color.white, 30, circle);
        Quad("ShineR", pupilR, new Vector2(0.18f, 0.2f), new Vector2(0.28f, 0.28f), Color.white, 30, circle);
        Quad("CheekL", rig, new Vector2(-0.24f, 0.33f), new Vector2(0.13f, 0.08f), C(1f, 0.55f, 0.5f, 0.55f), 27, circle);
        Quad("CheekR", rig, new Vector2(0.24f, 0.33f), new Vector2(0.13f, 0.08f), C(1f, 0.55f, 0.5f, 0.55f), 27, circle);
        Slab("Smile", rig, new Vector2(0, 0.28f), new Vector2(0.2f, 0.07f), C(0.65f, 0.2f, 0.2f), 27);
        Quad("GlassShine", rig, new Vector2(-0.26f, 0.72f), new Vector2(0.2f, 0.12f), C(1, 1, 1, 0.28f), 31, circle);
        // shield bubble used when the crew shelters from a solar storm
        shieldBubble = Quad("Shield", astro, new Vector2(0, 0.1f), new Vector2(2.2f, 2.2f), C(0.3f, 0.7f, 1f, 0f), 17, circle);
    }

    void AnimateAstronaut()
    {
        if (rig == null) return;
        if (moving) walkPhase += Time.deltaTime * 13f; else walkPhase = 0f;
        float sw = moving ? Mathf.Sin(walkPhase) : 0f;
        legL.localPosition = new Vector3(-0.2f, -0.68f + 0.1f * sw, 0);
        legR.localPosition = new Vector3(0.2f, -0.68f - 0.1f * sw, 0);
        armL.localPosition = new Vector3(-0.5f, -0.2f - 0.08f * sw, 0);
        armR.localPosition = new Vector3(0.5f, -0.2f + 0.08f * sw, 0);
        float idle = Mathf.Sin(Time.time * 2.2f) * 0.015f;
        rig.localPosition = new Vector3(0, moving ? Mathf.Abs(sw) * 0.07f : idle, 0);
        rig.localScale = new Vector3(0.66f, 0.66f + (moving ? 0 : idle), 1);
        // eyes look where the astronaut walks
        float look = 0.1f * facing;
        pupilL.localPosition = new Vector3(look, 0, 0); pupilR.localPosition = new Vector3(look, 0, 0);
        if (moving && Time.time > nextPuff) { nextPuff = Time.time + 0.18f; Puff((Vector2)astro.localPosition + new Vector2(-facing * 0.15f, -0.55f)); }
        moving = false;
    }
    float nextPuff;

    void AnimateWorld()
    {
        foreach (var pt in pulses) { float k = 0.9f + 0.12f * Mathf.Sin(Time.time * 3f + pt.position.x); pt.localScale = new Vector3(0.9f * k, 0.9f * k, 1); }
        for (int i = 0; i < hallLights.Count; i++)
        {
            float x = Mathf.Repeat(i * 1.43f + Time.time * 1.4f, 19f) - 9.5f;
            hallLights[i].localPosition = new Vector3(x, hallLights[i].localPosition.y, 0);
        }
        // sliding doors open when the astronaut is near
        Vector2 ap = astro.localPosition;
        foreach (var r in rooms.Values)
        {
            bool open = Vector2.Distance(ap, r.doorPos) < 1.8f;
            r.doorOpen = Mathf.MoveTowards(r.doorOpen, open ? 1f : 0f, Time.deltaTime * 3.5f);
            float off = 0.37f + 0.6f * r.doorOpen;
            r.doorL.localPosition = new Vector3(r.doorPos.x - off, r.doorPos.y, 0);
            r.doorR.localPosition = new Vector3(r.doorPos.x + off, r.doorPos.y, 0);
        }
        for (int i = 0; i < stars.Count; i++)
        {
            var c = stars[i].color; c.a = 0.35f + 0.5f * Mathf.PerlinNoise(i * 3.1f, Time.time * 0.6f); stars[i].color = c;
        }
        if (planetRoot != null) planetRoot.localPosition = -(Vector2)astro.localPosition * 0.04f;
    }

    void SetPlanet(string s)
    {
        bool moon = s == "moon";
        planetBody.color = moon ? C(0.62f, 0.65f, 0.72f) : C(0.78f, 0.38f, 0.22f);
        planetRim.color = moon ? C(0.5f, 0.7f, 1f, 0.3f) : C(1f, 0.55f, 0.3f, 0.35f);
    }
}
