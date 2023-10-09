using System.Text;

namespace FirstOfficer.Tests.Generator
{
    public class BookQueryable
    {
        protected readonly StringBuilder WhereBuilder = new();
        
        public BookQueryable WhereAreEqual(BookQueryBy queryBy, object value)
        {
            switch (queryBy)
            {
                case BookQueryBy.Id:
                    WhereBuilder.Append($"id = {value}");
                    break;
                case BookQueryBy.Checksum:
                    break;
            }

            return this;
        }

        public enum BookQueryBy
        {
            Id,
            Checksum
            
        }

    }
}
