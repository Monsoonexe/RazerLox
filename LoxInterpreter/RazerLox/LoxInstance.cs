namespace LoxInterpreter.RazerLox
{
    internal class LoxInstance
    {
        private LoxClass loxClass;

        public LoxInstance(LoxClass loxClass)
        {
            this.loxClass = loxClass;
        }

        public override string ToString()
        {
            return loxClass.identifier + " instance";
        }
    }
}