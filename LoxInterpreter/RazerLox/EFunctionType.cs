namespace LoxInterpreter.RazerLox
{
    internal enum EFunctionType
    {
        /// <summary>
        /// Top-level statements.
        /// </summary>
        None = 0,

        /// <summary>
        /// Some user-defined function.
        /// </summary>
        Function = 1,

        /// <summary>
        /// Function that is a member of a class.
        /// </summary>
        Method = 2,

        /// <summary>
        /// Specific 'constructor' method of a class.
        /// </summary>
        Initializer = 3,
    }
}
