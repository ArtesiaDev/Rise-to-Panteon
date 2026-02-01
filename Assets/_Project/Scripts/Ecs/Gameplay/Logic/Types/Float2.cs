using System;

namespace RuntimeRoguelike.Ecs
{
    public readonly struct Float2 : IEquatable<Float2>
    {
        public float X { get; }
        public float Y { get; }

        public static Float2 Zero => new Float2(0f, 0f);

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Float2 other)
        {
            return Math.Abs(X - other.X) < 0.0001f && Math.Abs(Y - other.Y) < 0.0001f;
        }

        public override bool Equals(object obj)
        {
            return obj is Float2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"({X:0.###}, {Y:0.###})";
        }

        public static Float2 operator +(Float2 left, Float2 right)
        {
            return new Float2(left.X + right.X, left.Y + right.Y);
        }

        public static Float2 operator -(Float2 left, Float2 right)
        {
            return new Float2(left.X - right.X, left.Y - right.Y);
        }

        public static Float2 operator *(Float2 value, float multiplier)
        {
            return new Float2(value.X * multiplier, value.Y * multiplier);
        }
    }
}
