// Pioneer Base 3D - PioneerGame.Rover.cs
// A detailed pressurized rover, inspired by real exploration-rover concepts: pressurized cabin with a wide
// canopy, rear suit ports, six lugged wheels on suspension arms, roof solar array, high-gain antenna,
// gold thermal blankets and radiators. Built from code: procedural tire meshes and generated textures.
using System.Collections.Generic;
using UnityEngine;

public partial class PioneerGame
{
    Material matPanel, matCells, matTire;

    // ------------------------------------------------------------------ generated textures
    Material PanelMat()
    {
        if (matPanel != null) return matPanel;
        const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGB24, true); t.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float v = 0.93f - Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.035f;
            bool seamH = y % 43 == 0, seamV = (x + (y / 43) * 21) % 64 == 0;
            if (seamH || seamV) v *= 0.8f;
            bool rivet = ((y % 43 == 4) || (y % 43 == 39)) && (x % 8 == 3);
            if (rivet) v *= 0.84f;
            px[y * n + x] = new Color(v, v * 0.995f, v * 0.985f);
        }
        t.SetPixels(px); t.Apply(true);
        matPanel = new Material(shLit); matPanel.mainTexture = t; matPanel.color = Color.white; matPanel.mainTextureScale = new Vector2(2f, 1.5f);
        return matPanel;
    }
    Material CellsMat()
    {
        if (matCells != null) return matCells;
        const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGB24, true); t.wrapMode = TextureWrapMode.Repeat;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            bool line = x % 8 == 0 || y % 16 == 0; float sheen = 0.06f * (1f - Mathf.Abs((x + y) / (float)n - 1f));
            t.SetPixel(x, y, line ? new Color(0.55f, 0.6f, 0.7f) : new Color(0.05f + sheen, 0.09f + sheen, 0.3f + sheen));
        }
        t.Apply(true);
        matCells = new Material(shLit); matCells.mainTexture = t; matCells.color = Color.white; matCells.mainTextureScale = new Vector2(3f, 2f);
        return matCells;
    }

    // ------------------------------------------------------------------ procedural tire (axle along X)
    static void Quad2(List<Vector3> v, List<int> tr, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {   // double-sided flat quad, so the winding never hides a face
        int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        tr.Add(i); tr.Add(i + 1); tr.Add(i + 2); tr.Add(i); tr.Add(i + 2); tr.Add(i + 3);
        i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        tr.Add(i); tr.Add(i + 2); tr.Add(i + 1); tr.Add(i); tr.Add(i + 3); tr.Add(i + 2);
    }
    Mesh tireMesh;
    Mesh TireMesh()
    {
        if (tireMesh != null) return tireMesh;
        const int N = 40; const float R = 0.56f, lug = 0.055f, W = 0.44f, Ri = 0.37f;
        var v = new List<Vector3>(); var tr = new List<int>();
        System.Func<int, float> rad = i => (i % 4 < 2) ? R : R - lug;
        System.Func<float, float, float, Vector3> P3 = (x, r, a) => new Vector3(x, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        for (int i = 0; i < N; i++)
        {
            float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N, r = rad(i), rn = rad((i + 1) % N);
            float sh = (i % 4 < 2) ? 0.06f : -0.06f; // chevron offset on the tread
            Quad2(v, tr, P3(-W / 2, r, a0 + sh * 0.1f), P3(W / 2, r, a0 - sh * 0.1f), P3(W / 2, r, a1 - sh * 0.1f), P3(-W / 2, r, a1 + sh * 0.1f));
            if (Mathf.Abs(r - rn) > 0.001f) Quad2(v, tr, P3(-W / 2, r, a1), P3(W / 2, r, a1), P3(W / 2, rn, a1), P3(-W / 2, rn, a1));
            foreach (float x in new[] { -W / 2, W / 2 }) Quad2(v, tr, P3(x, Ri, a0), P3(x, r, a0), P3(x, r, a1), P3(x, Ri, a1));
        }
        tireMesh = new Mesh(); tireMesh.SetVertices(v); tireMesh.SetTriangles(tr, 0); tireMesh.RecalculateNormals(); tireMesh.RecalculateBounds();
        return tireMesh;
    }
    Transform Wheel(Transform parent, Vector3 pos, float side)
    {
        var w = Empty("Wheel", parent, pos);
        if (matTire == null) matTire = Lit(0.16f, 0.16f, 0.18f);
        var tire = new GameObject("Tire"); tire.transform.SetParent(w, false);
        tire.AddComponent<MeshFilter>().sharedMesh = TireMesh(); tire.AddComponent<MeshRenderer>().sharedMaterial = matTire;
        var rim = Lit(0.72f, 0.74f, 0.78f); var dark = Lit(0.3f, 0.32f, 0.36f);
        P(PrimitiveType.Cylinder, w, Vector3.zero, new Vector3(0.75f, 0.2f, 0.75f), dark, new Vector3(0, 0, 90));
        P(PrimitiveType.Cylinder, w, new Vector3(0.2f * side, 0, 0), new Vector3(0.62f, 0.02f, 0.62f), rim, new Vector3(0, 0, 90));
        for (int k = 0; k < 5; k++)
            P(PrimitiveType.Cube, w, new Vector3(0.22f * side, 0, 0), new Vector3(0.03f, 0.08f, 0.62f), rim, new Vector3(k * 36f, 0, 0));
        P(PrimitiveType.Cylinder, w, new Vector3(0.24f * side, 0, 0), new Vector3(0.2f, 0.04f, 0.2f), Lit(0.95f, 0.5f, 0.15f), new Vector3(0, 0, 90));
        return w;
    }

    // ------------------------------------------------------------------ the rover
    // Returns the root. Fills the wheel list (for spinning), the roof panel pivot and the headlight when asked.
    Transform BuildRoverDetailed(Transform parent, Vector3 pos, float yaw, List<Transform> wheelList, bool withLight)
    {
        var r = Empty("Rover", parent, pos); r.localEulerAngles = new Vector3(0, yaw, 0);
        var body = Empty("Body", r, Vector3.zero);
        var panel = PanelMat(); var grey = Lit(0.55f, 0.58f, 0.64f); var dark = Lit(0.14f, 0.15f, 0.18f); var metal = Lit(0.72f, 0.74f, 0.78f);
        var gold = Lit(0.86f, 0.66f, 0.28f); var orange = Lit(0.95f, 0.5f, 0.15f); var glass = Glass(C(0.06f, 0.11f, 0.18f, 0.9f));

        // chassis
        P(PrimitiveType.Cube, body, new Vector3(0, 0.88f, 0), new Vector3(2.15f, 0.3f, 4.5f), dark);
        P(PrimitiveType.Cube, body, new Vector3(0, 0.72f, 2.38f), new Vector3(2.3f, 0.22f, 0.24f), grey);
        P(PrimitiveType.Cube, body, new Vector3(0, 0.72f, -2.38f), new Vector3(2.3f, 0.22f, 0.24f), grey);
        foreach (float x in new[] { -0.55f, 0.55f }) P(PrimitiveType.Cylinder, body, new Vector3(x, 0.72f, 2.56f), new Vector3(0.12f, 0.06f, 0.12f), metal, new Vector3(90, 0, 0));

        // pressurized cabin: long rounded hull, wide canopy at the front
        P(PrimitiveType.Capsule, body, new Vector3(0, 1.92f, -0.1f), new Vector3(2.05f, 1.75f, 1.75f), panel, new Vector3(90, 0, 0));
        P(PrimitiveType.Cube, body, new Vector3(0, 1.55f, -0.1f), new Vector3(2.08f, 0.14f, 3.2f), orange);
        P(PrimitiveType.Sphere, body, new Vector3(0, 2.0f, 1.32f), new Vector3(1.75f, 1.25f, 1.05f), glass, null, false);
        P(PrimitiveType.Cube, body, new Vector3(0, 2.58f, 1.22f), new Vector3(1.8f, 0.08f, 0.5f), panel);
        foreach (float x in new[] { -0.82f, 0.82f })
        {
            P(PrimitiveType.Cube, body, new Vector3(x, 2.0f, 1.42f), new Vector3(0.07f, 0.95f, 0.08f), dark);
            P(PrimitiveType.Cube, body, new Vector3(x * 1.2f, 2.08f, 0.35f), new Vector3(0.03f, 0.38f, 0.75f), glass);
            P(PrimitiveType.Cube, body, new Vector3(x * 1.2f, 2.08f, -0.55f), new Vector3(0.03f, 0.38f, 0.55f), glass);
            P(PrimitiveType.Cylinder, body, new Vector3(x * 0.98f, 2.72f, -0.1f), new Vector3(0.05f, 1.25f, 0.05f), metal, new Vector3(90, 0, 0));
        }
        // rear suit ports
        foreach (float x in new[] { -0.48f, 0.48f })
        {
            P(PrimitiveType.Cylinder, body, new Vector3(x, 1.95f, -1.86f), new Vector3(0.78f, 0.06f, 0.78f), grey, new Vector3(90, 0, 0));
            P(PrimitiveType.Cylinder, body, new Vector3(x, 1.95f, -1.9f), new Vector3(0.6f, 0.04f, 0.6f), Lit(0.82f, 0.84f, 0.88f), new Vector3(90, 0, 0));
        }
        // gold thermal blankets, radiators, rear equipment
        foreach (float x in new[] { -0.7f, 0.7f }) P(PrimitiveType.Cube, body, new Vector3(x, 1.28f, -1.95f), new Vector3(0.62f, 0.42f, 0.5f), gold);
        foreach (float x in new[] { -1.12f, 1.12f }) P(PrimitiveType.Cube, body, new Vector3(x, 1.95f, -1.15f), new Vector3(0.04f, 0.7f, 0.9f), Lit(0.88f, 0.9f, 0.94f));

        // roof solar array (tilts up when the rover camps)
        var pv = Empty("Panel", body, new Vector3(0, 2.86f, -1.5f));
        P(PrimitiveType.Cube, pv, new Vector3(0, 0, 0.85f), new Vector3(2.25f, 0.05f, 1.7f), metal);
        P(PrimitiveType.Cube, pv, new Vector3(0, 0.03f, 0.85f), new Vector3(2.15f, 0.02f, 1.6f), CellsMat());
        P(PrimitiveType.Cube, body, new Vector3(0, 2.76f, -1.5f), new Vector3(0.5f, 0.12f, 0.12f), dark);

        // antennas
        P(PrimitiveType.Cylinder, body, new Vector3(0.72f, 3.2f, -1.1f), new Vector3(0.06f, 0.5f, 0.06f), metal);
        var dish = Empty("Dish", body, new Vector3(0.72f, 3.75f, -1.1f)); dish.localEulerAngles = new Vector3(-35, 30, 0);
        P(PrimitiveType.Sphere, dish, Vector3.zero, new Vector3(0.75f, 0.75f, 0.16f), Lit(0.92f, 0.92f, 0.95f));
        P(PrimitiveType.Cylinder, dish, new Vector3(0, 0, 0.22f), new Vector3(0.04f, 0.2f, 0.04f), dark, new Vector3(90, 0, 0));
        P(PrimitiveType.Cylinder, body, new Vector3(-0.8f, 3.3f, -1.45f), new Vector3(0.03f, 0.55f, 0.03f), dark);
        P(PrimitiveType.Sphere, body, new Vector3(-0.8f, 3.88f, -1.45f), Vector3.one * 0.1f, Unlit(C(1f, 0.25f, 0.2f)), null, false);

        // lights
        for (int k = 0; k < 4; k++) P(PrimitiveType.Cube, body, new Vector3(-0.6f + k * 0.4f, 1.05f, 2.31f), new Vector3(0.26f, 0.1f, 0.05f), Unlit(C(1f, 0.97f, 0.85f)), null, false);
        foreach (float x in new[] { -1.06f, 1.06f }) P(PrimitiveType.Cube, body, new Vector3(x, 1.05f, 2.2f), new Vector3(0.05f, 0.08f, 0.16f), Unlit(C(1f, 0.55f, 0.15f)), null, false);

        // six wheels on suspension arms, with fenders
        foreach (float x in new[] { -1.32f, 1.32f }) foreach (float z in new[] { -1.55f, 0f, 1.55f })
        {
            float s = Mathf.Sign(x);
            var arm = P(PrimitiveType.Cube, r, new Vector3(x * 0.84f, 0.78f, z), new Vector3(0.5f, 0.1f, 0.16f), grey); arm.transform.localEulerAngles = new Vector3(0, 0, s * -14f);
            P(PrimitiveType.Cube, r, new Vector3(x * 0.97f, 1.2f, z), new Vector3(0.5f, 0.05f, 0.95f), grey);
            var w = Wheel(r, new Vector3(x, 0.56f, z), s);
            if (wheelList != null) wheelList.Add(w);
        }
        if (withLight)
        {
            var lg = new GameObject("Headlights"); lg.transform.SetParent(body, false); lg.transform.localPosition = new Vector3(0, 1.3f, 2.4f); lg.transform.localEulerAngles = new Vector3(12, 0, 0);
            roverLamp = lg.AddComponent<Light>(); roverLamp.type = LightType.Spot; roverLamp.range = 45f; roverLamp.spotAngle = 70f; roverLamp.color = C(1f, 0.96f, 0.85f); roverLamp.shadows = LightShadows.None;
            roverPanel = pv;
        }
        return r;
    }
}
