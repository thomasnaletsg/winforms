﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Drawing2D.Tests;

public class CustomLineCapTests
{
    public static IEnumerable<object[]> Ctor_Path_Path_LineCap_Float_TestData()
    {
        yield return new object[] { new GraphicsPath(), null, LineCap.Flat, 0f, LineCap.Flat };
        yield return new object[] { new GraphicsPath(), null, LineCap.Square, 1f, LineCap.Square };
        yield return new object[] { new GraphicsPath(), null, LineCap.Round, -1f, LineCap.Round };
        yield return new object[] { new GraphicsPath(), null, LineCap.Triangle, float.MaxValue, LineCap.Triangle };
        // All of these "anchor" values yield a "Flat" LineCap.
        yield return new object[] { new GraphicsPath(), null, LineCap.NoAnchor, 0f, LineCap.Flat };
        yield return new object[] { new GraphicsPath(), null, LineCap.SquareAnchor, 0f, LineCap.Flat };
        yield return new object[] { new GraphicsPath(), null, LineCap.DiamondAnchor, 0f, LineCap.Flat };
        yield return new object[] { new GraphicsPath(), null, LineCap.ArrowAnchor, 0f, LineCap.Flat };

        // Boxy cap
        GraphicsPath strokePath = new();
        strokePath.AddRectangle(new Rectangle(0, 0, 10, 10));
        yield return new object[] { null, strokePath, LineCap.Square, 0f, LineCap.Square };

        // Hook-shaped cap
        strokePath = new GraphicsPath();
        strokePath.AddLine(new Point(0, 0), new Point(0, 5));
        strokePath.AddLine(new Point(0, 5), new Point(5, 1));
        strokePath.AddLine(new Point(5, 1), new Point(3, 1));
        yield return new object[] { null, strokePath, LineCap.Flat, 0f, LineCap.Flat };

        // Fill path -- Must intercept the Y-axis.
        GraphicsPath fillPath = new();
        fillPath.AddLine(new Point(-5, -10), new Point(0, 10));
        fillPath.AddLine(new Point(0, 10), new Point(5, -10));
        fillPath.AddLine(new Point(5, -10), new Point(-5, -10));
        yield return new object[] { fillPath, null, LineCap.Flat, 0f, LineCap.Flat };
    }

    [Theory]
    [MemberData(nameof(Ctor_Path_Path_LineCap_Float_TestData))]
    public void Ctor_Path_Path_LineCap_Float(GraphicsPath fillPath, GraphicsPath strokePath, LineCap baseCap, float baseInset, LineCap expectedCap)
    {
        using (fillPath)
        using (strokePath)
        using (CustomLineCap customLineCap = new(fillPath, strokePath, baseCap, baseInset))
        {
            Assert.Equal(expectedCap, customLineCap.BaseCap);
            Assert.Equal(baseInset, customLineCap.BaseInset);
            Assert.Equal(LineJoin.Miter, customLineCap.StrokeJoin);
            Assert.Equal(1f, customLineCap.WidthScale);
        }
    }

    [Theory]
    // These values are outside the valid range of the LineCap enum.
    [InlineData(LineCap.Flat - 1)]
    [InlineData(LineCap.Custom + 1)]
    public void Ctor_InvalidLineCap_ReturnsFlat(LineCap baseCap)
    {
        using (GraphicsPath fillPath = new())
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(fillPath, strokePath, baseCap, 0f))
        {
            Assert.Equal(LineCap.Flat, customLineCap.BaseCap);
        }
    }

    [Fact]
    public void Ctor_FillPath_Incomplete_ThrowsArgumentException()
    {
        using (GraphicsPath fillPath = new())
        {
            fillPath.AddLine(new Point(0, -10), new Point(0, 10));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomLineCap(fillPath, null));
        }
    }

    [Fact]
    public void Ctor_FillPath_DoesNotCrossYAxis_ThrowsNotImplementedException()
    {
        // Closed fillPath, but does not cross the Y-axis.
        using (GraphicsPath fillPath = new())
        {
            fillPath.AddLine(new Point(-5, 5), new Point(5, 5));
            fillPath.AddLine(new Point(5, 5), new Point(5, 1));
            fillPath.AddLine(new Point(5, 1), new Point(-5, 5));
            Assert.Throws<NotImplementedException>(() => new CustomLineCap(fillPath, null));
        }
    }

    [Theory]
    [InlineData(LineCap.Square, LineCap.Square)]
    [InlineData(LineCap.Round, LineCap.Round)]
    [InlineData(LineCap.Triangle, LineCap.Triangle)]
    public void SetThenGetStrokeCaps_Success(LineCap startCap, LineCap endCap)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.SetStrokeCaps(startCap, endCap);
            customLineCap.GetStrokeCaps(out LineCap retrievedStartCap, out LineCap retrievedEndCap);

            Assert.Equal(startCap, retrievedStartCap);
            Assert.Equal(endCap, retrievedEndCap);
        }
    }

    [Theory]
    [InlineData(LineCap.SquareAnchor, LineCap.SquareAnchor)]
    [InlineData(LineCap.Custom, LineCap.Custom)]
    [InlineData(LineCap.Square, LineCap.Custom)]
    [InlineData(LineCap.Custom, LineCap.SquareAnchor)]
    [InlineData(LineCap.Flat - 1, LineCap.Flat)] // Below valid enum range
    [InlineData(LineCap.Custom + 1, LineCap.Flat)] // Above valid enum range
    public void SetStrokeCaps_InvalidLineCap_ThrowsArgumentException(LineCap startCap, LineCap endCap)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.SetStrokeCaps(startCap, endCap));

            // start and end cap should be unchanged.
            customLineCap.GetStrokeCaps(out LineCap retrievedStartCap, out LineCap retrievedEndCap);
            Assert.Equal(LineCap.Flat, retrievedStartCap);
            Assert.Equal(LineCap.Flat, retrievedEndCap);
        }
    }

    [Theory]
    [InlineData(LineJoin.Miter)] // Default value
    [InlineData(LineJoin.Bevel)]
    [InlineData(LineJoin.Round)]
    [InlineData(LineJoin.MiterClipped)]
    // Invalid (non-enum) values are allowed. Their values are stored and returned unchanged.
    [InlineData(LineJoin.Miter - 1)]
    [InlineData(LineJoin.MiterClipped + 1)]
    public void StrokeJoin_SetThenGet_Success(LineJoin lineJoin)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.StrokeJoin = lineJoin;
            Assert.Equal(lineJoin, customLineCap.StrokeJoin);
        }
    }

    [Theory]
    [InlineData(LineCap.Flat)] // Default value
    [InlineData(LineCap.Square)]
    [InlineData(LineCap.Round)]
    [InlineData(LineCap.Triangle)]
    public void BaseCap_SetThenGet_Success(LineCap baseCap)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.BaseCap = baseCap;
            Assert.Equal(baseCap, customLineCap.BaseCap);
        }
    }

    [Theory]
    [InlineData(LineCap.NoAnchor)]
    [InlineData(LineCap.SquareAnchor)]
    [InlineData(LineCap.RoundAnchor)]
    [InlineData(LineCap.DiamondAnchor)]
    [InlineData(LineCap.Custom)]
    [InlineData(LineCap.Flat - 1)]
    [InlineData(LineCap.Custom + 1)]
    public void BaseCap_Set_InvalidLineCap_ThrowsArgumentException(LineCap baseCap)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.BaseCap = baseCap);
            Assert.Equal(LineCap.Flat, customLineCap.BaseCap);
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(10f)]
    [InlineData(10000f)]
    [InlineData(-1f)]
    [InlineData(-10f)]
    [InlineData(-10000f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void BaseInset_SetThenGet_Success(float value)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.BaseInset = value;
            Assert.Equal(value, customLineCap.BaseInset);
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(10f)]
    [InlineData(10000f)]
    [InlineData(-1f)]
    [InlineData(-10f)]
    [InlineData(-10000f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.NaN)]
    public void WidthScale_SetThenGet_Success(float value)
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.WidthScale = value;
            Assert.Equal(value, customLineCap.WidthScale);
        }
    }

    [Fact]
    public void Disposed_MembersThrow()
    {
        using (GraphicsPath strokePath = new())
        using (CustomLineCap customLineCap = new(null, strokePath))
        {
            customLineCap.Dispose();
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.StrokeJoin);
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.BaseCap);
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.BaseInset);
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.WidthScale);
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.Clone());
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.SetStrokeCaps(LineCap.Flat, LineCap.Flat));
            AssertExtensions.Throws<ArgumentException>(null, () => customLineCap.GetStrokeCaps(out LineCap startCap, out LineCap endCap));
        }
    }
}
