using System;

namespace RuntimeRoguelike.Ecs
{
    public static class EcsMath
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }

        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        public static Float2 MoveTowards(Float2 current, Float2 target, float maxDelta)
        {
            var dx = target.X - current.X;
            var dy = target.Y - current.Y;
            var sqr = dx * dx + dy * dy;
            if (sqr <= maxDelta * maxDelta || sqr <= 0.000001f)
            {
                return target;
            }

            var dist = (float)Math.Sqrt(sqr);
            var scale = maxDelta / dist;
            return new Float2(current.X + dx * scale, current.Y + dy * scale);
        }
    }
}
