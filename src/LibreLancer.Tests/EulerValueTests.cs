using System;
using LibreLancer;
using System.Numerics;
using Xunit;
namespace LibreLancer.Tests;

public class EulerValueTests
{
    [Fact]
    public void CanRoundtripMatrix()
    {
        // Part with complex rotation
        RoundtripMatrix(new Matrix4x4(
            0.853097796f, 0.354865164f, 0.382489092f, 0f,
            -0.383862048f, 0.923390269f, -0.000541638583f, 0f,
            -0.353375226f, -0.156359444f, 0.92396152f, 0f,
            0f, 0f, 0f, 1f
        ), 1);
        //45 deg angle
        RoundtripMatrix(new Matrix4x4(
          1, 0, 0, 0,
          0, 0.7071068f, -0.7071078f, 0,
          0, 0.7071068f, 0.7071068f, 0,
          0, 0, 0, 1
        ), 2);
        // Reported matrix error - Sentinel.3db
        RoundtripMatrix(new Matrix4x4(
          0, 0, -1, 0,
          0, 1, 0, 0,
          1, 0, 0, 0,
          0, 0, 0, 1
        ), 3);
    }

    static void RoundtripMatrix(Matrix4x4 mat, int index)
    {
        var euler = mat.GetEulerDegrees();
        var mat2 = MathHelper.MatrixFromEulerDegrees(euler);
        AssertRotationsEqual(mat, mat2, index, euler);
    }

    static void AssertRotationsEqual(Matrix4x4 a, Matrix4x4 b, int index, Vector3 euler)
    {
        var qa = a.ExtractRotation();
        var qb = b.ExtractRotation();
        var error = MathHelper.QuatError(qa, qb);
        Assert.True(error <= 0.0001f, $"Case {index}: Rotations are noticeably different for {a} and {b} (error {error}, euler {euler})");
    }

}
