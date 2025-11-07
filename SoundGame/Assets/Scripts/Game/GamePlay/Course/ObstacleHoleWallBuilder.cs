// WHY:
// - 実ジオメッシュに穴は切らない。上下2枚の実壁(箱Collider)で通過可否を担保し、
//   側面(±X面)には「丸穴に見えるフェイス(Quad+Cutout)」を載せるだけで軽量に表現。
// - さらに、丸穴の左右(±Z)はフィラーQuadで不透明に塞ぎ、裏側にはBackFillを置いて
//   透けを完全に防ぐ（どの角度からも“穴以外は見えない”）。
// - マテリアル未指定でも動くよう、ランタイムで円形アルファのテクスチャを生成。
// - 既存のAPIに最小限で拡張：holeFaceMaterialと物理マテリアルを任意指定可能。

using UnityEngine;

namespace Game.Gameplay.Course
{
    public static class ObstacleHoleWallBuilder
    {
        public struct Params
        {
            public float wallWidth;    // X方向の厚み（コースがX進行なら板の厚み）
            public float wallHeight;   // 全体の高さ
            public float wallDepth;    // Z方向の幅
            public float holeCenterY;  // 穴の中心高さ（ワールドY）
            public float holeSize;     // 穴の直径（Y方向）
            public Material wallMaterial;          // 実壁/フィラー/裏板に使う色
            public Material holeFaceMaterial;      // 丸穴フェイス用（Cutout推奨・任意）
            public PhysicMaterial wallPhysicMat;   // 実壁の反発/摩擦（任意）
        }

        public static GameObject Build(GameObject parent, in Params p)
        {
            var root = new GameObject("ObstacleHoleWall");
            if (parent != null) root.transform.SetParent(parent.transform, false);

            float r = Mathf.Max(0.0001f, p.holeSize * 0.5f); // 半径
            float holeHalf = r;

            // ── 1) 実壁（上下2枚：通行制御/衝突）──────────────────────────
            float bottomHeight = Mathf.Clamp(p.holeCenterY - holeHalf, 0f, p.wallHeight);
            float topHeight    = Mathf.Clamp(p.wallHeight - (p.holeCenterY + holeHalf), 0f, p.wallHeight);

            if (bottomHeight > 0.001f)
            {
                var bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottom.name = "WallBottom";
                bottom.transform.SetParent(root.transform, false);
                bottom.transform.localScale    = new Vector3(p.wallWidth, bottomHeight, p.wallDepth);
                bottom.transform.localPosition = new Vector3(0f, bottomHeight * 0.5f, 0f);
                if (p.wallMaterial) bottom.GetComponent<MeshRenderer>().sharedMaterial = p.wallMaterial;
                var bc = bottom.GetComponent<BoxCollider>();
                bc.isTrigger = false;
                bc.sharedMaterial = EnsurePhysic(p.wallPhysicMat);
            }

            if (topHeight > 0.001f)
            {
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "WallTop";
                top.transform.SetParent(root.transform, false);
                top.transform.localScale    = new Vector3(p.wallWidth, topHeight, p.wallDepth);
                top.transform.localPosition = new Vector3(0f, p.wallHeight - topHeight * 0.5f, 0f);
                if (p.wallMaterial) top.GetComponent<MeshRenderer>().sharedMaterial = p.wallMaterial;
                var tc = top.GetComponent<BoxCollider>();
                tc.isTrigger = false;
                tc.sharedMaterial = EnsurePhysic(p.wallPhysicMat);
            }

            // ── 2) 見た目：±X面に丸穴フェイス + 左右フィラー + 裏板（透け防止）────────
            CreateFaceWithFillers(root.transform, p, +1); // +X面（進行方向側など）
            CreateFaceWithFillers(root.transform, p, -1); // -X面（背面）

            // ── 3) 通過判定トリガー（スコア/演出用。通過可否は実壁で担保）─────────
            var trigger = new GameObject("GateTrigger");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, p.holeCenterY, 0f);
            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(p.wallWidth * 0.9f, p.holeSize, p.wallDepth * 1.05f);

            return root;
        }

        // ±X面に丸穴フェイス（Cutout）と左右フィラー、裏板を生成
        private static void CreateFaceWithFillers(Transform parent, in Params p, int xSign)
        {
            float epsFace = 0.0006f; // Zファイト/面重なり回避
            float faceX   = (p.wallWidth * 0.5f + epsFace) * xSign;

            // 2-1) 丸穴フェイス：正方形Quadに円アルファ（楕円化防止）
            float faceHeight = p.holeSize * 1.06f; // 上下の段差隠しに少しだけ大きめ
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = xSign > 0 ? "HoleFace_PosX" : "HoleFace_NegX";
            face.transform.SetParent(parent, false);
            face.transform.rotation = Quaternion.Euler(0f, 90f * xSign, 0f); // 法線を±Xへ
            face.transform.localScale    = new Vector3(p.holeSize, faceHeight, 1f); // 正方形で円保持
            face.transform.localPosition = new Vector3(faceX, p.holeCenterY, 0f);
            Object.Destroy(face.GetComponent<Collider>());
            var faceMr = face.GetComponent<MeshRenderer>();
            faceMr.sharedMaterial = p.holeFaceMaterial != null ? p.holeFaceMaterial : CreateCutoutMat(p);

            // 2-2) 裏板：フェイスのすぐ内側に不透明板を置き、透過の先を遮断（中が見えない）
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = xSign > 0 ? "BackFill_PosX" : "BackFill_NegX";
            back.transform.SetParent(parent, false);
            back.transform.rotation = Quaternion.Euler(0f, 90f * xSign, 0f);
            back.transform.localScale    = new Vector3(p.holeSize * 1.02f, faceHeight, 1f);
            float backOffset = (p.wallWidth * 0.5f - 0.001f) * xSign; // 板の内側へ少し
            back.transform.localPosition = new Vector3(backOffset, p.holeCenterY, 0f);
            Object.Destroy(back.GetComponent<Collider>());
            CopyWallLikeMaterial(back, p);

            // 2-3) 左右フィラー：Z方向の穴の“左右”を不透明で塞ぐ（外部が覗けない）
            float remainZ = Mathf.Max(0f, p.wallDepth - p.holeSize);
            float eachZ   = remainZ * 0.5f;
            if (eachZ > 0.0001f)
            {
                float fillerH = faceHeight;

                // 左(Z-)フィラー
                var left = GameObject.CreatePrimitive(PrimitiveType.Quad);
                left.name = xSign > 0 ? "FillerL_PosX" : "FillerL_NegX";
                left.transform.SetParent(parent, false);
                left.transform.rotation = Quaternion.Euler(0f, 90f * xSign, 0f);
                left.transform.localScale    = new Vector3(eachZ, fillerH, 1f);
                float leftZ = -(p.holeSize * 0.5f + eachZ * 0.5f);
                left.transform.localPosition = new Vector3(faceX, p.holeCenterY, leftZ);
                Object.Destroy(left.GetComponent<Collider>());
                CopyWallLikeMaterial(left, p);

                // 右(Z+)フィラー
                var right = GameObject.CreatePrimitive(PrimitiveType.Quad);
                right.name = xSign > 0 ? "FillerR_PosX" : "FillerR_NegX";
                right.transform.SetParent(parent, false);
                right.transform.rotation = Quaternion.Euler(0f, 90f * xSign, 0f);
                right.transform.localScale    = new Vector3(eachZ, fillerH, 1f);
                float rightZ =  (p.holeSize * 0.5f + eachZ * 0.5f);
                right.transform.localPosition = new Vector3(faceX, p.holeCenterY, rightZ);
                Object.Destroy(right.GetComponent<Collider>());
                CopyWallLikeMaterial(right, p);
            }
        }

        // 実壁/フィラー/裏板用の“壁色”マテリアル
        private static void CopyWallLikeMaterial(GameObject go, in Params p)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (p.wallMaterial != null)
            {
                mr.sharedMaterial = p.wallMaterial;
            }
            else
            {
                var sh  = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
                var mat = new Material(sh) { color = new Color(0.75f, 0.75f, 0.75f, 1f) };
                mr.sharedMaterial = mat;
            }
        }

        // 丸穴フェイス用（Cutout系）マテリアルを生成
        private static Material CreateCutoutMat(in Params p)
        {
            var sh = Shader.Find("Unlit/Transparent Cutout")
                  ?? Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse")
                  ?? Shader.Find("Sprites/Default"); // 最終手段（アルファテスト無しでもOK）

            var mat = new Material(sh);
            mat.color = p.wallMaterial ? p.wallMaterial.color : new Color(0.75f, 0.75f, 0.75f, 1f);

            // 中央透明・外周不透明の円アルファ
            mat.mainTexture = GenerateCircleAlpha(512, 2f);

            if (sh != null && (sh.name.Contains("Cutout") || sh.name.Contains("Alpha")))
            {
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            return mat;
        }

        // ランタイム生成：正方形テクスチャに中央透明の円を描く（ソフトエッジ）
        private static Texture2D GenerateCircleAlpha(int size, float edgePx = 2f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            var cols = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            float r = c * 0.98f; // わずかに内側まで

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(dist <= r - edgePx ? 0 :
                                dist >= r + edgePx ? 255 :
                                Mathf.Lerp(0f, 255f, (dist - (r - edgePx)) / (edgePx * 2f)));
                cols[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(cols);
            tex.Apply(false, true);
            return tex;
        }

        // 物理マテリアル（未指定なら反発寄りのRuntime版を用意）
        private static PhysicMaterial EnsurePhysic(PhysicMaterial src)
        {
            if (src != null) return src;
            var pm = new PhysicMaterial("WallPhysic_Runtime")
            {
                bounciness = 0.45f,
                dynamicFriction = 0.0f,
                staticFriction  = 0.0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine   = PhysicMaterialCombine.Maximum
            };
            return pm;
        }
    }
}
