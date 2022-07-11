using System.Text;

namespace Elrond.Herotag.Creator.Web.Extensions
{
    public static class HexExtensions
    {
        public static string ToHex(this string text)
        {
            var sBuffer = new StringBuilder();
            foreach (var character in text)
            {
                sBuffer.Append(Convert.ToInt32(character).ToString("x"));
            }
            return sBuffer.ToString();
        }
    }
}
