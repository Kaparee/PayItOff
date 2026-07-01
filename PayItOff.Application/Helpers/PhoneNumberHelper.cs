namespace PayItOff.Application.Helpers;

public static class PhoneNumberHelper
{
    public static string? FormatPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        var raw = phoneNumber.Replace(" ", "");
        if (raw.StartsWith("+48")) raw = raw.Substring(3);
        if (raw.Length == 9)
            return $"+48 {raw.Substring(0, 3)} {raw.Substring(3, 3)} {raw.Substring(6, 3)}";
        return phoneNumber.Trim();
    }
}
