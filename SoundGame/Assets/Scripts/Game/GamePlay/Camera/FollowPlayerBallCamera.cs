using UnityEngine;

namespace Game.Gameplay.CameraSystem
{
    /// <summary>
    /// Rigidbodyで移動するPlayerBallにスムーズ追従するカメラ。
    /// - 一定のオフセット距離を保ちつつ、Y軸回転・俯瞰角を付与
    /// - Yは緩やかに補間して追従
    /// - LateUpdateでレンダリング直前に更新
    /// </summary>
    public sealed class FollowPlayerBallCamera : MonoBehaviour
    {
        [Header("追従対象")]
        [SerializeField] private Transform _target;

        [Header("オフセット設定")]
        [SerializeField] private float _distance = 6f;     // プレイヤーからの距離
        [SerializeField] private float _height = 3f;       // 高さオフセット
        [SerializeField] private float _yawAngle = 20f;    // Y軸回転角（左右）
        [SerializeField] private float _pitchAngle = 15f;  // 俯瞰角（下向き）

        [Header("スムージング")]
        [SerializeField, Range(0.1f, 10f)] private float _followSmooth = 6f;

        private void LateUpdate()
        {
            if (_target == null) return;

            // --- カメラのローカル回転をもとにオフセット計算 ---
            Quaternion rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
            Vector3 offset = rotation * new Vector3(0f, _height, -_distance);

            // 目標位置
            Vector3 desiredPos = _target.position + offset;

            // スムーズ追従
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPos,
                1f - Mathf.Exp(-_followSmooth * Time.deltaTime)
            );

            // プレイヤーを注視
            transform.LookAt(_target.position + Vector3.up * 0.5f);
        }

        public void SetTarget(Transform target) => _target = target;
    }
}