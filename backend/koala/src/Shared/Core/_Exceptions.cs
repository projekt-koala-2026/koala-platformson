namespace koala.src.Shared.Core
{
    public class NoActiveEditionException : Exception
    {
        public NoActiveEditionException(string message) : base(message) { }
    }
}