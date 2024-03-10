using System.Globalization;

namespace VariableSubitemsPOC
{
    class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object? unk, Type targetType, object? parameter, CultureInfo culture)
        {
            if (unk is Enum @enum && parameter is object o)
            {
                return string.Equals($"{@enum}", $"{parameter}", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
