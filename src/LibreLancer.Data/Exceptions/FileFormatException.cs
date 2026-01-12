// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.Serialization;

namespace LibreLancer;

[Serializable]
public class FileFormatException : FileException
{
    private string? actualFormat, expectedFormat;

    public FileFormatException() : base() { }

    public FileFormatException(string? path) : base(path) { }

    public FileFormatException(string message, Exception innerException) : base(message, innerException) { }

    public FileFormatException(string? path, string actualFormat, string expectedFormat)
        : base(path)
    {
        this.actualFormat = actualFormat;
        this.expectedFormat = expectedFormat;
    }

    public override string Message =>
        $"{base.Message}\r\nA {expectedFormat ?? "unknown"} file was expected but a {actualFormat ?? "unknown"} file was found";
}
