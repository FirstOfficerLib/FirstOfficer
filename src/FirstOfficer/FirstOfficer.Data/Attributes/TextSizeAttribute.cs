namespace FirstOfficer.Data.Attributes
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextSizeAttribute: Attribute
    {
        public TextSizeAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; }
    }
}
