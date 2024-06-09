using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace VoiDPlugins.Filter
{
    [PluginName("Reconstructor")]
    public class Reconstructor : IPositionedPipelineElement<IDeviceReport>
    {
        private Vector2? lastAvg;
        private float weight;
        private float bigMovementCutoff;

        [Property("EMA Weight"), DefaultPropertyValue(0.5f), ToolTip
        (
            "Default: 0.5\n\n" +
            "Defines the weight of the latest sample against previous ones [Range: 0.0 - 1.0]\n" +
            "  Lower == More hardware smoothing removed\n" +
            "  1 == No effect"
        )]
        [Property("Big Movement Cutoff"), DefaultPropertyValue(-1f), ToolTip
        (
            "Default: -1\n\n" +
            "Any move larger than this will not be smoothed, use with relative mode.\n" +
            "  -1 == No effect\n" +
        )]

        public float EMAWeight
        {
            set => weight = Math.Clamp(value, 0, 1);
            get => weight;
        }

        public float BigMovementCutoff
        {
            set => bigMovementCutoff = Math.Max(0, value);
            get => bigMovementCutoff;
        }

        public event Action<IDeviceReport>? Emit;

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                var truePoint = lastAvg.HasValue ? ReverseEMAFunc(report.Position, lastAvg.Value, (float)EMAWeight) : report.Position;
                lastAvg = report.Position;
                report.Position = truePoint;
                value = report;
            }

            Emit?.Invoke(value);
        }

        private static Vector2 ReverseEMAFunc(Vector2 currentEMA, Vector2 lastEMA, float weight)
        {
            if (bigMovementCutoff != -1 && Vector2.Distance(currentEMA, lastEMA) > bigMovementCutoff)
                return currentEMA;
            return ((currentEMA - lastEMA) / weight) + lastEMA;
        }
    }
}