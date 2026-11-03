using UnityEngine;

// 로그인 화면의 효과음을 한곳에서 재생한다. (씬에 하나, 싱글톤)
// 다른 스크립트에서는 LoginSfx.Instance?.Blip() 처럼 부른다.
public class LoginSfx : SingletonBehaviour<LoginSfx>
{
    [SerializeField] private AudioSource _oneShot;
    [SerializeField] private AudioClip _blip;    // 버튼에 마우스를 올렸을 때
    [SerializeField] private AudioClip _type;    // 글자가 찍힐 때
    [SerializeField] private AudioClip _click;   // 버튼을 눌렀을 때 (릴레이 딸깍 + 삑)
    [SerializeField] private AudioClip _glitch;  // 화면이 찢어질 때
    [SerializeField] private AudioClip _boot;    // 화면이 켜질 때
    [SerializeField] private AudioClip _alert;   // 움직임 감지

    public void Blip() => Play(_blip, 0.5f, Random.Range(0.96f, 1.04f));
    public void Type() => Play(_type, 0.45f, Random.Range(0.9f, 1.15f));
    public void Click() => Play(_click, 0.8f);
    public void Glitch() => Play(_glitch, 0.55f, Random.Range(0.85f, 1.15f));
    public void Boot() => Play(_boot, 0.9f);
    public void Alert() => Play(_alert, 0.5f);

    private void Play(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null || _oneShot == null) return;

        _oneShot.pitch = pitch;
        _oneShot.PlayOneShot(clip, volume);
    }
}
