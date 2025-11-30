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

    [Fact]
    public void QuatEulerAngles()
    {
        var v = new Vector3(35, 119, 20);

        var a = MathHelper.QuatFromEulerDegrees(v);
        var b = MathHelper.MatrixFromEulerDegrees(v);

        AssertRotationsEqual(Matrix4x4.CreateFromQuaternion(a), b, 1, v);
    }

    private static readonly Vector3[] eulers =
    [
        new (35, 119, 20),
        new (-102.4f, 63.7f, 4.9f),
        new (45.1f, 173.3f, -99.6f),
        new (-11.7f, 28.5f, 72.1f),
        new (130.8f, -56.2f, -12.3f),
        new (90.4f, -97.9f, 160.6f),
        new (-149.6f, -45.4f, -35.2f),
        new (32.0f, 89.7f, 45.8f),
        new (-75.3f, 160.1f, 128.5f),
        new (11.6f, -19.2f, -83.7f),
        new (174.4f, -171.9f, 109.3f)
    ];

    [Fact]
    public void RoundTripQuaternion()
    {
        foreach (var v in eulers)
        {
            var a = MathHelper.QuatFromEulerDegrees(v);
            var v2 = a.GetEulerDegrees();
            var b = MathHelper.QuatFromEulerDegrees(v2);
            var error = MathHelper.QuatError(a, b);
            Assert.True(error <= 0.0001f, $"Rotations are noticeably different for {a} and {b} based on angles {v} (error {error})");
        }
    }

    [Fact]
    public void LiDreadRotationBug()
    {
        var q = new Quaternion(-0.50000006f, -0.5f, -0.5f, 0.5f);
        var q3 = Quaternion.Normalize(q);
        var euler = Quaternion.Normalize(q3).GetEulerDegrees();
        var q2 = MathHelper.QuatFromEulerDegrees(euler);
        var error = MathHelper.QuatError(q, q2);
        Assert.True(error <= 0.0001f, $"Rotations are noticeably different for {q} and {q2} based on angles {euler} (error {error})");
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
