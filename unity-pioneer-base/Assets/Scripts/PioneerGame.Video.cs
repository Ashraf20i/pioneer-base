// Pioneer Base 3D - PioneerGame.Video.cs
// Automatic 30-second demo video. Started with -pbvideo (Unity editor, batchmode with graphics):
// the game plays the DEMO_SCRIPT.md story by itself and writes frames (30 fps, English captions)
// to <project>/Video/frames. FFmpeg then turns them into an MP4.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame
{
    bool video, recording; int vFrame; string vDir; RenderTexture vRT; Text vCaption; GameObject vBand, vEndCard;

    void Awake()
    {
        try { video = Environment.GetCommandLineArgs().Any(a => a == "-pbvideo"); } catch (Exception) { video = false; }
        if (video) StartCoroutine(VideoRun());
    }

    void VideoUI()
    {
        var band = Panel("VCaption", canvas.transform, new Vector2(0, 0.075f), new Vector2(1, 0.165f), new Color(0, 0, 0, 0.62f));
        band.GetComponent<Image>().sprite = sprFade; band.GetComponent<Image>().raycastTarget = false; vBand = band.gameObject;
        vCaption = TextUI("T", band, Vector2.zero, Vector2.one, 30, TextAnchor.MiddleCenter, Color.white, fontBold);
        var end = Panel("VEnd", canvas.transform, Vector2.zero, Vector2.one, new Color(0.02f, 0.04f, 0.08f, 1f)); vEndCard = end.gameObject;
        var t1 = TextUI("A", end, new Vector2(0, 0.55f), new Vector2(1, 0.75f), 64, TextAnchor.MiddleCenter, Color.white, fontBold); t1.text = "Pioneer Base";
        var t2 = TextUI("B", end, new Vector2(0, 0.42f), new Vector2(1, 0.55f), 26, TextAnchor.MiddleCenter, UiAccent, font); t2.text = "Try again. Play free in your browser.";
        var t3 = TextUI("C", end, new Vector2(0, 0.32f), new Vector2(1, 0.42f), 22, TextAnchor.MiddleCenter, UiText, fontMono); t3.text = "ashraf20i.github.io/pioneer-base";
        var t4 = TextUI("D", end, new Vector2(0, 0.06f), new Vector2(1, 0.14f), 13, TextAnchor.MiddleCenter, UiMuted, fontMono);
        t4.text = GameData.SourcesNote.ToUpper();
        vEndCard.SetActive(false); Caption(null);
    }
    void Caption(string s) { vBand.SetActive(!string.IsNullOrEmpty(s)); vCaption.text = s ?? ""; }

    // record n frames while the game keeps running
    IEnumerator Rec(int n, string caption)
    {
        Caption(caption);
        for (int i = 0; i < n; i++)
        {
            yield return new WaitForEndOfFrame();
            cam.Render();
            RenderTexture.active = vRT;
            var tex = new Texture2D(vRT.width, vRT.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, vRT.width, vRT.height), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(vDir, "f" + (vFrame++).ToString("0000") + ".png"), tex.EncodeToPNG()); Destroy(tex);
        }
    }

    IEnumerator VideoRun()
    {
        yield return null; yield return null;   // let Start() build the scene
        Time.captureFramerate = 30; masterVol = 0f;
        vDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Video", "frames");
        if (Directory.Exists(vDir)) Directory.Delete(vDir, true); Directory.CreateDirectory(vDir);
        vRT = new RenderTexture(1280, 720, 24); cam.targetTexture = vRT;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 0.35f;
        VideoUI();
        yield return Frames(30);

        // 1. title (2.5 s)
        yield return Rec(75, "Pioneer Base: run a Moon base for 30 days.");

        // 2. Moon outpost, opening flight (4 s)
        DismissModal(); StartGame("moon");
        yield return Rec(120, "Keep 4 astronauts alive: air, water, food, power, radiation.");
        while (mode == Mode.Cine) yield return null;

        // the player builds only solar arrays, no reactor; days 1 to 10 pass off camera
        var bot = Players.Naive();
        while (st.day < 11 && !st.over) { DismissModal(); plan = bot(st); var rec = Sim.RunDay(st, plan); AfterRun(rec); }
        DismissModal(); moonDayF = st.day - 0.5f; Refresh(); camYaw = 25f; camPitch = 24f; camDist = 26f; SnapCamera();
        yield return Frames(10);
        yield return Rec(75, "Day 11: a 15-day night begins. Solar panels make no power.");

        // 3. the night: the battery empties, systems shut down (5 s)
        plan = bot(st); StartCoroutine(EndSol());
        yield return Rec(150, "No reactor. The battery empties. Systems shut down, one by one.");
        while (mode == Mode.Cine) yield return null;

        // days pass until the last one, which is recorded with the evacuation
        while (!st.over)
        {
            DismissModal(); plan = bot(st);
            var c = st.Clone(); Sim.RunDay(c, bot(c)); if (c.over) break;
            var r = Sim.RunDay(st, plan); AfterRun(r);
        }
        DismissModal(); moonDayF = st.day; Refresh(); SnapCamera();
        plan = bot(st); StartCoroutine(EndSol());
        yield return Rec(120, "Oxygen runs out. Medical evacuation.");
        int g = 0; while (!(modalRoot.activeSelf && mode == Mode.Modal) && g++ < 2000) yield return null;
        yield return Frames(5);

        // 4. the debrief (5 s)
        yield return Rec(150, "The debrief shows the exact chain of decisions.");

        // 5. bonus: hard mode in 3D (3 s)
        DismissModal(); StartHard();
        yield return Rec(90, "Hard mode: crash 40 km away. Drive, and solve real physics and chemistry.");

        // 6. end card (2 s)
        Caption(null); vEndCard.SetActive(true);
        yield return Rec(60, null);

        Debug.Log("[VIDEO] frames written: " + vFrame);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit();
#endif
    }
}
