// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer;

public static class ApproximateCurve
{
    /// <summary>
    /// Build a quadratic function from the points (after transforming X to be [0..1]).
    /// WARNING: Probably very slow!
    /// </summary>
    /// <returns>The cubic function.</returns>
    /// <param name="points">Points to build data from</param>
    public static Vector3 GetQuadraticFunction(Vector2[] points)
    {
        if (points.Length == 0)
            throw new ArgumentException("Can't build function from 0 points");
        if (points.Length == 1)
            return new Vector3(0,0, points[0].Y);
        if (points.Length == 2)
            return new Vector3(0, (points[1].Y - points[0].Y), points[0].Y);
        //Transform points array to go from 0 to 1
        Array.Sort(points,(a, b) => a.X.CompareTo(b.X));
        for (var i = 0; i < points.Length; i++)
            points[i].X -= points[0].X;
        var max = points[^1].X;
        for (var i = 0; i < points.Length; i++)
            points[i].X /= max;
        var x = new double[points.Length];
        var y = new double[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            x[i] = points[i].X;
            y[i] = points[i].Y;
        }
        return Quadratic(x, y);
    }

    private static Vector3 Quadratic(double[] pointx, double[] pointy)
    {
        //notation sjk to mean the sum of x_i^j*y_i^k.
        var s40 = getSxn(4, pointx);
        var s30 = getSxn(3, pointx);
        var s20 = getSxn(2, pointx);
        var s10 = getSx(pointx);
        var s00 = (double)pointx.Length;
        //sum of x^0 * y^0  ie 1 * number of entries

        var s21 = getSx2y(pointx, pointy);
        var s11 = getSxy(pointx, pointy);
        var s01 = getSx(pointy);

        //a = Da/D
        var a = (s21 * (s20 * s00 - s10 * s10) -
                 s11 * (s30 * s00 - s10 * s20) +
                 s01 * (s30 * s10 - s20 * s20))
                /
                (s40 * (s20 * s00 - s10 * s10) -
                 s30 * (s30 * s00 - s10 * s20) +
                 s20 * (s30 * s10 - s20 * s20));
        var b = (s40 * (s11 * s00 - s01 * s10) -
                 s30 * (s21 * s00 - s01 * s20) +
                 s20 * (s21 * s10 - s11 * s20))
                /
                (s40 * (s20 * s00 - s10 * s10) -
                 s30 * (s30 * s00 - s10 * s20) +
                 s20 * (s30 * s10 - s20 * s20));
        var c = (s40 * (s20 * s01 - s10 * s11) -
                 s30 * (s30 * s01 - s10 * s21) +
                 s20 * (s30 * s11 - s20 * s21))
                /
                (s40 * (s20 * s00 - s10 * s10) -
                 s30 * (s30 * s00 - s10 * s20) +
                 s20 * (s30 * s10 - s20 * s20));

        return new Vector3((float)a, (float)b, (float)c);
    }

    //sum of x
    private static double getSx(double[] pointx)
    {
        double sx = 0;
        for (var i = 0; i < pointx.Length; i++)
        {
            sx += pointx[i];
        }
        return sx;
    }
    //x^n sum
    private static double getSxn(int n, double[] pointx)
    {
        double sxn = 0;
        for (var i = 0; i < pointx.Length; i++)
        {
            sxn += System.Math.Pow(pointx[i], n);
        }
        return sxn;
    }
    //xy sum
    private static double getSxy(double[] pointx, double[] pointy)
    {
        double sxy = 0;
        for (var i = 0; i < pointx.Length; i++)
        {
            sxy += pointx[i] * pointy[i];
        }
        return sxy;
    }
    //x^2*y sum
    private static double getSx2y(double[] pointx, double[] pointy)
    {
        double sx2y = 0;
        for (var i = 0; i < pointx.Length; i++)
        {
            sx2y += (pointx[i] * pointx[i]) * pointy[i];
        }
        return sx2y;
    }
}
