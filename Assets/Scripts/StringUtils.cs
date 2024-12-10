namespace PokeApi
{
    public static class StringUtils
    {
        public static string CapitalizeFirstLetter(string input)
        {
            return input.Substring(0, 1).ToUpper() + input.Substring(1);
        }
    }
}