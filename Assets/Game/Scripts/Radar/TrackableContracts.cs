using System;
using UnityEngine;

namespace Game.Radar
{
    public enum RadarEntityType
    {
        CoralZone = 0,
        Enemy = 1
    }

    public enum RadarSignalColor
    {
        Green = 0,
        Orange = 1,
        Red = 2
    }

    /// <summary>
    /// Immutable radar snapshot for a single tracked entity, ready for HUD presentation.
    /// </summary>
    public readonly struct RadarBlipData
    {
        public RadarBlipData(
            int trackableId,
            RadarEntityType entityType,
            RadarSignalColor color,
            float severity,
            Vector3 worldPosition,
            float relativeAngleDegrees,
            float normalizedDistance,
            bool isActive)
        {
            TrackableId = trackableId;
            EntityType = entityType;
            Color = color;
            Severity = Mathf.Clamp01(severity);
            WorldPosition = worldPosition;
            RelativeAngleDegrees = relativeAngleDegrees;
            NormalizedDistance = Mathf.Clamp01(normalizedDistance);
            IsActive = isActive;
        }

        public int TrackableId { get; }
        public RadarEntityType EntityType { get; }
        public RadarSignalColor Color { get; }
        public float Severity { get; }
        public Vector3 WorldPosition { get; }

        /// <summary>
        /// Signed horizontal angle in degrees relative to radar origin forward (0 ahead, +right, -left).
        /// </summary>
        public float RelativeAngleDegrees { get; }

        /// <summary>
        /// Horizontal distance to radar origin normalized to [0, 1] against radar range.
        /// </summary>
        public float NormalizedDistance { get; }

        public bool IsActive { get; }
    }

    /// <summary>
    /// Contract implemented by world entities that appear on the radar.
    /// </summary>
    public interface IRadarTrackable
    {
        RadarEntityType EntityType { get; }
        RadarSignalColor RadarColor { get; }
        float RadarSeverity { get; }
        Vector3 WorldPosition { get; }
        bool IsTrackableActive { get; }

        event Action<IRadarTrackable> TrackableStateChanged;
    }

    /// <summary>
    /// Pairs a registered trackable entity with its registry-assigned numeric ID.
    /// </summary>
    public readonly struct TrackableRecord
    {
        public TrackableRecord(int trackableId, IRadarTrackable trackable)
        {
            TrackableId = trackableId;
            Trackable = trackable;
        }

        public int TrackableId { get; }
        public IRadarTrackable Trackable { get; }
    }
}
