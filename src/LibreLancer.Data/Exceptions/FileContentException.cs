// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.Serialization;

namespace LibreLancer;

[Serializable]
public class FileContentException : FileException
{
    private string? format, message;

    public FileContentException() : base() { }

    public FileContentException(string? path) : base(path) { }

    public FileContentException(string message, Exception innerException) : base(message, innerException) { }

    public FileContentException(string format, string message)
    {
        this.format = format;
        this.message = message;
    }

    public FileContentException(string? path, string format, string message)
        : base(path)
    {
        this.format = format;
        this.message = message;
    }

    public override string Message => $"{base.Message}\r\nInvalid {format} file: {message}";
}
