namespace AuthService.Domain.Enums;

public enum MfaMode
{
    None = 0,
    PasswordOnly = 1,
    PasswordAndOtp = 2,
    OtpOnly = 3,
    PasswordAndTotp = 4
}
