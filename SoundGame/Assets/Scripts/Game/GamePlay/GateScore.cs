// WHY:
// - スコア加算責務をトリガー（穴）側へ集約し、プレイヤー/壁から分離（疎結合）。
// - one-shotで誤加算を防止。ScoreSystemがあれば加点、なければ無視（安全）。
using UnityEngine;
using Game.Gameplay;

namespace Game
{
    [RequireComponent(typeof(Collider))]
    public sealed class GateScore : MonoBehaviour
    {
        [SerializeField] private int _score = 1;
        [SerializeField] private bool _oneShot = true;
        private bool _used;

        private void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.attachedRigidbody && other.attachedRigidbody.GetComponent<PlayerBallController>())
            {
                //ScoreSystem.Instance?.Add(_score);
                if (_oneShot) _used = true;
            }
        }
    }
}