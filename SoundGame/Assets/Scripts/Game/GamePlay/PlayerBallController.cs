// WHY: 物理はFixedUpdate。高さのみ補間してMovePosition→物理整合性を保つ。
using UnityEngine;
using Game.Adapters;

namespace Game.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerBallController : MonoBehaviour
    {
        [SerializeField] private PitchInputLoop _input;
        [SerializeField] private float _followSpeed = 4f;
        [SerializeField] private float _baseXSpeed = 3f;
        private Rigidbody _rb;

        private void Awake(){ _rb = GetComponent<Rigidbody>(); }

        private void FixedUpdate()
        {
            var v = _rb.velocity; v.x = _baseXSpeed; _rb.velocity = v;
            if (_input?.CurrentHeight != null)
            {
                float targetY = _input.CurrentHeight.Value;
                float newY = Mathf.Lerp(transform.position.y, targetY, 1f - Mathf.Exp(-_followSpeed * Time.fixedDeltaTime));
                var pos = _rb.position; pos.y = newY; _rb.MovePosition(pos);
            }
        }
    }
}