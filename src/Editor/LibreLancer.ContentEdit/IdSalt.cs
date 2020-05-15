// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.ContentEdit
{
    public class IdSalt
    {
        private const string charset = "0123456789abcdefghijklmnopqrstuvwxyz";
        //Create a 6 character random string to add to generated IDs
        public static string New()
        {
            var r = new Random();
            return
                $"{charset[r.Next(0, charset.Length)]}{charset[r.Next(0, charset.Length)]}{charset[r.Next(0, charset.Length)]}" +
                $"{charset[r.Next(0, charset.Length)]}{charset[r.Next(0, charset.Length)]}{charset[r.Next(0, charset.Length)]}";
        }
    }
}