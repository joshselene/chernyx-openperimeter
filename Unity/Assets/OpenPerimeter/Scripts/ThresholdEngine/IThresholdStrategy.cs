using System;
using OpenPerimeter.Calibration;
using OpenPerimeter.Core;

namespace OpenPerimeter.ThresholdEngine
{
    public class ThresholdPointResult
    {
        public int pointIndex;
        public float xDeg;
        public float yDeg;
        public float estimatedThresholdDb;
        public float posteriorSdDb;
        public CensoringStatus censoringStatus;
        public int presentationCount;
        public bool isComplete;
        public GoldmannSize stimulusSize = GoldmannSize.SizeIII;
    }

    public interface IThresholdStrategy
    {
        int PointIndex { get; }
        float XDeg { get; }
        float YDeg { get; }
        bool IsComplete { get; }
        int PresentationCount { get; }

        void Initialize(int pointIndex, float xDeg, float yDeg, float priorMeanDb, float priorSdDb);
        float GetNextStimulusDb();
        void ProcessResponse(bool userResponded, double reactionTimeMs);
        ThresholdPointResult GetFinalResult();
    }
}
