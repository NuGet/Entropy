namespace Structures
{
    public class QueryValue
    {
        public Value Value { get; private set; }
        public string Variable { get; private set; }

        public QueryValue(object obj)
        {
            if (obj is Variable)
            {
                Value = null;
                Variable = ((Variable)obj).Value;
            }
            else
            {
                Value = new Value(obj);
                Variable = null;
            }
        }
    }
}
