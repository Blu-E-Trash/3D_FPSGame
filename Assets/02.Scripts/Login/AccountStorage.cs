using UnityEngine;

// 계정(아이디와 비밀번호)을 이 기기에 저장하고 확인한다. 화면은 모른다.
public static class AccountStorage
{
    // 다른 저장 값과 이름표가 겹치지 않게 아이디 앞에 붙인다.
    private const string KeyPrefix = "Account_";

    // 이 아이디로 가입한 계정이 있는가?
    public static bool Exists(string id)
    {
        return PlayerPrefs.HasKey(KeyPrefix + id);
    }

    // 새 계정을 저장한다.
    public static void Register(string id, string password)
    {
        PlayerPrefs.SetString(KeyPrefix + id, password);
        PlayerPrefs.Save();  // 게임이 갑자기 꺼져도 남도록 바로 쓴다.
    }

    // 아이디와 비밀번호가 저장된 계정과 맞는가?
    public static bool CheckPassword(string id, string password)
    {
        return Exists(id) && PlayerPrefs.GetString(KeyPrefix + id) == password;
    }
}
