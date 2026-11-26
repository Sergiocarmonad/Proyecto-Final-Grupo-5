using Game.Flow;
using Game.Reef;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Displays the total collected trash count from TrashSpawner events.
    /// </summary>
    public sealed class TrashCounterHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrashSpawner _trashSpawner;
        [SerializeField] private LevelFlowController _levelFlowController;
        [SerializeField] private TMP_Text _countText;

        [Header("Formatting & Visuals")]
        [SerializeField] private string _textFormat = "{0}";
        [SerializeField] private Color _textColor = Color.white;

        private void OnEnable()
        {
            if (_trashSpawner != null)
            {
                _trashSpawner.CollectedCountChanged += HandleCollectedCountChanged;
                ApplyCount(_trashSpawner.CollectedCount);
            }

            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted += HandleLevelStarted;
            }
        }

        private void OnDisable()
        {
            if (_trashSpawner != null)
            {
                _trashSpawner.CollectedCountChanged -= HandleCollectedCountChanged;
            }

            if (_levelFlowController != null)
            {
                _levelFlowController.LevelStarted -= HandleLevelStarted;
            }
        }

        private void HandleLevelStarted(LevelConfig _)
        {
            if (_trashSpawner != null)
            {
                ApplyCount(_trashSpawner.CollectedCount);
            }
        }

        private void HandleCollectedCountChanged(int collectedCount)
        {
            ApplyCount(collectedCount);
        }

        private void ApplyCount(int collectedCount)
        {
            if (_countText == null)
            {
                return;
            }

            _countText.text = string.Format(_textFormat, Mathf.Max(0, collectedCount));
            _countText.color = _textColor;
        }
    }
}
