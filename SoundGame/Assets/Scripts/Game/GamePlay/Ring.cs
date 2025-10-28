// WHY: 通過ルールをトリガーに集約（結合低下）。one-shotで誤多重加算防止。
using UnityEngine;

namespace Game.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class Ring : MonoBehaviour
    {
        [SerializeField] private int _score = 1;
        [SerializeField] private bool _oneShot = true;
        private bool _consumed;

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed) return;
            if (other.attachedRigidbody && other.attachedRigidbody.GetComponent<PlayerBallController>())
            {
                ScoreSystem.Instance?.Add(_score);
                if (_oneShot){ _consumed = true; gameObject.SetActive(false); }
            }
        }
    }
}