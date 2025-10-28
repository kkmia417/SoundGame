// WHY: デバッグ用HUD。読み取り専用でUIとロジックを分離。
using UnityEngine;
using UnityEngine.UI;
using Game.Adapters;

namespace Game.UI
{
    public sealed class PitchHud : MonoBehaviour
    {
        [SerializeField] private PitchInputLoop _input;
        [SerializeField] private Text _hzText;
        [SerializeField] private Text _confText;
        [SerializeField] private Text _heightText;

        private void Update()
        {
            if (_input == null) return;
            if (_hzText) _hzText.text = $"Hz: {_input.CurrentHz:F1}";
            if (_confText) _confText.text = $"Conf: {_input.CurrentConfidence:F2}";
            if (_heightText) _heightText.text = _input.CurrentHeight.HasValue ? $"Height: {_input.CurrentHeight.Value:F2}" : "Height: --";
        }
    }
}