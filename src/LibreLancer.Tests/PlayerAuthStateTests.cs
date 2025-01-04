using System;
using System.IO;
using System.Numerics;
using LibreLancer.Net.Protocol;
using Xunit;

namespace LibreLancer.Tests;

public class PlayerAuthStateTests
{
    static Vector3 Permutation(Random rand)
    {
        var big = rand.Next(0, 4) == 1;
        var x = big ? rand.NextFloat(-600, 600) : rand.NextFloat(-10, 10);
        var y = big ? rand.NextFloat(-600, 600) : rand.NextFloat(-10, 10);
        var z = big ? rand.NextFloat(-600, 600) : rand.NextFloat(-10, 10);
        return new Vector3(x, y, z);
    }

    [Fact]
    public void ShouldDiffAcrossFrames()
    {
        Random rand = new Random(9874);
        var ogs = new PlayerAuthState[500];
        ogs[0] = new PlayerAuthState()
        {
            AngularVelocity = new Vector3(78, 2, 0),
            LinearVelocity = new Vector3(100, 0, 0),
            Health = 100,
            Orientation = Quaternion.Identity,
            Position = new Vector3(100, 100, 100),
            Shield = 100
        };
        for (int i = 1; i < ogs.Length; i++)
        {
            var rot = new Vector3(rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f));
            var orient = Quaternion.CreateFromYawPitchRoll(rot.X, rot.Y, rot.Z) * ogs[i - 1].Orientation;
            ogs[i] = new PlayerAuthState() {
                AngularVelocity = ogs[i - 1].AngularVelocity + Permutation(rand),
                LinearVelocity = ogs[i - 1].LinearVelocity + Permutation(rand),
                Health = 100,
                Orientation = orient,
                Position = ogs[i - 1].Position + Permutation(rand),
                Shield = 100
            };
        }

        var read = new PlayerAuthState[ogs.Length];
        for (int i = 0; i < ogs.Length; i++)
        {
            var bw = new BitWriter();
            ogs[i].Write(ref bw, i > 0 ? ogs[i - 1] : new PlayerAuthState(), (uint)(i + 1));
            var br = new BitReader(bw.GetCopy(), 0);
            read[i] = PlayerAuthState.Read(ref br, i > 0 ? ogs[i - 1] : new PlayerAuthState());
            Assert.True((ogs[i].Position - read[i].Position).Length() < 0.001f, $"Position differs {i} " +
                $"({ogs[i].Position} != {read[i].Position})");
            Assert.True((ogs[i].LinearVelocity - read[i].LinearVelocity).Length() < 0.001f, $"LinearVelocity differs {i} " +
                $"({ogs[i].LinearVelocity} != {read[i].LinearVelocity})");
            Assert.True((ogs[i].AngularVelocity - read[i].AngularVelocity).Length() < 0.001f, $"AngularVelocity differs {i} " +
                $"({ogs[i].AngularVelocity} != {read[i].AngularVelocity})");
        }

    }
}
