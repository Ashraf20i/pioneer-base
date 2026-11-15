// Pioneer Base 3D - PioneerGame.Shots.cs
// Automatic check: when Unity is started with -pbshots, a scripted player plays full missions and
// saves screenshots to <project>/Shots. Used to verify the game without a person at the keyboard.
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using PioneerBase;

public partial class PioneerGame
{
    float debugStorm = -1f;
    RenderTexture shotRT; string shotDir;

    IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    IEnumerator WaitNotCine() { int g = 0; yield return null; while (mode == Mode.Cine && g++ < 3000) yield return null; yield return Frames(8); }

    void Snap(string name)
    {
        try
        {
            cam.Render();
            RenderTexture.active = shotRT;
            var tex = new Texture2D(shotRT.width, shotRT.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, shotRT.width, shotRT.height), 0, 0); tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG()); Destroy(tex);
            Debug.Log("[SHOT] " + name + "  day=" + (st != null ? st.day : 0) + " mode=" + mode);
        }
        catch (Exception e) { Debug.LogError("[SHOT] failed " + name + ": " + e); }
    }
    void DismissModal() { modalRoot.SetActive(false); if (mode == Mode.Modal) mode = Mode.Play; }

    IEnumerator PlayMission(string s, string tag, Func<GameState, Plan> bot, bool detail)
    {
        DismissModal(); StartGame(s); yield return WaitNotCine(); yield return Frames(20);
        if (detail)
        {
            Snap(tag + "_01_start");
            player.pos = oxyMod.ipos; player.yaw = -90; camYaw = -60; SnapCamera(); yield return Frames(20); Snap(tag + "_02_card");
            camYaw = 35; camPitch = 28; camDist = 30; player.pos = Ground(new Vector3(-4, 0, -20)); SnapCamera(); yield return Frames(20); Snap(tag + "_03_wide");
            Interact("depot"); yield return Frames(4); Snap(tag + "_04_depot"); DismissModal();
            Interact("hab"); yield return Frames(4); Snap(tag + "_05_plan"); DismissModal();
            camYaw = 0; camPitch = 20; camDist = 13; player.pos = Ground(new Vector3(0, 0, -11)); SnapCamera();
        }
        bool stormShot = false, nightShot = false, solarShot = false; int guard = 0;
        while (!st.over && guard++ < 45)
        {
            DismissModal();
            plan = bot(st); Refresh();
            var env = Sim.EnvFor(st, st.day);
            if (detail && st.day == 1) { yield return Frames(10); Snap(tag + "_06_planned"); }
            if (detail && st.day == 3) { camYaw = 150; camPitch = 16; camDist = 24; SnapCamera(); yield return Frames(30); Snap(tag + "_07_day3"); camYaw = 0; camPitch = 20; camDist = 13; }
            if (env.dustStorm && !stormShot && st.day >= st.dust.start + 1) { stormShot = true; yield return Frames(120); Snap(tag + "_08_duststorm"); }
            if (env.solarStorm && !solarShot) { solarShot = true; yield return Frames(60); Snap(tag + "_09_solarstorm"); }
            if (env.night && !nightShot && st.day >= 13) { nightShot = true; yield return Frames(40); Snap(tag + "_10_night"); camYaw = 200; camDist = 22; SnapCamera(); yield return Frames(20); Snap(tag + "_10b_night_wide"); camYaw = 0; camDist = 13; }
            int day = st.day;
            StartCoroutine(EndSol());
            if (detail && day == 2) { yield return Frames(30); Snap(tag + "_11_lapse"); }
            if (st != null && day == 29) { yield return WaitNotCine(); }
            else yield return WaitNotCine();
        }
        yield return Frames(20); Snap(tag + "_12_debrief");
        Debug.Log("[SHOT] mission " + tag + " ended: " + st.ending.type + " day " + st.ending.day + " stars " + st.ending.stars);
    }

    IEnumerator ShotRun()
    {
        Time.captureFramerate = 30;
        shotDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots"); Directory.CreateDirectory(shotDir);
        shotRT = new RenderTexture(1280, 720, 24); cam.targetTexture = shotRT;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 0.35f;
        yield return Frames(60); Snap("00_title");
        yield return PlayMission("mars", "mars", Players.Expert(), true);
        yield return PlayMission("moon", "moon", Players.Expert(), true);
        yield return PlayMission("moon", "fail", Players.Naive(), false);
        // ending cinematics, mid-flight
        Debug.Log("[SHOT] all done");
        yield return Frames(5);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit();
#endif
    }
}
