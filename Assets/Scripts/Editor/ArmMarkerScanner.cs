using UnityEditor;
using UnityEngine;

public static class ArmMarkerScanner
{
    // เลือก MechArmSet ใน Project แล้วเรียกเมนูนี้ เพื่อสแกนไฟล์ *Mark ทั้งหมดที่ระบุไว้
    [MenuItem("Mech/Scan Arm Markers For Selected ArmSet")]
    static void ScanSelected()
    {
        MechArmSet set = Selection.activeObject as MechArmSet;
        if (set == null) { Debug.LogWarning("เลือก MechArmSet asset ก่อน"); return; }

        foreach (DirectionArmData dir in set.directions)
        {
            ScanTorso(dir);
            foreach (ArmPoseSet p in dir.poses)
            {
                ScanArmPiece(p.left);
                ScanArmPiece(p.right);
            }
        }

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log("สแกนจุดมาร์กเสร็จสำหรับ " + set.name);
    }

    static void ScanTorso(DirectionArmData dir)
    {
        if (dir.torso == null) return;
        Texture2D markTex = FindMarkTexture(dir.torso.texture);
        if (markTex == null) { Debug.LogWarning("ไม่พบไฟล์ Mark ของลำตัว " + dir.torso.name); return; }

        EnsureReadable(markTex);
        int w = markTex.width, h = markTex.height;

        Vector2? green = MarkerDot.Find(markTex, MarkerDot.Kind.Green);
        Vector2? red = MarkerDot.Find(markTex, MarkerDot.Kind.Red);
        // ทิศที่ศอกสองข้างทับกัน (เช่น Left/Right): วาดจุดเดียวแล้วใช้ร่วมกันทั้งสองข้าง
        if (!green.HasValue && red.HasValue) green = red;
        if (!red.HasValue && green.HasValue) red = green;
        float ppu = dir.torso.pixelsPerUnit;

        if (green.HasValue) dir.leftElbowLocal = PixelToLocal(green.Value, w, h, ppu);
        if (red.HasValue) dir.rightElbowLocal = PixelToLocal(red.Value, w, h, ppu);

        var oranges = MarkerDot.FindAll(markTex, MarkerDot.Kind.Orange);
        dir.trailPoints = new System.Collections.Generic.List<GlowPoint>();
        foreach (Vector2 op in oranges)
            dir.trailPoints.Add(new GlowPoint { localPos = PixelToLocal(op, w, h, ppu) });

        // จุดปล่อย missile (ม่วงแดง): แยกซ้าย/ขวาตามตำแหน่งบนจอ (x เทียบกึ่งกลางลำตัว) เรียงบนลงล่าง
        var left = new System.Collections.Generic.List<GlowPoint>();
        var right = new System.Collections.Generic.List<GlowPoint>();
        var all = new System.Collections.Generic.List<GlowPoint>();
        foreach (Vector2 mp in MarkerDot.FindAll(markTex, MarkerDot.Kind.Magenta))
        {
            GlowPoint gp = new GlowPoint { localPos = PixelToLocal(mp, w, h, ppu) };
            all.Add(gp);
            (gp.localPos.x < 0f ? left : right).Add(gp);
        }

        // Left/Right: ตัวปล่อยสองข้างทับกัน วาดชุดเดียวแล้วใช้ร่วมกันทั้งสองข้าง (เหมือนจุดศอก)
        // ทิศอื่นที่จุดอยู่ฝั่งเดียวกันหมดก็ใช้ร่วมกันเช่นกัน
        bool shared = dir.directionName == "Left" || dir.directionName == "Right"
                      || left.Count == 0 || right.Count == 0;
        if (all.Count > 0 && shared)
        {
            left = new System.Collections.Generic.List<GlowPoint>(all);
            right = new System.Collections.Generic.List<GlowPoint>(all);
        }

        left.Sort(ByTop);
        right.Sort(ByTop);
        dir.launcherLeftPoints = left;
        dir.launcherRightPoints = right;
        Debug.Log(dir.directionName + ": จุดปล่อย ซ้าย " + left.Count + " ขวา " + right.Count);

        if (!green.HasValue || !red.HasValue)
            Debug.LogWarning("ลำตัว " + dir.torso.name + " ขาดจุด: " + (green.HasValue ? "" : "เขียว ") + (red.HasValue ? "" : "แดง"));
    }

    static void ScanArmPiece(ArmPieceData piece)
    {
        if (piece == null || piece.sprite == null) return;
        Texture2D realTex = piece.sprite.texture;
        Texture2D markTex = FindMarkTexture(realTex);
        if (markTex == null) { Debug.LogWarning("ไม่พบไฟล์ Mark ของ " + realTex.name); return; }

        EnsureReadable(markTex);
        int w = markTex.width, h = markTex.height;

        Vector2? pivot = MarkerDot.Find(markTex, MarkerDot.Kind.Green);
        if (!pivot.HasValue) pivot = MarkerDot.Find(markTex, MarkerDot.Kind.Red);
        Vector2? muzzle = MarkerDot.Find(markTex, MarkerDot.Kind.Blue);

        if (!pivot.HasValue) { Debug.LogWarning(realTex.name + " ไม่พบจุดหมุน (เขียว/แดง)"); return; }

        // ตั้ง Sprite Pivot ของไฟล์จริงให้ตรงจุดหมุนที่สแกนได้ (Unity pivot: 0,0=ล่างซ้าย, ค่าปกติ 0-1)
        string path = AssetDatabase.GetAssetPath(realTex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning("หา TextureImporter ของ " + realTex.name + " ไม่เจอ"); return; }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(pivot.Value.x / w, 1f - pivot.Value.y / h);

        importer.SetTextureSettings(settings);
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        float ppu = piece.sprite.pixelsPerUnit;

        if (muzzle.HasValue)
        {
            Vector2 deltaPx = muzzle.Value - pivot.Value;   // พิกัดภาพ y ขึ้นบน (จากตัวอ่านแล้ว)
            piece.muzzleLocal = new Vector2(deltaPx.x / ppu, -deltaPx.y / ppu);
        }
    }

    static Vector2 PixelToLocal(Vector2 px, int w, int h, float ppu)
    {
        // ลำตัวใช้ pivot = Center เสมอ (0.5, 0.5)
        float lx = (px.x - w * 0.5f) / ppu;
        float ly = -(px.y - h * 0.5f) / ppu;
        return new Vector2(lx, ly);
    }

    static Texture2D FindMarkTexture(Texture2D real)
    {
        string path = AssetDatabase.GetAssetPath(real);
        string markPath = path.Replace(".png", "Mark.png");
        return AssetDatabase.LoadAssetAtPath<Texture2D>(markPath);
    }

    static void EnsureReadable(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && !imp.isReadable)
        {
            imp.isReadable = true;
            imp.SaveAndReimport();
        }
    }

    static int ByTop(GlowPoint a, GlowPoint b) => b.localPos.y.CompareTo(a.localPos.y);   // สูงกว่าก่อน

    static int ByTopThenX(GlowPoint a, GlowPoint b)
    {
        int c = b.localPos.y.CompareTo(a.localPos.y);
        return c != 0 ? c : a.localPos.x.CompareTo(b.localPos.x);
    }
}