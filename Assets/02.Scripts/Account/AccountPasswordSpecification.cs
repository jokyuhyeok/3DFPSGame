using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자
    // (?=.*X)는 "어딘가에 X가 하나 이상 있다"는 조건이다. 기호는 영문·숫자가 아닌 문자다.
    // [!-~]는 공백을 뺀 키보드 문자(ASCII 33~126)라서 공백이나 한글은 쓸 수 없다.
    private static readonly Regex PasswordRegex =
        new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{8,30}$");

    public string ErrorMessage => "비밀번호는 소문자, 숫자, 기호를 모두 포함한 10~30 글자여야 합니다.";

    public bool IsSatisfiedBy(string email)
    {
        return PasswordRegex.IsMatch(email);
    }
}