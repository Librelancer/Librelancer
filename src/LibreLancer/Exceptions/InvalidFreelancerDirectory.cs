using System;

namespace LibreLancer.Exceptions
{
    public class InvalidFreelancerDirectory : Exception
    {
        public InvalidFreelancerDirectory(string path) : base(path)
        { }
    }
}
