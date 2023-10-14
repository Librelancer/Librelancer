using System.Numerics;

namespace LibreLancer;

public class MovingAverage<T> where T : ISignedNumber<T>
{
    private T[] values;
    private int index = 0;
    private T average;
    public MovingAverage(int count)
    {
        values = new T[count];
    }

    public void AddValue(T value)
    {
        values[index] = value;
        index = (index + 1) % values.Length;
        CalculateAverage();
    }

    public T Average => average;

    void CalculateAverage()
    {
        double accum = 0;
        for (int i = 0; i < values.Length; i++) {
            accum += double.CreateChecked(values[i]);
        }
        accum /= values.Length;
        average = T.CreateTruncating(accum);
    }

    public void ForceSetAverage(T average)
    {
        this.average = average;
        for (int i = 0; i < values.Length; i++)
            values[i] = average;
        index = 0;
    }
}
