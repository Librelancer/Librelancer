// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.Ini;

/// <summary>
/// Possible types of data stored in a BINI value field
/// </summary>
internal enum IniValueType : byte
{
    /// <summary>
    /// Boolean value
    /// </summary>
    Boolean = 0x00,

    /// <summary>
    /// 32bit signed integer value
    /// </summary>
    Int32 = 0x01,

    /// <summary>
    /// 32bit single precision floating point value
    /// </summary>
    Single = 0x02,

    /// <summary>
    /// 32bit unsigned integer as string table pointer
    /// </summary>
    String = 0x03
}