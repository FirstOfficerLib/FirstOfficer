namespace FirstOfficer.Data.Attributes
{

    [AttributeUsage(AttributeTargets.Property)]
    public class TextSizeAttribute: Attribute
    {
        public TextSizeAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; }
    }
}
