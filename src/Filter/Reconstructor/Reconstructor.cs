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
        public float EMAWeight
        {
            set => weight = Math.Clamp(value, 0, 1);
            get => weight;
        }

        [Property("Big Movement Cutoff"), DefaultPropertyValue(-1f), ToolTip
        (
            "Default: -1\n\n" +
            "Any move larger than this will not be smoothed, use with relative output mode.\n" +
            "  -1 == No effect"
        )]
        public float BigMovementCutoff
        {
            set => bigMovementCutoff = value == -1 ? value : Math.Max(0, value);
            get => bigMovementCutoff;
        }

        public event Action<IDeviceReport>? Emit;

        public PipelinePosition Position => PipelinePosition.PreTransform;

        public void Consume(IDeviceReport value)
        {
            if (value is ITabletReport report)
            {
                var truePoint = lastAvg.HasValue ? ReverseEMAFunc(report.Position, lastAvg.Value, (float)EMAWeight, (float)BigMovementCutoff) : report.Position;
                lastAvg = report.Position;
                report.Position = truePoint;
                value = report;
            }

            Emit?.Invoke(value);
        }

        private static Vector2 ReverseEMAFunc(Vector2 currentEMA, Vector2 lastEMA, float weight, float bigMovementCutoff)
        {
            if (bigMovementCutoff != -1 && Vector2.Distance(currentEMA, lastEMA) > bigMovementCutoff)
                return currentEMA;
            return ((currentEMA - lastEMA) / weight) + lastEMA;
        }
    }
}