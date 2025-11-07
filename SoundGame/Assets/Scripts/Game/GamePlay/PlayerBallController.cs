// WHY:
// - 壁に当たった時の“ちょい跳ね”を活かすため、Y座標を MovePosition で直接書き換えない。
//   目標高さへの追従は PD 制御 + AddForce(Acceleration) で行い、物理一貫性を維持する。
// - 前進(X)は「目標X速度」に近づける加速度だけ適用。Y/Z速度は触らず、衝突由来のバウンドを殺さない。
// - すべての係数はインスペクタで調整可能（保守性・可読性・拡張性を優先）。

using UnityEngine;
using Game.Adapters;

namespace Game.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerBallController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PitchInputLoop _input;  // _input.CurrentHeight: float?（Hz→高さの結果）

        [Header("Forward (X)")]
        [SerializeField, Min(0f)] private float _baseXSpeed = 10f;   // ここを上げると「もっと速く」進む
        [SerializeField, Min(0f)] private float _xAccelSnap = 60f;   // X速度を目標へ寄せる加速度の上限（大きいほどキビキビ）

        [Header("Vertical Follow (PD)")]
        [SerializeField, Min(0f)] private float _kp = 30f;           // 位置誤差係数（↑で素早く追従）
        [SerializeField, Min(0f)] private float _kd = 8f;            // 速度誤差係数（減衰・ブレーキ）
        [SerializeField, Min(0f)] private float _maxUpAccel = 60f;   // 上方向の最大加速度
        [SerializeField, Min(0f)] private float _maxDownAccel = 60f; // 下方向の最大加速度
        [SerializeField, Min(0f)] private float _maxVerticalSpeed = 18f; // Y速度の安全クリップ

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            // 衝突の安定性向上
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.freezeRotation = true; // 回転不要なら固定（必要なら外してOK）
        }

        private void FixedUpdate()
        {
            // --- 1) 前進(X)：目標速度へ“加速度”で寄せる（Y/Zは触らない） ---
            float targetVx = _baseXSpeed;
            float vx = _rb.velocity.x;
            float dvx = targetVx - vx;

            // 速度差をこのフレームで埋めるのに必要な加速度 ≒ dv/dt をクリップして適用
            float ax = Mathf.Clamp(dvx / Time.fixedDeltaTime, -_xAccelSnap, _xAccelSnap);
            _rb.AddForce(new Vector3(ax, 0f, 0f), ForceMode.Acceleration);

            // --- 2) 垂直(Y)：PD制御で目標高さへ“力学的に”追従（MovePositionは使わない） ---
            if (_input != null && _input.CurrentHeight.HasValue)
            {
                float targetY = _input.CurrentHeight.Value;
                float yErr = targetY - _rb.position.y; // 位置誤差
                float vy    = _rb.velocity.y;          // 現在のY速度

                // P(位置) + D(速度) の簡易PD制御
                float ay = _kp * yErr + _kd * (-vy);

                // 過大な加速度を抑制（跳ね/めり込みの暴れ防止）
                ay = Mathf.Clamp(ay, -_maxDownAccel, _maxUpAccel);

                _rb.AddForce(new Vector3(0f, ay, 0f), ForceMode.Acceleration);

                // 垂直速度を安全にクリップ（数値爆発の保険）
                float clampedVy = Mathf.Clamp(_rb.velocity.y, -_maxVerticalSpeed, _maxVerticalSpeed);
                if (!Mathf.Approximately(clampedVy, _rb.velocity.y))
                    _rb.velocity = new Vector3(_rb.velocity.x, clampedVy, _rb.velocity.z);
            }

            // ※ MovePosition/MoveRotation は使わない：衝突応答と反発を正しく生かすため
        }

        // --- 外部から動的に調整したい場合のAPI（任意） ---
        public void SetBaseSpeed(float xSpeed) => _baseXSpeed = Mathf.Max(0f, xSpeed);
        public void SetPDGains(float kp, float kd) { _kp = Mathf.Max(0f, kp); _kd = Mathf.Max(0f, kd); }
    }
}
