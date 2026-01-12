// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.Serialization;

namespace LibreLancer;

[Serializable]
public class FileVersionException : FileException
{
    private string? format;
    private int actualVersion, expectedVersion;

    public FileVersionException() : base() { }

    public FileVersionException(string? path) : base(path) { }

    public FileVersionException(string message, Exception innerException) : base(message, innerException) { }

    public FileVersionException(string? path, string format, int actualVersion, int expectedVersion)
        : base(path)
    {
        this.format = format;
        this.actualVersion = actualVersion;
        this.expectedVersion = expectedVersion;
    }

    public override string Message =>
        $"{base.Message}\r\nA {format ?? "unknown"} file of version {expectedVersion} was expected but a vesion {actualVersion} file was found";
}
