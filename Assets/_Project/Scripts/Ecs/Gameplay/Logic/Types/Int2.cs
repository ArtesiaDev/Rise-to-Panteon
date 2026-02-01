using System;

namespace RuntimeRoguelike.Ecs
{
    public readonly struct Int2 : IEquatable<Int2>
    {
        public int X { get; }
        public int Y { get; }

        public static Int2 Zero => new Int2(0, 0);
        public static Int2 Up => new Int2(0, 1);
        public static Int2 Down => new Int2(0, -1);
        public static Int2 Left => new Int2(-1, 0);
        public static Int2 Right => new Int2(1, 0);

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanDistance(Int2 other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(Int2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Int2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(Int2 left, Int2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Int2 left, Int2 right)
        {
            return !left.Equals(right);
        }

        public static Int2 operator +(Int2 left, Int2 right)
        {
            return new Int2(left.X + right.X, left.Y + right.Y);
        }

        public static Int2 operator -(Int2 left, Int2 right)
        {
            return new Int2(left.X - right.X, left.Y - right.Y);
        }

        public static Int2 operator *(Int2 value, int multiplier)
        {
            return new Int2(value.X * multiplier, value.Y * multiplier);
        }

        public static Int2 operator *(int multiplier, Int2 value)
        {
            return new Int2(value.X * multiplier, value.Y * multiplier);
        }
    }
}
