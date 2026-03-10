using System.Linq;
using System.Numerics;

namespace LibreLancer;

public class MovingAverage<T> where T : ISignedNumber<T>
{
    private T[] values;
    private int index = 0;
    private T average = default!;
    public MovingAverage(int count)
    {
        values = new T[count];
        CalculateAverage();
    }

    public void AddValue(T value)
    {
        values[index] = value;
        index = (index + 1) % values.Length;
        CalculateAverage();
    }

    public T Average => average;

    private void CalculateAverage()
    {
        var accum = values.Sum(double.CreateChecked);
        accum /= values.Length;
        average = T.CreateTruncating(accum);
    }

    public void ForceSetAverage(T avg)
    {
        average = avg;
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = avg;
        }

        index = 0;
    }
}
