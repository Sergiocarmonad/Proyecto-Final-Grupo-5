using System.Collections.Generic;
using System.Text;
using Game.Reef;
using UnityEngine;

namespace Game.Radar
{
    // Temporary debug script — delete after validating radar and coral alert events in Unity.
    /// <summary>
    /// Temporary logger that prints RadarController and CoralAlertNotifier events to the Unity console.
    /// </summary>
    public sealed class RadarDebugLogger : MonoBehaviour
    {
        [SerializeField] private RadarController _radarController;
        [SerializeField] private CoralAlertNotifier _coralAlertNotifier;

        private readonly StringBuilder _logBuilder = new StringBuilder();

        private void OnEnable()
        {
            if (_radarController != null)
            {
                _radarController.RadarUpdated += HandleRadarUpdated;
            }

            if (_coralAlertNotifier != null)
            {
                _coralAlertNotifier.AlertRaised += HandleAlertRaised;
                _coralAlertNotifier.AlertCleared += HandleAlertCleared;
            }
        }

        private void OnDisable()
        {
            if (_radarController != null)
            {
                _radarController.RadarUpdated -= HandleRadarUpdated;
            }

            if (_coralAlertNotifier != null)
            {
                _coralAlertNotifier.AlertRaised -= HandleAlertRaised;
                _coralAlertNotifier.AlertCleared -= HandleAlertCleared;
            }
        }

        private void HandleAlertRaised(CoralZone zone)
        {
            string zoneLabel = ResolveZoneLabel(zone);
            Debug.Log($"[RadarDebug] AlertRaised zone={zoneLabel}", this);
        }

        private void HandleAlertCleared(CoralZone zone)
        {
            string zoneLabel = ResolveZoneLabel(zone);
            Debug.Log($"[RadarDebug] AlertCleared zone={zoneLabel}", this);
        }

        private void HandleRadarUpdated(IReadOnlyList<RadarBlipData> blips)
        {
            int count = blips != null ? blips.Count : 0;
            _logBuilder.Clear();
            _logBuilder.Append("[RadarDebug] RadarUpdated blips=").Append(count);

            for (int i = 0; i < count; i++)
            {
                RadarBlipData blip = blips[i];
                _logBuilder
                    .Append(" | #").Append(blip.TrackableId)
                    .Append(" type=").Append(blip.EntityType)
                    .Append(" color=").Append(blip.Color)
                    .Append(" severity=").Append(blip.Severity.ToString("0.00"))
                    .Append(" angle=").Append(blip.RelativeAngleDegrees.ToString("0.0"))
                    .Append(" dist=").Append(blip.NormalizedDistance.ToString("0.00"));
            }

            Debug.Log(_logBuilder.ToString(), this);
        }

        private static string ResolveZoneLabel(CoralZone zone)
        {
            if (zone == null)
            {
                return "<null>";
            }

            return string.IsNullOrWhiteSpace(zone.ZoneId) ? zone.name : zone.ZoneId;
        }
    }
}
