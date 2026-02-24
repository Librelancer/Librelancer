using System;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Ale;
using LibreLancer.Utf;
using LibreLancer.Utf.Ale;
using Xunit;

namespace LibreLancer.Tests;

public class AleNodeWriterTests
{
    private static AlchemyCurveAnimation testCurve;
    private static AlchemyCurveAnimation testCurve2;

    static AleNodeWriterTests()
    {
        testCurve = new AlchemyCurveAnimation();
        testCurve.Type = EasingTypes.EaseInOut;
        var curve0 = new AlchemyCurve();
        curve0.Value = 8.0f;
        testCurve.Items.Add(curve0);
        var curve1 = new AlchemyCurve();
        curve1.SParam = 0.8f;
        curve1.Flags = LoopFlags.Repeat;
        curve1.Value = 12.0f;
        curve1.IsCurve = true;
        curve1.Keyframes = new();
        curve1.Keyframes.Add(new CurveKeyframe() { Time = 0, Start = 2, End = 3, Value = 4});
        curve1.Keyframes.Add(new CurveKeyframe() { Time = 5, Start = 6, End = 7, Value = 8});
        testCurve.Items.Add(curve1);

        testCurve2 = new AlchemyCurveAnimation();
        testCurve2.Type = EasingTypes.EaseInOut;
        var curve2 = new AlchemyCurve();
        curve2.Value = 8.0f;
        testCurve2.Items.Add(curve2);
        var curve3 = new AlchemyCurve();
        curve3.SParam = 0.8f;
        curve3.Flags = LoopFlags.Repeat;
        curve3.Value = 12.0f;
        curve3.IsCurve = true;
        curve3.Keyframes = new();
        curve3.Keyframes.Add(new CurveKeyframe() { Time = 0, Start = 2, End = 3, Value = 4});
        curve3.Keyframes.Add(new CurveKeyframe() { Time = 5, Start = 6, End = 7, Value = 8});
        testCurve2.Items.Add(curve3);
    }

    static LeafNode ToUtfNode(ALEffectLib effectLib)
    {
        var eutf = new EditableUtf();
        var newNode = new LUtfNode()
            { Parent = eutf.Root, Data = AleNodeWriter.WriteALEffectLib(effectLib), Name = "x" };
        eutf.Root.Children.Add(newNode);
        return (LeafNode)eutf.Export()[0];
    }

    static LeafNode ToUtfNode(AlchemyNodeLibrary nodeLib)
    {
        var eutf = new EditableUtf();
        var newNode = new LUtfNode()
            { Parent = eutf.Root, Data = AleNodeWriter.WriteAlchemyNodeLibrary(nodeLib), Name = "x" };
        eutf.Root.Children.Add(newNode);
        return (LeafNode)eutf.Export()[0];
    }

    [Fact]
    public void ShouldRoundtripALEffectLib()
    {
        var fxLib = new ALEffectLib();
        {
            var alfx = new ALEffect();
            alfx.Name = "Hello";
            alfx.Pairs = new();
            alfx.Pairs.Add((4,5));
            alfx.Pairs.Add((9,3));
            alfx.Fx.Add(new AlchemyNodeRef(1, 5, 2, 1));
            alfx.Fx.Add(new AlchemyNodeRef(0, 9, 4, 2));
            fxLib.Effects.Add(alfx);
        }
        {
            var alfx = new ALEffect();
            alfx.Name = "World";
            alfx.Pairs = new();
            alfx.Pairs.Add((82,5));
            alfx.Pairs.Add((94,3));
            alfx.Fx.Add(new AlchemyNodeRef(1, 54, 22, 31));
            alfx.Fx.Add(new AlchemyNodeRef(0, 19, 54, 52));
            fxLib.Effects.Add(alfx);
        }


        var asUtf = ToUtfNode(fxLib);
        var parsed = new ALEffectLib(asUtf);

        Assert.Equal(fxLib.Effects.Count, parsed.Effects.Count);
        for (int i = 0; i < fxLib.Effects.Count; i++)
        {
            Assert.Equal(fxLib.Effects[i].Name, parsed.Effects[i].Name);
            Assert.Equal(fxLib.Effects[i].Pairs, parsed.Effects[i].Pairs);
            Assert.Equal(fxLib.Effects[i].Fx.Count, parsed.Effects[i].Fx.Count);
            for (int j = 0; j < fxLib.Effects[i].Fx.Count; j++)
            {
                Assert.Equal(fxLib.Effects[i].Fx[j].CRC, parsed.Effects[i].Fx[j].CRC);
                Assert.Equal(fxLib.Effects[i].Fx[j].Flag, parsed.Effects[i].Fx[j].Flag);
                Assert.Equal(fxLib.Effects[i].Fx[j].Parent, parsed.Effects[i].Fx[j].Parent);
                Assert.Equal(fxLib.Effects[i].Fx[j].Index, parsed.Effects[i].Fx[j].Index);
            }
        }
    }

    private const AleProperty StringProp = AleProperty.BasicApp_TexName;
    private const AleProperty BoolProp = AleProperty.BasicApp_UseCommonTexFrame;
    private const AleProperty CurveProp = AleProperty.Emitter_Pressure;
    private const AleProperty TransformProp = AleProperty.Node_Transform;
    private const AleProperty FloatsProp = AleProperty.BasicApp_HToVAspect;
    private const AleProperty ColorProp = AleProperty.BasicApp_Color;

    [Fact]
    public void ShouldRoundtripAlchemyNodeLibrary()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";
        n.Parameters.Add(new AleParameter(StringProp, "Texture"));
        n.Parameters.Add(new AleParameter(BoolProp, true));
        nodelib.Nodes.Add(n);

        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));

        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        for (int i = 0; i < nodelib.Nodes.Count; i++)
        {
            Assert.Equal(nodelib.Nodes[i].ClassName, copylib.Nodes[i].ClassName);
            Assert.Equal(nodelib.Nodes[i].Parameters.Count, copylib.Nodes[i].Parameters.Count);
            for (int j = 0; j < nodelib.Nodes[i].Parameters.Count; j++)
            {
                Assert.Equal(nodelib.Nodes[i].Parameters[j].Name, copylib.Nodes[i].Parameters[j].Name);
            }
        }

        Assert.Equal("Texture", copylib.Nodes[0].Parameters[0].Value);
        Assert.Equal(true, copylib.Nodes[0].Parameters[1].Value);
    }

    static void AssertCurves(AlchemyCurve expected, AlchemyCurve actual)
    {
        Assert.Equal(expected.SParam, actual.SParam);
        Assert.Equal(expected.Flags, actual.Flags);
        Assert.Equal(expected.Value, actual.Value);
        if (expected.Keyframes == null)
        {
            Assert.Equal(expected.Keyframes, actual.Keyframes);
            return;
        }
        Assert.Equal(expected.Keyframes.Count, actual.Keyframes.Count);
        for (int i = 0; i < expected.Keyframes.Count; i++)
        {
            Assert.Equal(expected.Keyframes[i].Time, actual.Keyframes[i].Time);
            Assert.Equal(expected.Keyframes[i].Start, actual.Keyframes[i].Start);
            Assert.Equal(expected.Keyframes[i].End, actual.Keyframes[i].End);
            Assert.Equal(expected.Keyframes[i].Value, actual.Keyframes[i].Value);
        }
    }

    static void AssertCurveAnimations(AlchemyCurveAnimation expected, AlchemyCurveAnimation actual)
    {
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Items.Count, actual.Items.Count);
        for (int i = 0; i < expected.Items.Count; i++)
        {
            AssertCurves(expected.Items[i], actual.Items[i]);
        }
    }

    [Fact]
    public void ShouldRoundtripCurveAnimation()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";
        // Create our curve

        n.Parameters.Add(new AleParameter(CurveProp, testCurve));
        nodelib.Nodes.Add(n);
        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));

        // Sanity check
        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        Assert.Equal(nodelib.Nodes[0].ClassName, copylib.Nodes[0].ClassName);
        Assert.Equal(nodelib.Nodes[0].Parameters.Count, copylib.Nodes[0].Parameters.Count);

        // Test curve
        AssertCurveAnimations(testCurve, (AlchemyCurveAnimation)nodelib.Nodes[0].Parameters[0].Value);
    }

    [Fact]
    public void ShouldRoundtripEmptyTransform()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";
        var emptyTransform = new AlchemyTransform();
        n.Parameters.Add(new AleParameter(TransformProp, emptyTransform));
        nodelib.Nodes.Add(n);

        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));
        // Sanity check
        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        Assert.Equal(nodelib.Nodes[0].ClassName, copylib.Nodes[0].ClassName);
        Assert.Equal(nodelib.Nodes[0].Parameters.Count, copylib.Nodes[0].Parameters.Count);

        var actual = (AlchemyTransform)nodelib.Nodes[0].Parameters[0].Value;
        Assert.False(actual.HasTransform);
        Assert.Null(actual.TranslateX);
    }

    [Fact]
    public void ShouldRoundtripAnimatedTransform()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";
        var animTransform = new AlchemyTransform();
        animTransform.HasTransform = true;
        animTransform.TranslateX = testCurve;
        animTransform.TranslateY = testCurve2;
        animTransform.TranslateZ = testCurve;
        animTransform.ScaleX = testCurve;
        animTransform.ScaleY = testCurve2;
        animTransform.ScaleZ = testCurve;
        animTransform.RotatePitch = testCurve;
        animTransform.RotateYaw = testCurve2;
        animTransform.RotateRoll = testCurve;

        n.Parameters.Add(new AleParameter(TransformProp, animTransform));
        nodelib.Nodes.Add(n);

        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));
        // Sanity check
        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        Assert.Equal(nodelib.Nodes[0].ClassName, copylib.Nodes[0].ClassName);
        Assert.Equal(nodelib.Nodes[0].Parameters.Count, copylib.Nodes[0].Parameters.Count);

        var actual = (AlchemyTransform)nodelib.Nodes[0].Parameters[0].Value;
        Assert.True(actual.HasTransform);
        AssertCurveAnimations(animTransform.TranslateX, actual.TranslateX);
        AssertCurveAnimations(animTransform.TranslateY, actual.TranslateY);
        AssertCurveAnimations(animTransform.TranslateZ, actual.TranslateZ);
        AssertCurveAnimations(animTransform.ScaleX, actual.ScaleX);
        AssertCurveAnimations(animTransform.ScaleY, actual.ScaleY);
        AssertCurveAnimations(animTransform.ScaleZ, actual.ScaleZ);
        AssertCurveAnimations(animTransform.RotatePitch, actual.RotatePitch);
        AssertCurveAnimations(animTransform.RotateYaw, actual.RotateYaw);
        AssertCurveAnimations(animTransform.RotateRoll, actual.RotateRoll);
    }

    [Fact]
    public void ShouldRoundtripColorAnimation()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";

        var colorAnim = new AlchemyColorAnimation();

        var colors1 = new AlchemyColors()
        {
            SParam = 4,
            Type = EasingTypes.Step,
            Keyframes = [new(0, Color3f.White)]
        };
        colorAnim.Items.Add(colors1);
        var colors2 = new AlchemyColors()
        {
            SParam = 5,
            Type = EasingTypes.Linear,
            Keyframes =
            [
                new(0, Color3f.Black),
                new (0.2f, new Color3f(0.5f, 0.5f, 0.8f)),
                new (1, Color3f.White)
            ]
        };
        colorAnim.Items.Add(colors2);


        n.Parameters.Add(new AleParameter(ColorProp, colorAnim));
        nodelib.Nodes.Add(n);

        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));
        // Sanity check
        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        Assert.Equal(nodelib.Nodes[0].ClassName, copylib.Nodes[0].ClassName);
        Assert.Equal(nodelib.Nodes[0].Parameters.Count, copylib.Nodes[0].Parameters.Count);

        var actual = (AlchemyColorAnimation)nodelib.Nodes[0].Parameters[0].Value;
        Assert.Equal(colorAnim.Type, actual.Type);
        Assert.Equal(colorAnim.Items.Count, actual.Items.Count);
        for (int i = 0; i < colorAnim.Items.Count; i++)
        {
            Assert.Equal(colorAnim.Items[i].Type, actual.Items[i].Type);
            Assert.Equal(colorAnim.Items[i].SParam,  actual.Items[i].SParam);
            Assert.Equal(colorAnim.Items[i].Keyframes, actual.Items[i].Keyframes);
        }
    }

    [Fact]
    public void ShouldRoundtripFloatAnimation()
    {
        var nodelib = new AlchemyNodeLibrary();
        var n = new AlchemyNode();
        n.ClassName = "Hello";

        var floatAnim = new AlchemyFloatAnimation();

        var floats1 = new AlchemyFloats()
        {
            SParam = 4,
            Type = EasingTypes.Step,
            Keyframes = [new(0, 1)]
        };
        floatAnim.Items.Add(floats1);
        var floats2 = new AlchemyFloats()
        {
            SParam = 5,
            Type = EasingTypes.Linear,
            Keyframes =
            [
                new(0, 3),
                new (0.2f, 6),
                new (1, 9)
            ]
        };
        floatAnim.Items.Add(floats2);


        n.Parameters.Add(new AleParameter(FloatsProp, floatAnim));
        nodelib.Nodes.Add(n);

        var copylib = new AlchemyNodeLibrary(ToUtfNode(nodelib));
        // Sanity check
        Assert.Equal(nodelib.Nodes.Count, copylib.Nodes.Count);
        Assert.Equal(nodelib.Nodes[0].ClassName, copylib.Nodes[0].ClassName);
        Assert.Equal(nodelib.Nodes[0].Parameters.Count, copylib.Nodes[0].Parameters.Count);

        var actual = (AlchemyFloatAnimation)nodelib.Nodes[0].Parameters[0].Value;
        Assert.Equal(floatAnim.Type, actual.Type);
        Assert.Equal(floatAnim.Items.Count, actual.Items.Count);
        for (int i = 0; i < floatAnim.Items.Count; i++)
        {
            Assert.Equal(floatAnim.Items[i].Type, actual.Items[i].Type);
            Assert.Equal(floatAnim.Items[i].SParam,  actual.Items[i].SParam);
            Assert.Equal(floatAnim.Items[i].Keyframes, actual.Items[i].Keyframes);
        }
    }
}
