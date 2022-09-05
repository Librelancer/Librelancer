namespace LibreLancer.Data
{
    public struct ValueRange<T> where T : struct
    {
        public T Min;
        public T Max;

        public ValueRange(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            return $"[{Min} -> {Max}]";
        }
    }
}