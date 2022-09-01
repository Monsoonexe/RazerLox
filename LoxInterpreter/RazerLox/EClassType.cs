namespace LoxInterpreter.RazerLox
{
    /// <summary>
    /// 
    /// </summary>
    internal enum EClassType
    {
        /// <summary>
        /// Not inside any class.
        /// </summary>
        None = 0,

        /// <summary>
        /// Inside a class's method.
        /// </summary>
        Class = 1,

        /// <summary>
        /// Inside a class that has a super class.
        /// </summary>
        Subclass = 2,
    }
}