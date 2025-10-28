// WHY:
// - 3Dメッシュに実際の穴を切るのは重い＆複雑。代わりに「上下2枚の壁」で“穴”を構成する。
// - これならCollider/Rendererの組み合わせだけで高速・堅牢に実現でき、サイズ調整も容易。
// - ランタイム生成に統一し、Prefab依存を減らす（見た目のマテリアルは差し替え前提）。
using UnityEngine;

namespace Game.Gameplay.Course
{
    public static class ObstacleHoleWallBuilder
    {
        public struct Params
        {
            public float wallWidth;    // X方向の厚み（奥行きはZ）
            public float wallHeight;   // 全体の高さ（MinHeight〜MaxHeight程度）
            public float wallDepth;    // Z方向の幅
            public float holeCenterY;  // 穴の中心高さ
            public float holeSize;     // 穴の縦サイズ（通過可否の難易度）
            public Material wallMaterial;
        }

        public static GameObject Build(GameObject parent, in Params p)
        {
            var root = new GameObject("ObstacleHoleWall");
            if (parent != null) root.transform.SetParent(parent.transform, false);

            float half = p.wallHeight * 0.5f;
            float holeHalf = p.holeSize * 0.5f;

            // 下壁の高さ：穴中心から下端まで
            float bottomHeight = Mathf.Clamp(p.holeCenterY - holeHalf, 0f, p.wallHeight);
            // 上壁の高さ：上端から穴中心まで
            float topHeight = Mathf.Clamp(p.wallHeight - (p.holeCenterY + holeHalf), 0f, p.wallHeight);

            // 下ブロック
            if (bottomHeight > 0.001f)
            {
                var bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottom.name = "WallBottom";
                bottom.transform.SetParent(root.transform, false);
                bottom.transform.localScale = new Vector3(p.wallWidth, bottomHeight, p.wallDepth);
                bottom.transform.localPosition = new Vector3(0f, bottomHeight * 0.5f, 0f);
                if (p.wallMaterial) bottom.GetComponent<MeshRenderer>().sharedMaterial = p.wallMaterial;
                var bc = bottom.GetComponent<BoxCollider>(); bc.isTrigger = false;
            }

            // 上ブロック
            if (topHeight > 0.001f)
            {
                var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "WallTop";
                top.transform.SetParent(root.transform, false);
                top.transform.localScale = new Vector3(p.wallWidth, topHeight, p.wallDepth);
                top.transform.localPosition = new Vector3(0f, p.wallHeight - topHeight * 0.5f, 0f);
                if (p.wallMaterial) top.GetComponent<MeshRenderer>().sharedMaterial = p.wallMaterial;
                var tc = top.GetComponent<BoxCollider>(); tc.isTrigger = false;
            }

            // 穴中央にトリガー（スコア用/判定用。実際は通行自体は壁構造で制御される）
            var trigger = new GameObject("GateTrigger");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, p.holeCenterY, 0f);
            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(p.wallWidth * 0.9f, p.holeSize, p.wallDepth * 1.05f);

            return root;
        }
    }
}
