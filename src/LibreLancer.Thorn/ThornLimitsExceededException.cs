using System;

namespace LibreLancer.Thorn;

public class ThornLimitsExceededException(string message) : Exception(message);

